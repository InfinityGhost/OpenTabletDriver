using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;

namespace OpenTabletDriver.Desktop.Reflection
{
    public class PluginManager : ServiceManager
    {
        public PluginManager()
        {
            var internalTypes = from asm in AssemblyLoadContext.Default.Assemblies
                where IsLoadable(asm)
                from type in asm.DefinedTypes
                where type.IsPublic && !(type.IsInterface || type.IsAbstract)
                where IsPluginType(type)
                where IsPlatformSupported(type)
                select type;

            pluginTypes = new ConcurrentBag<TypeInfo>(internalTypes);
        }

        public IReadOnlyCollection<TypeInfo> PluginTypes => pluginTypes;
        protected ConcurrentBag<TypeInfo> pluginTypes;

        protected readonly static IEnumerable<Type> libTypes =
            from type in Assembly.GetAssembly(typeof(IDriver)).GetExportedTypes()
                where type.IsAbstract || type.IsInterface
                select type;

        public virtual PluginReference GetPluginReference(string path) => new PluginReference(this, path);
        public virtual PluginReference GetPluginReference(Type type) => GetPluginReference(type.FullName);
        public virtual PluginReference GetPluginReference(object obj) => GetPluginReference(obj.GetType());

        public virtual T ConstructObject<T>(string name, object[] args = null) where T : class
        {
            args ??= new object[0];
            if (!string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    if (PluginTypes.FirstOrDefault(t => t.FullName == name) is TypeInfo type)
                    {
                        var matchingConstructors = from ctor in type.GetConstructors()
                            let parameters = ctor.GetParameters()
                            where parameters.Length == args.Length
                            where IsValidParameterFor(args, parameters)
                            select ctor;

                        if (matchingConstructors.FirstOrDefault() is ConstructorInfo constructor)
                        {
                            T obj = (T)constructor.Invoke(args) ?? null;
                            
                            if (obj != null)
                            {
                                var resolvedProperties = from property in type.GetProperties()
                                    where property.GetCustomAttribute<ResolvedAttribute>() is ResolvedAttribute
                                    select property;

                                foreach (var property in resolvedProperties)
                                {
                                    var service = GetService(property.PropertyType);
                                    if (service != null)
                                        property.SetValue(obj, service);
                                }

                                var resolvedFields = from field in type.GetFields()
                                    where field.GetCustomAttribute<ResolvedAttribute>() is ResolvedAttribute
                                    select field;

                                foreach (var field in resolvedFields)
                                {
                                    var service = GetService(field.FieldType);
                                    if (service != null)
                                        field.SetValue(obj, service);
                                }
                            }
                            return obj;
                        }
                        else
                        {
                            Log.Write("Plugin", $"No constructor found for '{name}'", LogLevel.Error);
                        }
                    }
                }
                catch (TargetInvocationException e) when (e.Message == "Exception has been thrown by the target of an invocation.")
                {
                    Log.Write("Plugin", "Object construction has thrown an error", LogLevel.Error);
                    Log.Exception(e.InnerException);
                }
                catch (Exception e)
                {
                    Log.Write("Plugin", $"Unable to construct object '{name}'", LogLevel.Error);
                    Log.Exception(e);
                }
            }
            return null;
        }

        public virtual IReadOnlyCollection<TypeInfo> GetChildTypes<T>()
        {
            var children = from type in PluginTypes
                where typeof(T).IsAssignableFrom(type)
                select type;

            return children.ToArray();
        }

        protected virtual bool IsValidParameterFor(object[] args, ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var arg = args[i];
                if (!parameter.ParameterType.IsAssignableFrom(arg.GetType()))
                    return false;
            }
            return true;
        }

        protected virtual bool IsPluginType(Type type)
        {
            return !type.IsAbstract && !type.IsInterface &&
                libTypes.Any(t => t.IsAssignableFrom(type) ||
                    type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == t));
        }

        protected virtual bool IsPlatformSupported(Type type)
        {
            var attr = (SupportedPlatformAttribute)type.GetCustomAttribute(typeof(SupportedPlatformAttribute), false);
            return attr?.IsCurrentPlatform ?? true;
        }

        protected virtual bool IsPluginIgnored(Type type)
        {
            return type.GetCustomAttributes(false).Any(a => a.GetType() == typeof(PluginIgnoreAttribute));
        }

        protected virtual bool IsLoadable(Assembly asm)
        {
            try
            {
                _ = asm.DefinedTypes;
                return true;
            }
            catch
            {
                var asmName = asm.GetName();
                Log.Write("Plugin", $"Plugin '{asmName.Name}, Version={asmName.Version}' can't be loaded and is likely out of date.", LogLevel.Warning);
                return false;
            }
        }
    }
}

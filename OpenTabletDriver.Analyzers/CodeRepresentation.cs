using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenTabletDriver.Analyzers
{
    internal static class CodeRepresentation
    {
        private static Regex OneLine = new Regex(@"\s+");

        public static int IndentLevel { get; set; }

        public static void PushBlock(this StringBuilder builder, string push = "{")
        {
            builder.AppendLine(Indent(push));
            IndentLevel++;
        }

        public static void PopBlock(this StringBuilder builder, string pop = "}")
        {
            IndentLevel--;
            builder.Append(Indent(pop));
        }

        public static string Indent(string code)
        {
            return code.PadLeft(code.Length + (IndentLevel * 4));
        }

        public static string ToOneLine(this string code)
        {
            return OneLine.Replace(code, " ");
        }

        public static string Create(object obj)
        {
            if (obj.DirectTranslate(out var directString))
            {
                return directString;
            }

            var a = new Dictionary<string, string>();

            var builder = new StringBuilder();
            switch (obj)
            {
                case DictionaryEntry dictionaryEntry:
                    var entry = $"{{ {Create(dictionaryEntry.Key).ToOneLine()}, {Create(dictionaryEntry.Value).ToOneLine()} }}";
                    builder.Append(entry);
                    break;
                case IEnumerable enumerable:
                    var enumerableInitializer = CreateEnumerableInitializer(enumerable);
                    builder.Append(enumerableInitializer);
                    break;
                default:
                    var defaultInitializer = CreateInitializer(obj);
                    builder.Append(defaultInitializer);
                    break;
            }

            return builder.ToString();
        }

        private static string GetCodeTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var typeName = type.Name.Split('`')[0];
                return $"{typeName}<{CreateGenericTypeArguments(type)}>";
            }
            else
            {
                return type.Name;
            }
        }

        private static bool DirectTranslate(this object obj, out string sourceCode)
        {
            switch (obj)
            {
                case string:
                    sourceCode = $"@\"{obj}\"";
                    return true;
                case float:
                    sourceCode = $"{obj}f";
                    return true;
                case ulong:
                case uint:
                case ushort:
                    sourceCode = $"{obj}u";
                    return true;
                case double:
                case long:
                case int:
                case short:
                case sbyte:
                case byte:
                    sourceCode = obj.ToString();
                    return true;
                case bool:
                    sourceCode = obj.ToString().ToLowerInvariant();
                    return true;
                case null:
                    sourceCode = "null";
                    return true;
            }

            sourceCode = null;
            return false;
        }

        private static string CreateInitializer(object obj)
        {
            var builder = new StringBuilder();

            var objType = obj.GetType();
            var properties = objType.GetProperties().Select(p => Indent($"{p.Name} = {Create(p.GetValue(obj))}"));

            builder.AppendLine($"new {GetCodeTypeName(objType)}");
            builder.PushBlock("{");
            builder.AppendLine(string.Join($",{Environment.NewLine}", properties));
            builder.PopBlock("}");

            return builder.ToString();
        }

        private static string CreateEnumerableInitializer(IEnumerable enumerable)
        {
            var builder = new StringBuilder();

            var enumerableType = enumerable.GetType();
            List<string> entryBuffer = new List<string>();

            builder.AppendLine($"new {GetCodeTypeName(enumerableType)}");
            builder.PushBlock("{");

            if (enumerable is IDictionary enumerableDictionary)
            {
                foreach (var dictionaryEntry in enumerableDictionary)
                {
                    entryBuffer.Add(Indent(Create(dictionaryEntry)));
                }
            }
            else
            {
                foreach (var entry in enumerable)
                {
                    entryBuffer.Add(Indent(Create(entry)));
                }
            }

            builder.AppendLine(string.Join($",{Environment.NewLine}", entryBuffer));
            builder.PopBlock("}");

            return builder.ToString();
        }

        private static string CreateGenericTypeArguments(Type type)
        {
            return string.Join(", ", type.GetGenericArguments().Select(t => t.Name));
        }
    }
}
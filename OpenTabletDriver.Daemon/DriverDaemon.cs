﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HidSharp;
using TabletDriverLib;
using TabletDriverLib.Binding;
using TabletDriverLib.Contracts;
using TabletDriverLib.Plugins;
using TabletDriverPlugin;
using TabletDriverPlugin.Attributes;
using TabletDriverPlugin.Logging;
using TabletDriverPlugin.Output;
using TabletDriverPlugin.Tablet;

namespace OpenTabletDriver.Daemon
{
    public class DriverDaemon : IDriverDaemon
    {
        public DriverDaemon()
        {
            Driver = new Driver();
            Log.Output += (sender, message) => LogMessages.Add(message);
            Log.Output += (sender, message) => Console.WriteLine(Log.GetStringFormat(message));
            LoadUserSettings();

            HidSharp.DeviceList.Local.Changed += (sender, e) => 
            {
                var newDevices = from device in DeviceList.Local.GetHidDevices()
                    where !CurrentDevices.Any(d => d == device)
                    select device;

                if (newDevices.Count() > 0)
                {
                    if (GetTablet() == null)
                        DetectTablets();
                }
                CurrentDevices = DeviceList.Local.GetHidDevices().ToList();
            };
        }

        private void LoadUserSettings()
        {
            LoadPlugins();
            DetectTablets();

            var appdataDir = new DirectoryInfo(AppInfo.Current.AppDataDirectory);
            if (!appdataDir.Exists)
            {
                appdataDir.Create();
                Log.Write("Settings", $"Created OpenTabletDriver application data directory: {appdataDir.FullName}");
            }

            var settingsFile = new FileInfo(AppInfo.Current.SettingsFile);
            if (Settings == null && settingsFile.Exists)
            {
                var settings = Settings.Deserialize(settingsFile);
                SetSettings(settings);
            }
        }

        public Driver Driver { private set; get; }
        private Settings Settings { set; get; }
        private List<HidDevice> CurrentDevices { set; get; } = DeviceList.Local.GetHidDevices().ToList();
        private Collection<FileInfo> LoadedPlugins { set; get; } = new Collection<FileInfo>();
        private Collection<LogMessage> LogMessages { set; get; } = new Collection<LogMessage>();
        private Collection<ITool> Tools { set; get; } = new Collection<ITool>();
        private DeviceDebuggerServer TabletDebuggerServer { set; get; }
        private DeviceDebuggerServer AuxDebuggerServer { set; get; }
        private LogServer LogServer { set; get; }

        public bool SetTablet(TabletProperties tablet)
        {
            return Driver.TryMatch(tablet);
        }

        public TabletProperties GetTablet()
        {
            return Driver.TabletReader != null && Driver.TabletReader.Reading ? Driver.TabletProperties : null;
        }

        public TabletProperties DetectTablets()
        {
            var configDir = new DirectoryInfo(AppInfo.Current.ConfigurationDirectory);
            if (configDir.Exists)
            {
                foreach (var file in configDir.EnumerateFiles("*.json", SearchOption.AllDirectories))
                {
                    var tablet = TabletProperties.Read(file);
                    if (SetTablet(tablet))
                        return GetTablet();
                }
            }
            else
            {
                Log.Write("Detect", $"The configuration directory '{configDir.FullName}' does not exist.", LogLevel.Error);
            }
            return null;
        }

        public void SetSettings(Settings settings)
        {
            Settings = settings;
            
            Driver.OutputMode = new PluginReference(Settings.OutputMode).Construct<IOutputMode>();

            if (Driver.OutputMode != null)
            {
                Log.Write("Settings", $"Output mode: {Driver.OutputMode.GetType().FullName}");
            }

            if (Driver.OutputMode is IOutputMode outputMode)
                SetOutputModeSettings(outputMode);
            
            if (Driver.OutputMode is AbsoluteOutputMode absoluteMode)
                SetAbsoluteModeSettings(absoluteMode);

            if (Driver.OutputMode is RelativeOutputMode relativeMode)
                SetRelativeModeSettings(relativeMode);

            if (Driver.OutputMode is IBindingHandler<IBinding> bindingHandler)
                SetBindingHandlerSettings(bindingHandler);

            if (Settings.AutoHook)
            {
                Driver.EnableInput = true;
                Log.Write("Settings", "Driver is auto-enabled.");
            }

            SetToolSettings();
        }

        private void SetOutputModeSettings(IOutputMode outputMode)
        {
            outputMode.Filters = from filterPath in Settings?.Filters
                let filter = new PluginReference(filterPath).Construct<IFilter>()
                where filter != null
                select filter;

            foreach (var filter in outputMode.Filters)
            {
                foreach (var property in filter.GetType().GetProperties())
                {
                    var settingPath = filter.GetType().FullName + "." + property.Name;
                    if (property.GetCustomAttribute<PropertyAttribute>(false) != null && 
                        Settings.PluginSettings.TryGetValue(settingPath, out var strValue))
                    {
                        try
                        {
                            var value = Convert.ChangeType(strValue, property.PropertyType);
                            property.SetValue(filter, value);
                        }
                        catch (FormatException)
                        {
                            Log.Write("Settings", $"Invalid filter setting for '{property.Name}', this setting will be cleared.");
                            Settings.PluginSettings.Remove(settingPath);
                        }
                    }
                }
            }

            if (outputMode.Filters != null && outputMode.Filters.Count() > 0)
                Log.Write("Settings", $"Filters: {string.Join(", ", outputMode.Filters)}");
            
            outputMode.TabletProperties = Driver.TabletProperties;
        }

        private void SetAbsoluteModeSettings(AbsoluteOutputMode absoluteMode)
        {
            absoluteMode.Output = new Area
            {
                Width = Settings.DisplayWidth,
                Height = Settings.DisplayHeight,
                Position = new Point
                {
                    X = Settings.DisplayX,
                    Y = Settings.DisplayY
                }
            };
            Log.Write("Settings", $"Display area: {absoluteMode.Output}");

            absoluteMode.Input = new Area
            {
                Width = Settings.TabletWidth,
                Height = Settings.TabletHeight,
                Position = new Point
                {
                    X = Settings.TabletX,
                    Y = Settings.TabletY
                },
                Rotation = Settings.TabletRotation
            };
            Log.Write("Settings", $"Tablet area: {absoluteMode.Input}");

            absoluteMode.VirtualScreen = TabletDriverLib.Interop.Platform.VirtualScreen;

            absoluteMode.AreaClipping = Settings.EnableClipping;   
            Log.Write("Settings", $"Clipping: {(absoluteMode.AreaClipping ? "Enabled" : "Disabled")}");
        }

        private void SetRelativeModeSettings(RelativeOutputMode relativeMode)
        {
            relativeMode.XSensitivity = Settings.XSensitivity;
            Log.Write("Settings", $"Horizontal Sensitivity: {relativeMode.XSensitivity}");

            relativeMode.YSensitivity = Settings.YSensitivity;
            Log.Write("Settings", $"Vertical Sensitivity: {relativeMode.YSensitivity}");

            relativeMode.ResetTime = Settings.ResetTime;
            Log.Write("Settings", $"Reset time: {relativeMode.ResetTime}");
        }

        private void SetBindingHandlerSettings(IBindingHandler<IBinding> bindingHandler)
        {
            bindingHandler.TipBinding = BindingTools.GetBinding(Settings.TipButton);
            bindingHandler.TipActivationPressure = Settings.TipActivationPressure;
            Log.Write("Settings", $"Tip Binding: '{(bindingHandler.TipBinding is IBinding binding ? binding.ToString() : "None")}'@{bindingHandler.TipActivationPressure}%");

            if (Settings.PenButtons != null)
            {
                for (int index = 0; index < Settings.PenButtons.Count; index++)
                    bindingHandler.PenButtonBindings[index] = BindingTools.GetBinding(Settings.PenButtons[index]);

                Log.Write("Settings", $"Pen Bindings: " + string.Join(", ", bindingHandler.PenButtonBindings));
            }

            if (Settings.AuxButtons != null)
            {
                for (int index = 0; index < Settings.AuxButtons.Count; index++)
                    bindingHandler.AuxButtonBindings[index] = BindingTools.GetBinding(Settings.AuxButtons[index]);

                Log.Write("Settings", $"Express Key Bindings: " + string.Join(", ", bindingHandler.AuxButtonBindings));
            }
        }

        private void SetToolSettings()
        {
            foreach (var runningTool in Tools)
            {
                runningTool.Dispose();
            }
            
            foreach (var toolName in Settings.Tools)
            {
                var plugin = new PluginReference(toolName);
                var type = plugin.GetTypeReference<ITool>();
                
                var tool = plugin.Construct<ITool>();
                foreach (var property in type.GetProperties())
                {
                    if (property.GetCustomAttribute<PropertyAttribute>(false) != null && 
                        Settings.PluginSettings.TryGetValue(type.FullName + "." + property.Name, out var strValue))
                    {
                        var value = Convert.ChangeType(strValue, property.PropertyType);
                        property.SetValue(tool, value);
                    }
                }

                if (tool.Initialize())
                    Tools.Add(tool);
                else
                    Log.Write("Tool", $"Failed to initialize {plugin.Name} tool.", LogLevel.Error);
            }
        }

        public Settings GetSettings()
        {
            return Settings;
        }

        public AppInfo GetApplicationInfo()
        {
            return AppInfo.Current;
        }

        public bool LoadPlugins()
        {
            var pluginDir = new DirectoryInfo(AppInfo.Current.PluginDirectory);
            if (pluginDir.Exists)
            {
                foreach (var file in pluginDir.EnumerateFiles("*.dll", SearchOption.AllDirectories))
                    ImportPlugin(file.FullName);
                return true;
            }
            else
                return false;
        }

        public bool ImportPlugin(string pluginPath)
        {
            if (LoadedPlugins.Any(p => p.FullName == pluginPath))
            {
                return true;
            }
            else
            {
                var plugin = new FileInfo(pluginPath);
                LoadedPlugins.Add(plugin);
                return TypeManager.AddPlugin(plugin);
            }
        }

        public void SetInputHook(bool isHooked)
        {
            Driver.EnableInput = isHooked;
        }

        public IEnumerable<Guid> SetTabletDebug(bool isEnabled)
        {
            if (isEnabled && TabletDebuggerServer == null)
            {
                if (Driver.TabletReader != null)
                {
                    TabletDebuggerServer = new DeviceDebuggerServer();
                    yield return TabletDebuggerServer.Identifier;
                    Driver.TabletReader.Report += TabletDebuggerServer.HandlePacket;
                }
                
                if (Driver.AuxReader != null)
                {
                    AuxDebuggerServer = new DeviceDebuggerServer();
                    yield return AuxDebuggerServer.Identifier;
                    Driver.AuxReader.Report += AuxDebuggerServer.HandlePacket;
                }
            }
            else if (!isEnabled && TabletDebuggerServer != null)
            {
                if (Driver.TabletReader != null)
                {
                    Driver.TabletReader.Report -= TabletDebuggerServer.HandlePacket;
                    TabletDebuggerServer.Dispose();
                    TabletDebuggerServer = null;
                }
                
                if (Driver.AuxReader != null)
                {
                    Driver.AuxReader.Report -= AuxDebuggerServer.HandlePacket;
                    AuxDebuggerServer.Dispose();
                    AuxDebuggerServer = null;
                }
            }
            else if (isEnabled && TabletDebuggerServer != null)
            {
                yield return TabletDebuggerServer.Identifier;
                if (AuxDebuggerServer != null)
                    yield return AuxDebuggerServer.Identifier;
            }
        }

        public Guid SetLogOutput(bool isEnabled)
        {
            if (isEnabled && LogServer == null)
            {
                LogServer = new LogServer();
            }
            else if (!isEnabled && LogServer != null)
            {
                LogServer.Dispose();
                LogServer = null;
            }
            return LogServer?.Identifier ?? Guid.Empty;
        }

        public IEnumerable<LogMessage> GetCurrentLog()
        {
            return LogMessages;
        }

        public IEnumerable<string> GetChildTypes<T>()
        {
            return from type in TypeManager.GetChildTypes<T>()
                where !type.IsInterface
                select type.FullName;
        }
    }
}
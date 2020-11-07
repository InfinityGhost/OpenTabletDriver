using System;
using System.IO;
using System.Reflection;
using OpenTabletDriver.Native;

namespace OpenTabletDriver
{
    public class AppInfo
    {
        public static readonly AppInfo Current = new AppInfo();

        private string configDirectory, appDataDirectory;
        
        public string ConfigurationDirectory
        {
            set => this.configDirectory = value;
            get => this.configDirectory ??= DefaultConfigurationDirectory;
        }

        public string AppDataDirectory
        {
            set => this.appDataDirectory = value;
            get => this.appDataDirectory ??= DefaultAppDataDirectory;
        }

        public string SettingsFile => Path.Join(AppDataDirectory, "settings.json");
        public string PluginDirectory => Path.Join(AppDataDirectory, "Plugins");

        private static string ProgramDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        private static string DefaultConfigurationDirectory
        {
            get
            {
                var path = Path.Join(ProgramDirectory, "Configurations");
                var fallbackPath = Path.Join(Environment.CurrentDirectory, "Configurations");
                return Directory.Exists(path) ? path : fallbackPath;
            }
        }

        private static string DefaultAppDataDirectory
        {
            get
            {
                var path = Path.Join(ProgramDirectory, "userdata");
                var fallbackPath = SystemInfo.CurrentPlatform switch
                {
                    RuntimePlatform.Windows => Path.Join(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "OpenTabletDriver"),
                    RuntimePlatform.Linux   => Path.Join(Environment.GetEnvironmentVariable("HOME"), ".config", "OpenTabletDriver"),
                    RuntimePlatform.MacOS   => Path.Join(Environment.GetEnvironmentVariable("HOME"), "Library", "Application Support", "OpenTabletDriver"),
                    _                       => null
                };
                return Directory.Exists(path) ? path : fallbackPath;
            }
        }
    }
}
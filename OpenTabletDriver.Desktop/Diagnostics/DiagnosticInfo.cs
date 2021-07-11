using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HidSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Logging;

namespace OpenTabletDriver.Desktop.Diagnostics
{
    public class DiagnosticInfo
    {
        public DiagnosticInfo(IEnumerable<LogMessage> log)
        {
            ConsoleLog = log;
        }

        [JsonProperty("App Version")]
        public string AppVersion { private set; get; } = GetAppVersion();

        [JsonProperty("Operating System")]
        public OperatingSystem OperatingSystem { private set; get; } = Environment.OSVersion;

        [JsonProperty("Environment Variables")]
        public IDictionary EnvironmentVariables { private set; get; } = Environment.GetEnvironmentVariables();

        [JsonProperty("HID Devices")]
        public IEnumerable<HidDevice> Devices { private set; get; } = DeviceList.Local.GetHidDevices();

        [JsonProperty("Console Log")]
        public IEnumerable<LogMessage> ConsoleLog { private set; get; }

        private static string GetAppVersion()
        {
            return "OpenTabletDriver v" + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        [OnError]
        internal void OnError(StreamingContext _, ErrorContext errorContext)
        {
            errorContext.Handled = true;
            Log.Write("Diagnostics", $"Handled diagnostics serialization error", LogLevel.Error);
            Log.Exception(errorContext.Error);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}

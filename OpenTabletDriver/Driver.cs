using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HidSharp;
using OpenTabletDriver.Devices;
using OpenTabletDriver.Interop;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Tablet;

namespace OpenTabletDriver
{
    public class Driver : IDriver, IDisposable
    {
        public Driver()
        {
            Info.GetDriverInstance = () => this;

            DeviceList.Local.Changed += (sender, e) =>
            {
                var newList = DeviceList.Local.GetHidDevices();
                var changes = new DevicesChangedEventArgs(CurrentDevices, newList);
                if (changes.Any())
                {
                    DevicesChanged?.Invoke(this, changes);
                    CurrentDevices = newList;
                }
            };
        }
        
        public event EventHandler<bool> Reading;
        public event EventHandler<IDeviceReport> ReportReceived;
        public event EventHandler<DevicesChangedEventArgs> DevicesChanged;
        public event EventHandler<TabletState> TabletChanged;

        private static readonly Dictionary<string, Func<IReportParser<IDeviceReport>>> reportParserDict = new Dictionary<string, Func<IReportParser<IDeviceReport>>>
        {
            { typeof(DeviceReportParser).FullName, () => new DeviceReportParser() },
            { typeof(TabletReportParser).FullName, () => new TabletReportParser() },
            { typeof(AuxReportParser).FullName, () => new AuxReportParser() },
            { typeof(TiltTabletReportParser).FullName, () => new TiltTabletReportParser() },
            { typeof(Vendors.SkipByteTabletReportParser).FullName, () => new Vendors.SkipByteTabletReportParser() },
            { typeof(Vendors.UCLogic.UCLogicReportParser).FullName, () => new Vendors.UCLogic.UCLogicReportParser() },
            { typeof(Vendors.Huion.GianoReportParser).FullName, () => new Vendors.Huion.GianoReportParser() },
            { typeof(Vendors.Wacom.BambooReportParser).FullName, () => new Vendors.Wacom.BambooReportParser() },
            { typeof(Vendors.Wacom.IntuosV2ReportParser).FullName, () => new Vendors.Wacom.IntuosV2ReportParser() },
            { typeof(Vendors.Wacom.IntuosV3ReportParser).FullName, () => new Vendors.Wacom.IntuosV3ReportParser() },
            { typeof(Vendors.Wacom.Wacom64bAuxReportParser).FullName, () => new Vendors.Wacom.Wacom64bAuxReportParser() },
            { typeof(Vendors.Wacom.WacomDriverIntuosV2ReportParser).FullName, () => new Vendors.Wacom.WacomDriverIntuosV2ReportParser() },
            { typeof(Vendors.Wacom.WacomDriverIntuosV3ReportParser).FullName, () => new Vendors.Wacom.WacomDriverIntuosV3ReportParser() },
            { typeof(Vendors.XP_Pen.XP_PenReportParser).FullName, () => new Vendors.XP_Pen.XP_PenReportParser() },
            { typeof(Vendors.XP_Pen.XP_PenTiltReportParser).FullName, () => new Vendors.XP_Pen.XP_PenTiltReportParser() }
        };

        protected IEnumerable<HidDevice> CurrentDevices { set; get; } = DeviceList.Local.GetHidDevices();

        public bool EnableInput { set; get; }

        private TabletState tablet;
        public TabletState Tablet
        {
            private set
            {
                if (value != this.tablet)
                {
                    // Stored locally to avoid re-detecting to switch output modes
                    this.tablet = value;
                    if (OutputMode != null)
                        OutputMode.Tablet = Tablet;
                    TabletChanged?.Invoke(this, value);
                }
            }
            get => this.tablet;
        }

        public IOutputMode OutputMode { set; get; }
        
        public DeviceReader<IDeviceReport> TabletReader { private set; get; }
        public DeviceReader<IDeviceReport> AuxReader { private set; get; }

        public virtual IEnumerable<TabletConfiguration> GetConfigurations() => CompiledTabletConfigurations.GetConfigurations();

        public bool TryMatch(TabletConfiguration config)
        {
            Log.Write("Detect", $"Searching for tablet '{config.Name}'");
            try
            {
                if (TryMatchDigitizer(config, out var digitizer))
                {
                    Log.Write("Detect", $"Found tablet '{config.Name}'");
                    if (!TryMatchAuxDevice(config, out var aux))
                    {
                        Log.Write("Detect", "Failed to find auxiliary device, express keys may be unavailable.", LogLevel.Warning);
                    }

                    Tablet = new TabletState(config, digitizer, aux);
                    return true;
                }
            }
            catch (IOException iex) when (iex.Message.Contains("Unable to open HID class device")
                && SystemInterop.CurrentPlatform == PluginPlatform.Linux)
            {
                Log.Write("DeviceUnathorizedAccessException",
                    "Current user don't have the permissions to open device streams. "
                    + "To fix this issue, please follow the instructions from https://github.com/OpenTabletDriver/OpenTabletDriver/wiki/Linux-FAQ#the-driver-fails-to-open-the-tablet-deviceioexception", LogLevel.Error);
            }
            catch (ArgumentOutOfRangeException aex) when (aex.Message.Contains("Value range is [0, 15]")
                && SystemInterop.CurrentPlatform == PluginPlatform.Linux)
            {
                Log.Write("DeviceInUseException",
                    "Device is currently in use by another kernel module. "
                    + "To fix this issue, please follow the instructions from https://github.com/OpenTabletDriver/OpenTabletDriver/wiki/Linux-FAQ#argumentoutofrangeexception-value-0-15", LogLevel.Error);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            Tablet = null;
            return false;
        }

        protected bool TryMatchDigitizer(TabletConfiguration config, out DigitizerIdentifier digitizerIdentifier)
        {
            digitizerIdentifier = default;
            foreach (var identifier in config.DigitizerIdentifiers)
            {
                var matches = FindMatchingDigitizer(identifier, config.Attributes);

                if (matches.Count() > 1)
                    Log.Write("Detect", "More than 1 matching digitizer has been found.", LogLevel.Warning);

                foreach (HidDevice dev in matches)
                {
                    // Try every matching device until we initialize successfully
                    try
                    {
                        var parser = GetReportParser(identifier) ?? new TabletReportParser();
                        InitializeDigitizerDevice(dev, identifier, parser);
                        digitizerIdentifier = identifier;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                        continue;
                    }
                }
            }
            return config.DigitizerIdentifiers.Count == 0;
        }

        protected bool TryMatchAuxDevice(TabletConfiguration config, out DeviceIdentifier auxIdentifier)
        {
            auxIdentifier = default;
            foreach (var identifier in config.AuxilaryDeviceIdentifiers)
            {
                var matches = FindMatchingAuxiliary(identifier, config.Attributes);

                if (matches.Count() > 1)
                    Log.Write("Detect", "More than 1 matching auxiliary device has been found.", LogLevel.Warning);

                foreach (HidDevice dev in matches)
                {
                    // Try every matching device until we initialize successfully
                    try
                    {
                        var parser = GetReportParser(identifier) ?? new AuxReportParser();
                        InitializeAuxDevice(dev, identifier, parser);
                        auxIdentifier = identifier;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                        continue;
                    }
                }
            }
            return config.AuxilaryDeviceIdentifiers.Count == 0;
        }

        protected void InitializeDigitizerDevice(HidDevice tabletDevice, DigitizerIdentifier tablet, IReportParser<IDeviceReport> reportParser)
        {
            TabletReader?.Dispose();

            string friendlyName = "Unnamed Device";
            try
            {
                friendlyName = tabletDevice.GetFriendlyName();
            }
            catch { }

            Log.Debug("Detect", $"Using device '{friendlyName}'.");
            Log.Debug("Detect", $"Using report parser type '{reportParser.GetType().FullName}'.");
            Log.Debug("Detect", $"Device path: {tabletDevice.DevicePath}");

            foreach (byte index in tablet.InitializationStrings)
            {
                Log.Debug("Device", $"Initializing index {index}");
                tabletDevice.GetDeviceString(index);
            }

            TabletReader = new DeviceReader<IDeviceReport>(tabletDevice, reportParser);
            TabletReader.Report += OnReportRecieved;
            TabletReader.ReadingChanged += (_, state) =>
            {
                Reading?.Invoke(this, state);
                if (state == false)
                    Tablet = null;
            };

            if (tablet.FeatureInitReport is byte[] featureInitReport && featureInitReport.Length > 0)
            {
                try
                {
                    TabletReader.ReportStream.SetFeature(featureInitReport);
                    Log.Debug("Device", "Set tablet feature: " + BitConverter.ToString(featureInitReport));
                }
                catch
                {
                    Log.Write("Device", "Failed to set tablet feature: " + BitConverter.ToString(featureInitReport), LogLevel.Warning);
                }
            }

            if (tablet.OutputInitReport is byte[] outputInitReport && outputInitReport.Length > 0)
            {
                try
                {
                    TabletReader.ReportStream.Write(outputInitReport);
                    Log.Debug("Device", "Set tablet output: " + BitConverter.ToString(outputInitReport));
                }
                catch
                {
                    Log.Write("Device", "Failed to set tablet output: " + BitConverter.ToString(outputInitReport), LogLevel.Warning);
                }
            }
        }

        protected void InitializeAuxDevice(HidDevice auxDevice, DeviceIdentifier identifier, IReportParser<IDeviceReport> reportParser)
        {
            AuxReader?.Dispose();

            string friendlyName = "Unnamed Device";
            try
            {
                friendlyName = auxDevice.GetFriendlyName();
            }
            catch { }

            Log.Debug("Detect", $"Using device '{friendlyName}'.");
            Log.Debug("Detect", $"Using auxiliary report parser type '{reportParser.GetType().Name}'.");
            Log.Debug("Detect", $"Device path: {auxDevice.DevicePath}");

            foreach (byte index in identifier.InitializationStrings)
            {
                Log.Debug("Device", $"Initializing index {index}");
                auxDevice.GetDeviceString(index);
            }

            AuxReader = new DeviceReader<IDeviceReport>(auxDevice, reportParser);
            AuxReader.Report += OnReportRecieved;

            if (identifier.FeatureInitReport is byte[] featureInitReport && featureInitReport.Length > 0)
            {
                try
                {
                    AuxReader.ReportStream.SetFeature(featureInitReport);
                    Log.Debug("Device", "Set aux feature: " + BitConverter.ToString(featureInitReport));
                }
                catch
                {
                    Log.Write("Device", "Failed to set aux feature: " + BitConverter.ToString(featureInitReport), LogLevel.Warning);
                }
            }

            if (identifier.OutputInitReport is byte[] outputInitReport && outputInitReport.Length > 0)
            {
                try
                {
                    AuxReader.ReportStream.Write(outputInitReport);
                    Log.Debug("Device", "Set aux output: " + BitConverter.ToString(outputInitReport));
                }
                catch
                {
                    Log.Write("Device", "Failed to set output: " + BitConverter.ToString(outputInitReport), LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Retrieve and construct the the report parser for an identifier.
        /// </summary>
        /// <param name="identifier">The identifier to retrieve the report parser path from.</param>
        protected virtual IReportParser<IDeviceReport> GetReportParser(DeviceIdentifier identifier) 
        {
            return reportParserDict[identifier.ReportParser].Invoke();
        }

        private void OnReportRecieved(object _, IDeviceReport report)
        {
            this.ReportReceived?.Invoke(this, report);
            if (EnableInput && OutputMode?.Tablet != null)
                HandleReport(report);
        }

        public virtual void HandleReport(IDeviceReport report)
        {
            OutputMode.Read(report);
        }

        private IEnumerable<HidDevice> FindMatchingDigitizer(DeviceIdentifier identifier, Dictionary<string, string> attributes)
        {
            return from device in FindMatches(identifier)
                where DigitizerMatchesAttribute(device, attributes)
                select device;
        }

        private IEnumerable<HidDevice> FindMatchingAuxiliary(DeviceIdentifier identifier, Dictionary<string, string> attributes)
        {
            return from device in FindMatches(identifier)
                where AuxMatchesAttribute(device, attributes)
                select device;
        }

        private bool TryDeviceOpen(HidDevice device)
        {
            try
            {
                return device.CanOpen;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return false;
            }
        }

        private IEnumerable<HidDevice> FindMatches(DeviceIdentifier identifier)
        {
            return from device in DeviceList.Local.GetHidDevices()
                where identifier.VendorID == device.VendorID
                where identifier.ProductID == device.ProductID
                where TryDeviceOpen(device)
                where identifier.InputReportLength == null || identifier.InputReportLength == device.GetMaxInputReportLength()
                where identifier.OutputReportLength == null || identifier.OutputReportLength == device.GetMaxOutputReportLength()
                where DeviceMatchesAllStrings(device, identifier)
                select device;
        }

        private bool DigitizerMatchesAttribute(HidDevice device, Dictionary<string, string> attributes)
        {
            if (SystemInterop.CurrentPlatform != PluginPlatform.Windows)
                return true;

            var devName = device.GetFileSystemName();

            bool interfaceMatches = attributes.ContainsKey("WinInterface") ? Regex.IsMatch(devName, $"&mi_{attributes["WinInterface"]}") : true;
            bool keyMatches = attributes.ContainsKey("WinUsage") ? Regex.IsMatch(devName, $"&col{attributes["WinUsage"]}") : true;

            return interfaceMatches && keyMatches;
        }

        private bool AuxMatchesAttribute(HidDevice device, Dictionary<string, string> attributes)
        {
            return true; // Future proofing
        }

        private bool DeviceMatchesAllStrings(HidDevice device, DeviceIdentifier identifier)
        {
            if (identifier.DeviceStrings == null || identifier.DeviceStrings.Count == 0)
                return true;
            
            foreach (var matchQuery in identifier.DeviceStrings)
            {
                try
                {
                    // Iterate through each device string, if one doesn't match then its the wrong configuration.
                    var input = device.GetDeviceString(matchQuery.Key);
                    var pattern = matchQuery.Value;
                    if (!Regex.IsMatch(input, pattern))
                        return false;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    return false;
                }
            }
            return true;
        }

        public void Dispose()
        {
            TabletReader?.Dispose();
            TabletReader.Report -= OnReportRecieved;
            TabletReader = null;
            
            AuxReader?.Dispose();
            AuxReader.Report -= OnReportRecieved;
            AuxReader = null;
        }
    }
}

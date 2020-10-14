using System;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Display;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.Plugin
{
    public interface IDriver
    {
        event EventHandler<bool> Reading;
        event EventHandler<IDeviceReport> ReportRecieved;

        bool EnableInput { set; get; }
        TabletConfiguration Tablet { get; }
        DigitizerIdentifier TabletIdentifier { get; }
        DeviceIdentifier AuxiliaryIdentifier { get; }
        IVirtualScreen VirtualScreen { get; }
        IOutputMode OutputMode { set; get; }

        bool TryMatch(TabletConfiguration tablet);
    }
}
using System;

namespace OpenTabletDriver.Environ.Drivers
{
    internal class WacomDriver : ProcessModuleQueryableDriver
    {
        protected override string FriendlyName => "Wacom";

        protected override string LinuxFriendlyName => FriendlyName;

        protected override string LinuxModuleName => "wacom";

        protected override string[] WinProcessNames { get; } = Array.Empty<string>();

        protected override string[] Heuristics { get; } = new string[]
        {
            "Wacom"
        };

        protected override DriverInfo GetWinDriverInfo()
        {
            return base.GetWinDriverInfo() with { IsBlockingDriver = false };
        }

        protected override DriverInfo GetLinuxDriverInfo()
        {
            return base.GetLinuxDriverInfo() with { IsBlockingDriver = false };
        }
    }
}
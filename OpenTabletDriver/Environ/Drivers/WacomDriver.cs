using System;

namespace OpenTabletDriver.Environ.Drivers
{
    internal class WacomDriver : ProcessModuleQueryableDriver
    {
        protected override string FriendlyName => "Wacom";

        protected override (string, string) LinuxModuleName => ("Wacom", "wacom");

        protected override string[] WinProcessNames => Array.Empty<string>();

        protected override string[] Heuristics { get; } = new string[]
        {
            "Wacom"
        };

        protected override DriverInfo GetLinuxDriverInfo()
        {
            return base.GetLinuxDriverInfo() with { IsBlockingDriver = true };
        }
    }
}
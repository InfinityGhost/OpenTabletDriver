namespace OpenTabletDriver.Environ.Drivers
{
    internal class VeikkDriver : ProcessModuleQueryableDriver
    {
        protected override string FriendlyName => "Veikk";

        protected override string LinuxFriendlyName => "UC Logic";

        protected override string LinuxModuleName => "hid_uclogic";

        protected override string[] WinProcessNames { get; } = new string[]
        {
            "TabletDriverCenter",
            "TabletDriverSetting"
        };

        protected override string[] Heuristics { get; } = new string[]
        {
            "Veikk"
        };
    }
}
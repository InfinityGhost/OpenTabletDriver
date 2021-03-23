namespace OpenTabletDriver.Environ.Drivers
{
    internal class VeikkDriver : ProcessModuleQueryableDriver
    {
        protected override string FriendlyName => "Veikk";

        protected override (string, string) LinuxModuleName => ("UC Logic", "hid_uclogic");

        protected override string[] WinProcessNames => new string[]
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
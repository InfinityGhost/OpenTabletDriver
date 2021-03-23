namespace OpenTabletDriver.Environ.Drivers
{
    internal class GaomonDriver : ProcessModuleQueryableDriver
    {
        protected override string FriendlyName => "Gaomon";

        protected override (string, string) LinuxModuleName => ("UC Logic", "hid_uclogic");

        protected override string[] WinProcessNames { get; } = new string[]
        {
            "TabletDriverCore"
        };

        protected override string[] Heuristics { get; } = new string[]
        {
            "Gaomon"
        };
    }
}
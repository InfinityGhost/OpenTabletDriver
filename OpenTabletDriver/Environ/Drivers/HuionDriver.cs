namespace OpenTabletDriver.Environ.Drivers
{
    internal class HuionDriver : ProcessModuleQueryableDriver
    {
        protected override string FriendlyName => "Huion";

        protected override (string, string) LinuxModuleName => ("UC Logic", "hid_uclogic");

        protected override string[] WinProcessNames => new string[]
        {
            "TabletDriverCore"
        };

        protected override string[] Heuristics { get; } = new string[]
        {
            "Huion"
        };
    }
}
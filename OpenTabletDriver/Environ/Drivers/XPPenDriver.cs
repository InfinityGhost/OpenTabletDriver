using System.Linq;

namespace OpenTabletDriver.Environ.Drivers
{
    internal class XPPenDriver : ProcessModuleQueryableDriver
    {
        protected override string FriendlyName => "XP-Pen";

        protected override (string, string) LinuxModuleName => ("UC Logic", "hid_uclogic");

        protected override string[] WinProcessNames => new string[]
        {
            "PentabletService",
            "PentabletUIService",
            "PenTablet"
        };

        protected override string[] Heuristics { get; } = new string[]
        {
            "XP[ _-]*Pen",
            "Pentablet"
        };

        protected override DriverInfo GetWinDriverInfo()
        {
            var processes = DriverInfo.SystemProcesses.Where(p => WinProcessNames.Contains(p.ProcessName)).ToArray();
            if (processes.Any())
            {
                return new DriverInfo
                {
                    Name = FriendlyName,
                    Processes = processes,
                    IsSendingInput = true
                };
            }
            else
            {
                return null;
            }
        }
    }
}
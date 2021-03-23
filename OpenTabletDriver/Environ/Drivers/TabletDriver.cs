using System.Diagnostics;
using System.Linq;
using OpenTabletDriver.Interop;
using OpenTabletDriver.Plugin;

namespace OpenTabletDriver.Environ.Drivers
{
    internal class TabletDriver : IDriverInfoProvider
    {
        private string[] ProcessNames = new string[]
        {
            "TabletDriverGUI",
            "TabletDriverService"
        };

        public DriverInfo GetDriverInfo()
        {
            if (SystemInterop.CurrentPlatform == PluginPlatform.Windows)
            {
                var processes = DriverInfo.SystemProcesses.Where(p => ProcessNames.Contains(p.ProcessName)).ToArray();
                if (processes.Any())
                {
                    return new DriverInfo
                    {
                        Name = "TabletDriver",
                        Processes = processes,
                        IsBlockingDriver = true, // TabletDriver opens tablets in exclusive mode by default
                        IsSendingInput = true
                    };
                }
            }

            return null;
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using OpenTabletDriver.Interop;
using OpenTabletDriver.Plugin;

namespace OpenTabletDriver.Environ.Drivers
{
    internal abstract class ProcessModuleQueryableDriver : IDriverInfoProvider
    {
        protected abstract string FriendlyName { get; }
        protected abstract (string, string) LinuxModuleName { get; }
        protected abstract string[] WinProcessNames { get; }
        protected abstract string[] Heuristics { get; }

        private static string PnpUtil;

        public DriverInfo GetDriverInfo()
        {
            return SystemInterop.CurrentPlatform switch
            {
                PluginPlatform.Windows => GetWinDriverInfo(),
                PluginPlatform.Linux => GetLinuxDriverInfo(),
                _ => null
            };
        }

        protected virtual DriverInfo GetWinDriverInfo()
        {
            IEnumerable<Process> processes;
            var match = Heuristics.Any(name => Regex.IsMatch(PnpUtil, name, RegexOptions.IgnoreCase));
            if (match)
            {
                processes = DriverInfo.SystemProcesses
                    .Where(p => WinProcessNames.Concat(Heuristics)
                    .Any(n => Regex.IsMatch(p.ProcessName, n, RegexOptions.IgnoreCase)));

                return new DriverInfo
                {
                    Name = FriendlyName,
                    Processes = processes.Any() ? processes.ToArray() : null,
                    IsBlockingDriver = true,
                    IsSendingInput = processes.Any()
                };
            }

            return null;
        }

        protected virtual DriverInfo GetLinuxDriverInfo()
        {
            var lsmodProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "lsmod",
                    RedirectStandardOutput = true,
                    UseShellExecute = true,
                }
            };
            lsmodProc.Start();

            if (lsmodProc.WaitForExit(1000) && lsmodProc.StandardOutput.ReadToEnd().Contains(LinuxModuleName.Item2))
            {
                return new DriverInfo
                {
                    Name = LinuxModuleName.Item1,
                    IsBlockingDriver = true,
                    IsSendingInput = true
                };
            }
            else
            {
                return null;
            }
        }

        internal static void Refresh()
        {
            var pnputilProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "C:\\Windows\\System32\\pnputil.exe",
                    Arguments = "-e",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };

            pnputilProc.Start();
            PnpUtil = pnputilProc.StandardOutput.ReadToEnd();
        }
    }
}
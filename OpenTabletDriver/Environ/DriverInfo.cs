using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenTabletDriver.Environ.Drivers;

namespace OpenTabletDriver.Environ
{
    /// <summary>
    /// Contains information and hints about an installed tablet driver.
    /// </summary>
    /// <remarks>
    /// See <see cref="GetDriverInfos"/> to get all the currently active tablet drivers.
    /// </remarks>
    public record DriverInfo
    {
        /// <summary>
        /// The human-friendly name of the driver.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Running processes that might be associated with the driver.
        /// </summary>
        /// <remarks>
        /// This is set to null when there is no associated process.
        /// </remarks>
        public Process[] Processes { get; init; }

        /// <summary>
        /// Provides hints of whether this driver might interfere with OTD's detection mechanism, or prevent OTD from accessing the tablet.
        /// </summary>
        public bool IsBlockingDriver { get; init; }

        /// <summary>
        /// Returns true if this driver sends input to the operating system.
        /// </summary>
        public bool IsSendingInput { get; init; }

        /// <summary>
        /// Retrieves all the currently active tablet drivers.
        /// </summary>
        public static IEnumerable<DriverInfo> GetDriverInfos()
        {
            var providers = new IDriverInfoProvider[]
            {
                new WacomDriver(),
                new GaomonDriver(),
                new HuionDriver(),
                new XPPenDriver(),
                new VeikkDriver(),
                new TabletDriver()
            };

            SystemProcesses = Process.GetProcesses();
            ProcessModuleQueryableDriver.Refresh();

            // Remove "UC Logic" duplicates
            return providers.Select(provider => provider.GetDriverInfo())
                .Where(i => i != null)
                .GroupBy(i => i.Name)
                .Select(g => g.First());
        }

        internal static Process[] SystemProcesses { get; private set; }
    }
}
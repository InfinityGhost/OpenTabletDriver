using BenchmarkDotNet.Attributes;
using OpenTabletDriver;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.Benchmarks
{
    [MemoryDiagnoser]
    public class ConfigurationEnumerationBenchmarks
    {
        public TabletConfiguration Configuration { get; set; }

        [Benchmark]
        public void EnumerateCompiledConfigurations()
        {
            foreach (var config in CompiledTabletConfigurations.GetConfigurations())
            {
                Configuration = config;
            }
        }
    }
}
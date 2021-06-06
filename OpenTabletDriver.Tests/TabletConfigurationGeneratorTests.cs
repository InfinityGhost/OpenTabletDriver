using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenTabletDriver.Tests
{
    [TestClass]
    public class TabletConfigurationGeneratorTests
    {
        [TestMethod]
        public void GetPreCompiledConfiguration()
        {
            var configs = CompiledTabletConfigurations.GetConfigurations().ToArray();
            Assert.IsTrue(configs.Length > 100);
            
            foreach (var tabletConfig in configs)
            {
                Assert.IsNotNull(tabletConfig);
                Assert.IsTrue(!string.IsNullOrEmpty(tabletConfig.Name));
            }
        }
    }
}
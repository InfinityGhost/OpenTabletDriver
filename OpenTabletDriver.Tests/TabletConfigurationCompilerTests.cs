using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenTabletDriver.Tests
{
    [TestClass]
    public class TabletConfigurationCompilerTests
    {
        [TestMethod]
        public void GetPreCompiledConfiguration()
        {
            var tabletConfig = Driver.GetPreCompiledConfigurations().FirstOrDefault();
            Assert.IsNotNull(tabletConfig);
            Assert.IsTrue(!string.IsNullOrEmpty(tabletConfig.Name));
        }
    }
}
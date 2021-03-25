using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenTabletDriver.Tests
{
    [TestClass]
    public class TestBase
    {
        protected static string TestDirectory = System.Environment.GetEnvironmentVariable("OPENTABLETDRIVER_TEST") ?? System.Environment.CurrentDirectory;
    }
}
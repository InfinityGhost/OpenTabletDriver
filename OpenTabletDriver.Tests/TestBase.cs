using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTabletDriver.Desktop;

namespace OpenTabletDriver.Tests
{
    [TestClass]
    public class TestBase
    {
        protected static string TestDirectory = System.Environment.GetEnvironmentVariable("OPENTABLETDRIVER_TEST") ?? System.Environment.CurrentDirectory;

        protected class TestAppInfo : AppInfo
        {
            public override string AppDataDirectory => Path.Join(TestDirectory, nameof(PluginRepositoryTest));
        }
    }
}
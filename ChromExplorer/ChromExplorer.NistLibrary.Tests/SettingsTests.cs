using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.NistLibrary.Tests
{
    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void FirstLocatorTest()
        {
            var settings = NistLibrarySettings.Default;

            var fi = new FileInfo(settings.FirstLocatorFile);

            Assert.IsTrue(fi.Exists);
        }
    }
}

using System;
using ChromExplorer.ModasReader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.Modules.ModasChromProvider.Tests
{
    [TestClass]
    public class CalibrationCacheTests
    {
        [TestMethod]
        public void CodeContractTest()
        {
            var calibrations = new CalibrationCache();

            bool thrown = false;
            try
            {
                calibrations.GetCalibration(null);
            }
            catch(Exception)
            {
                thrown = true;
            }

            Assert.IsTrue(thrown);
        }
    }
}

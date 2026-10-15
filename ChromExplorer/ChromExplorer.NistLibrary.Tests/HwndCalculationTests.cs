using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.NistLibrary.Tests
{
    /// <summary>
    /// Summary description for HwndCalculationTests
    /// </summary>
    [TestClass]
    public class HwndCalculationTests
    {
        public HwndCalculationTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void DemoCalcTest()
        {
            var hwnd = 0xA02D4;

            int hi = GetHighint(hwnd);
            int lo = GetLowint(hwnd);

            //Assert.AreEqual(10, hi);
            //Assert.AreEqual(724, lo);

            Assert.AreEqual(724, BitConverter.ToInt16(BitConverter.GetBytes(hwnd), 0));
            Assert.AreEqual(10, BitConverter.ToInt16(BitConverter.GetBytes(hwnd), 2));
            
        }

        public static int GetHighint(int intValue)
        {
            return Convert.ToInt32(intValue >> 16);
        }

        public static int GetLowint(int intValue)
        {
            long tmp = intValue << 32;
            return Convert.ToInt32(tmp >> 16);
        }
    }
}

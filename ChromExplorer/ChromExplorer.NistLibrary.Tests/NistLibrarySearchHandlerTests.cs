using System;
using System.Text;
using System.Collections.Generic;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Formats;
using ChromExplorer.DataModel.PeakPicking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.NistLibrary.Tests
{
    /// <summary>
    /// Summary description for NistLibrarySearchHandlerTests
    /// </summary>
    [TestClass]
    public class NistLibrarySearchHandlerTests
    {
        public NistLibrarySearchHandlerTests()
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
        public void FileCreationTest()
        {
            var handler = new NistLibrarySearchHandler();
            var s = GetSpectrum();

            handler.CreateSearchFiles(s, false);
        }

        [TestMethod]
        public void StartInteractiveSearch()
        {
            var handler = new NistLibrarySearchHandler();
            var s = GetSpectrum();

            handler.CreateSearchFiles(s, false);
        }


        private NistTextSpectrum GetSpectrum()
        {
            var chrom = new GenericChromatogram
            {
                Title = "caffeine 400ngul POS 201412009.mdm",
                SelectedIndexes = null,
                SelectedIndex = 36,
            };

            var pl = new List<PeakInfo>
            {
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332}
            };


            var spectrum = new NistTextSpectrum();
            spectrum.Init(chrom, pl);

            return spectrum;
            
        }
    }
}

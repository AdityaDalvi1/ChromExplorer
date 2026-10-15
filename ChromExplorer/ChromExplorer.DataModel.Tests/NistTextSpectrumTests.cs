using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Formats;
using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.DataModel.Spectrum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{
    /// <summary>
    /// Summary description for NistTextSpectrumTests
    /// </summary>
    [TestClass]
    public class NistTextSpectrumTests
    {
        public NistTextSpectrumTests()
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
        public void EmptyTest()
        {
            var spectrum = new NistTextSpectrum();

            var writer = new StringWriter();
            spectrum.Write(writer);

            Assert.AreEqual("NAME: Empty Spectrum", writer.ToString().Trim());


        }

        [TestMethod]
        public void ExportCoffeinTest()
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

            using (var writer = new StreamWriter("coffein.msp"))
            {
                spectrum.Write(writer);
            }
        }

        [TestMethod]
        public void LongPeakListTest()
        {
            var chrom = new GenericChromatogram
            {
                Title = "Long peak list.mdm",
                SelectedIndexes = null,
                SelectedIndex = 1,
            };

            var pl = new List<PeakInfo>
            {
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},
                new PeakInfo() {Position = 195.285336090619, Intensity = 1104.17800526266},
                new PeakInfo() {Position = 196.298430048542, Intensity = 114.883942685327},
                new PeakInfo() {Position = 199.105076876766, Intensity = 228.252314708958},
                new PeakInfo() {Position = 211.093264297761, Intensity = 379.275976498332},

            };


            var spectrum = new NistTextSpectrum();
            spectrum.Init(chrom, pl);

            using (var writer = new StreamWriter("long.msp"))
            {
                spectrum.Write(writer);
            }
        }

    }
}

/*
 * 195.285336090619	1104.17800526266	195.3
196.298430048542	114.883942685327	196.3
199.105076876766	228.252314708958	199.1
211.093264297761	379.275976498332	211.1
 * */

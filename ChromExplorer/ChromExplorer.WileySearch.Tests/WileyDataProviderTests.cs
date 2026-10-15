using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.WileySearch.Tests
{
    [TestClass]
    public class WileyDataProviderTests
    {
        [TestMethod]
        public void PathTest()
        {
            var path = WileyDataProvider.WileyLibPath;

            Console.WriteLine(path);
        }

        [TestMethod]
        public void CreateTests()
        {
            var provider = new WileyDataProvider();

            Assert.AreEqual(10712, provider.RecordCount);
        }


        [TestMethod]
        public void LoadTimeTest()
        {
            var currentUsage = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            var provider = new WileyDataProvider();

            var count = provider.RecordCount;
            var dict = new Dictionary<int, string>();

            for (int i = 0; i < count; i++)
            {
                dict.Add(i, provider.GetCompound(i));
            }

            
            Console.WriteLine("Elapsed {0} ms, {1} elements readed", sw.ElapsedMilliseconds, dict.Count);

            var newUsage = GC.GetTotalMemory(false);

            Console.WriteLine("Memory change {0} Mb", (newUsage - currentUsage));
        }

        [TestMethod]
        public void GetIntData()
        {
           
        }

        [TestMethod]
        public void GetRecordTest()
        {
            int id = 168;
            var provider = new WileyDataProvider();

            var record = provider.GetRecord(id);

            Assert.IsNotNull(record);

            Assert.AreEqual(id, record.RecordID);
            Assert.AreEqual(328.15, record.PrecursorIon, 0.01);
            Assert.AreEqual(17, record.SpectrumGroup);
            Assert.AreEqual(13, record.SpectrumNo);
            Assert.AreEqual("[M+H]+", record.PrecursorType);
            Assert.AreEqual("ESI+", record.IonizationType);
            Assert.AreEqual(30, record.IonizationEnergy);
            Assert.AreEqual("QStar XL, AB Sciex", record.Instrument);
            Assert.AreEqual(5462507, record.PubchemCID);
            Assert.AreEqual(327.38, record.MolWeight, 0.01);
            Assert.AreEqual(327.15, record.MSMolWeight, 0.01);
            Assert.AreEqual("C19H21NO4", record.MolFormula);
            Assert.AreEqual("6-o-monoacetylmorphine", record.Compaund);
            Assert.AreEqual("2784-73-8", record.CASRegistryNo);
            Assert.AreEqual("InChI=1S/C19H21NO4/c1-10(21)23-15-6-4-12-13-9-11-3-5-14(22)17-16(11)19(12,18(15)24-17)7-8-20(13)2/h3-6,12-13,15,18,22H,7-9H2,1-2H3/t12-,13+,15-,18-,19-/m0/s1", record.InChi);
            Assert.AreEqual("", record.Name);
            Assert.IsNotNull(record.Structure);
            Assert.IsNotNull(record.Spectrum);
            Assert.IsTrue(record.Spectrum.Count > 0);
            Assert.AreEqual(142, record.Spectrum.Count);

        }

        [TestMethod]
        public void GetSpectrumAsString()
        {
            var provider = new WileyDataProvider();
            var data = provider.GetFieldAsSting(123, "MassSpectrum");
            var record = provider.GetRecord(123);
            Console.WriteLine("Spectrum size {0}", record.Spectrum.Count);
            Console.WriteLine(data);

        }
    }
}

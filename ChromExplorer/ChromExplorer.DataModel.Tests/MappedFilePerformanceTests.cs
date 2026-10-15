using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{
    [TestClass]
    public class MappedFilePerformanceTests
    {
        private static int _spectrums = 1000;
        private static int _points = 50000;
        private static double _ppd = 25;
        private static double _mFrom = 200;
        private static string _filename = "bench.dat";
        private static List<ChromatogramPoint> _chromPoints;

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

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            Write(_filename);
            Thread.Sleep(100);

            _chromPoints = new List<ChromatogramPoint>();

            for (int i = 0; i < _spectrums; i++)
            {
                _chromPoints.Add(new ChromatogramPoint() { Index = i, SpectrumID = i, TIC = 0.0, Time = i, TimeUnits = "au" });
            }

        }

        [TestMethod]
        public void ReadChrom()
        {
            var provider = new FileMappedSpectrumData(_filename);
            var timer = Stopwatch.StartNew();

            var chrom = provider.ReadChromatogram();
            timer.Stop();
            Assert.AreEqual(chrom[0].Time, 0.0);
            Assert.AreEqual(chrom[0].TIC, 1000);

            Assert.AreEqual(chrom[500].Time, 500);
            Assert.AreEqual(chrom[500].TIC, 1500);

            Console.WriteLine("{0}: {1} ms", chrom.Count(), timer.ElapsedMilliseconds);
        }

        [TestMethod]
        public void RandomAccessBenchmark()
        {
            var provider = new FileMappedSpectrumData(_filename);
            var rnd = new Random(DateTime.Now.Millisecond);
            for(int i = 0; i < 100; i++)
            {
                var index = rnd.Next(0, _spectrums);

                var timer = Stopwatch.StartNew();

                var spectrum = provider[index];

                Console.WriteLine("{0}: {1} ms", index, timer.ElapsedMilliseconds);
            }
        }

        //[TestMethod]
        public void RebuildChromBenchmark()
        {
            var sizes = new int[] {1, 2, 5, 100, 1000, 5000};
            int tries = 5;

            var provider = new FileMappedSpectrumData(_filename);
            var rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < tries; i++)
            {
                foreach (var size in sizes)
                {
                    var index = rnd.Next(0, _points - size);

                    var timer = Stopwatch.StartNew();

                    var indexes = new int[size];

                    for(int j = 0; j < size; j++)
                    {
                        indexes[j] = index++;
                    }

                    var chrom = provider.Rebuild(indexes, _chromPoints);

                    Console.WriteLine("{0}: {1} ms", index, timer.ElapsedMilliseconds);
                }
               
            }

        }

        //[TestMethod]
        public void AggregateBenchmark()
        {
            var sizes = new int[] {1, 2, 5, 10, 100  };
            int tries = 1;

            var provider = new FileMappedSpectrumData(_filename);
            var rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < tries; i++)
            {
                foreach (var size in sizes)
                {
                    var index = rnd.Next(0, _spectrums - size);

                    var timer = Stopwatch.StartNew();

                    var indexes = new int[size];

                    for (int j = 0; j < size; j++)
                    {
                        indexes[j] = index++;
                    }

                    var spectrum = provider.GetAggregateSpectrum(indexes);

                    Console.WriteLine("{0}: {1} ms", index, timer.ElapsedMilliseconds);
                }

            }
        }

        private static void Write(string filename)
        {
            int id = 1;
            using (var stream = new FileStream(filename, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var header = new FileMappedSpectrumData.Header
                    {
                        Spectrums = _spectrums,
                        Points = _points,
                        PPD = _ppd,
                        MFrom = -_mFrom
                    };

                    header.Write(writer);
                    
                    for (int i = 0; i < _spectrums; i++)
                    {
                        var meta = new FileMappedSpectrumData.SpectrumMetaData() { EllapsedMilliseconds = i, TIC = 1000 + i };
                        meta.Write(writer);
                        for (int j = 0; j < _points; j++)
                        {
                            int value = id++;
                            writer.Write(value);
                            //Console.WriteLine("Write: S = {0}, P = {1}, Data = {2}", i, j, value);
                        }
                    }

                    writer.Close();
                }
            }
        }


    }
}

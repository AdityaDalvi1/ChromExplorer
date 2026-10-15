using System;
using System.Collections.Generic;
using System.IO;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.SpikeRemoval;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{
    [TestClass]
    public class SpikeremovalTests
    {
        private const string Filename = @"spikes_0.dat";
        private const string GoodFilename = @"good_0.dat";
        private const string OriginalOutFilename = @"spikes_0_out.dat";
        private const string SortedOutFilename = @"sorted_0_out.dat";
        private const string StdOutFilename = @"std_0_out.dat";
        private const string IncOutFilename = @"inc_0_out.dat";
        private const string ProcessedOutFilename = @"clean_0_out.dat";

        
        [TestMethod]
        public void SaveLoadTest()
        {
            var ds = Load(Filename);
            Console.WriteLine("points = {0}", ds.Length);

            Save(ds, OriginalOutFilename);
        }

        [TestMethod]
        public void SortTest()
        {
            var ds = Load(Filename);
            var remover = new StatisticalSpikeRemover(ds);
            remover.PeakPeaks();
            remover.Sort();

            var points = remover.SortedIntensity;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Assert.IsTrue(points[i].Intensity <= points[i + 1].Intensity);
            }
        }

        [TestMethod]
        public void CleanupTest()
        {
            var ds = Load(Filename);
            var remover = new StatisticalSpikeRemover(ds);
            remover.PeakPeaks();
            remover.Sort();
            remover.Cleanup(true);

            //var points = remover.SortedIntensity;

            StatisticalSpikeRemover.Save(SortedOutFilename, remover.SortedIntensity);
            StatisticalSpikeRemover.Save(StdOutFilename, remover.Std);
            StatisticalSpikeRemover.Save(IncOutFilename, remover.Increments);


            Save(ds, ProcessedOutFilename);
        }


        [TestMethod]
        public void CleanupGoogTest()
        {
            var curDir = Directory.GetCurrentDirectory();
            var ds = Load(GoodFilename);
            var remover = new StatisticalSpikeRemover(ds);
            remover.PeakPeaks();
            remover.Sort();
            remover.Cleanup(true);

            //var points = remover.SortedIntensity;

            StatisticalSpikeRemover.Save(SortedOutFilename, remover.SortedIntensity);
            StatisticalSpikeRemover.Save(StdOutFilename, remover.Std);
            StatisticalSpikeRemover.Save(IncOutFilename, remover.Increments);


            Save(ds, ProcessedOutFilename);
        }


        [TestMethod]
        public void MedianTest()
        {
     
        }

        void Save(SimpleDataSource ds, string filename)
        {
            var file = new StreamWriter(filename);

            for (int i = 0; i < ds.Length; i++)
            {
                file.WriteLine("{0}\t{1}", ds.Xvals[i], ds.Yvals[i]);
            }

            file.Close();
        }

        SimpleDataSource Load(string filename)
        {
            var xs = new List<double>();
            var ys = new List<double>();

            var file = new StreamReader(filename);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var parts = line.Split('\t');

                    if (parts.Length == 2)
                    {
                        double x = 0;
                        double y = 0;
                        if (double.TryParse(parts[0], out x) && double.TryParse(parts[1], out y))
                        {
                            xs.Add(x);
                            ys.Add(y);
                        }

                    }
                }
            }

            file.Close();

            return new SimpleDataSource(xs.ToArray(), ys.ToArray());

        }

    }
}

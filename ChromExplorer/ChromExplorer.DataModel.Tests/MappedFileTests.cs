using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{
    [TestClass]
    public class MappedFileTests
    {
        private int _spectrums = 500;
        private int _points = 1000;
        private double _ppd = 25;
        private double _mFrom = 200;

        /*
        [TestMethod]
        public void CreateFileTest()
        {
           
            var timer = Stopwatch.StartNew();
            
           
            Write("chrom.dat");
            Console.WriteLine("File: {0} - {1} ms", _spectrums * _points, timer.ElapsedMilliseconds);
        }
        */
        [TestMethod]
        public void WriteReadSmallTest()
        {
           
            _spectrums = 3;
            _points = 5;

            var chromPoints = new List<ChromatogramPoint>();
            
            for(int i = 0 ; i < _spectrums; i++)
            {
                chromPoints.Add(new ChromatogramPoint() { Index = i, SpectrumID = i, TIC = 0.0, Time = i, TimeUnits = "au",
                                                          SpectrumMeta = new FileMappedSpectrumData.SpectrumMetaData() { EllapsedMilliseconds = i, TIC = 1000 + i }
                });
            }

            Write("small.dat");

            Thread.Sleep(100);

            var data = new FileMappedSpectrumData("small.dat");
            int id = 1;
            for(int i = 0; i < _spectrums; i++)
            {
                for(int j = 0; j < _points; j++)
                {
                    //Assert.AreEqual(data.GetValue(i, j), id++);
                    Console.WriteLine("Read: S = {0}, P = {1}, Data = {2}", i, j, data.GetValue(i, j));
                    //Console.WriteLine();
                }
            }

            var spectrum = data.GetSpectrum(0);

            Assert.AreEqual(spectrum.Points.Yvals[0], 1);

            spectrum = data.GetSpectrum(2);

            Assert.AreEqual(spectrum.Points.Yvals[3], 14);

            spectrum = data.GetSpectrum(0);

            Assert.AreEqual(spectrum.Points.Yvals[4], 5);

            var value = 1;
            
            foreach (var s in data)
            {
                Assert.AreEqual(s.Points.Yvals[0], value);
                value += 5;
            }

            var aggergated = data.GetAggregateSpectrum(new int[] {0, 1});

            Assert.AreEqual(aggergated.Points.Yvals[0], 7);

            var chrom = data.Rebuild(new int[] {0}, chromPoints);
            Assert.AreEqual(chrom.Length, 3);
            Assert.AreEqual(chrom.Yvals[0], 1);
            Assert.AreEqual(chrom.Yvals[1], 6);
            Assert.AreEqual(chrom.Yvals[2], 11);

            chrom = data.Rebuild(new int[] { 0, 1 }, chromPoints);
            Assert.AreEqual(chrom.Length, 3);
            Assert.AreEqual(chrom.Yvals[0], 3);
            Assert.AreEqual(chrom.Yvals[1], 13);
            Assert.AreEqual(chrom.Yvals[2], 23);
        }

        private void Write(string filename)
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
                            Console.WriteLine("Write: S = {0}, P = {1}, Data = {2}", i, j, value);
                        }
                    }

                    writer.Close();
                }
            }
        }

    }
}

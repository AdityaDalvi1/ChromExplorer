using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.PeakPicking;

namespace ChromExplorer.DataModel.Formats
{


    public class NistTextSpectrum
    {
        private const int NameLengt = 511;
        private const int CommentLength = 1023;
        private const int PointsPerLine = 4;
        private bool _isInitialized = false;



        public NistTextSpectrum()
        {
            Points = new List<Point>();
        }

        public string Name { get; set; }
        public string Comment { get; set; }
        public string Formula { get; set; }
        public double MW { get; set; }
        public string CAS { get; set; }
        public string Synonym { get; set; }

        public List<Point> Points { get; }

        public void Write(TextWriter writer)
        {
            if (!_isInitialized)
            {
                writer.WriteLine("NAME: Empty Spectrum");
                return;
            }

            writer.WriteLine($"NAME: {Name}");
            writer.WriteLine($"COMMENT: {Comment}");
            writer.WriteLine($"FORMULA: {Formula}");

            writer.WriteLine($"CAS: {CAS}");
            writer.WriteLine($"SYNONYM: {Synonym}");
            writer.WriteLine($"Num Peaks: {Points.Count}");

            int counter = 0;
            foreach (var point in Points)
            {
                writer.Write($"{Math.Round(point.X):0} {Math.Round(point.Y):0}; ");

                counter++;
                if(counter > 1 && counter % PointsPerLine == 0)
                    writer.WriteLine();
            }


        }

        public void Init(GenericChromatogram chrom, List<PeakInfo> peaks)
        {
            // (chrom?.CurrentSpectrum == null)
            //    return;

            Name = chrom.Title;
            ChromFilename = chrom.Filename;
            Comment = chrom.SelectedIndexes == null
                ? $"Index {chrom.SelectedIndex}"
                : $"Averaged spectrum, indexes - {string.Join(",", chrom.SelectedIndexes)}";

            Points.Clear();
            foreach (var peak in peaks)
            {
                Points.Add(new Point(peak.Position, peak.Intensity));
            }

            _isInitialized = true;
        }

        public string ChromFilename { get; set; }
    }
}
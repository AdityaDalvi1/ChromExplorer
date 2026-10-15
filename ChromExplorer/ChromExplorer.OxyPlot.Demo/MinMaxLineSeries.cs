using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChromExplorer.Infrastructure;
using OxyPlot;

namespace ChromExplorer.OxyPlot.Demo
{
    public class MinMaxLineSeries : LineSeries
    {
        public MinMaxLineSeries() : base()
        {
            DataRangeX = new DataRange(double.MinValue, double.MaxValue);
            YRangeExpansion = 1.1;
        }

        public double YRangeExpansion { get; set; }

        public void InvalidateData()
        {
            if (Points.Count == 0)
            {
                DataRangeX = new DataRange(double.MinValue, double.MaxValue);
                return;
            }

            double min = Points[0].X;
            double max = Points[0].X;

            foreach(var p in Points)
            {
                if (p.X < min)
                    min = p.X;

                if (p.X > max)
                    max = p.X;
            }

            DataRangeX = new DataRange(min, max);
        }

        public DataRange DataRangeX { get; private set; }
        public DataRange GetYRange(double xMin, double xMax)
        {
            var xRange = new DataRange(xMin, xMax);

            double min = double.MaxValue;
            double max = double.MinValue;
            int count = 0;

            foreach (var p in Points)
            {
                if (!xRange.Contains(p.X))
                    continue;

                if (p.Y < min)
                    min = p.Y;

                if (p.Y > max)
                    max = p.Y;

                count++;
            }

            if (count > 0)
                return new DataRange(min, max * YRangeExpansion);


            return new DataRange(0, 1);
        }


        public double Integrate(double min = double.MinValue, double max = double.MaxValue)
        {
            if (Points.Count < 2)
                return 0;

            
            var start = GetIndexByX(min);
            var stop = GetIndexByX(max);

            var integral = 0.0;
            for (var i = start + 1; i <= stop; i++)
            {
                integral += (Points[i].Y + Points[i - 1].Y) / 2.0 * (Points[i].X - Points[i - 1].X);
            }
            return integral;
        }

        private int GetIndexByX(double x)
        {
            int index = Points.BinarySearch(new DataPoint(x, 0)
                , new ComparisonComparer<DataPoint>((a, b) => a.X.CompareTo(b.X)));

            if (index < 0)
                index = ~index;

            if (index == Points.Count)
                index = Points.Count - 1;

            return index;
            
        }

        public void ExportToCsvFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                WriteData(writer, ",");
            }
        }

        public void WriteData(StreamWriter writer, string s)
        {
            foreach (var point in Points)
            {
                writer.WriteLine($"{point.X}{s}{point.Y}");
            }
        }
    }
}

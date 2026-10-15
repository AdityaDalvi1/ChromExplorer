using System;
using System.Collections.Generic;
using System.Windows;

namespace ChromExplorer.DataModel.Core
{
    public class SimpleDataSource
    {
        //xvals must be ordered
        public SimpleDataSource(double[] xvals, double[] yvals)
        {
            Xvals = xvals;
            Yvals = yvals;

            Points = new List<Point>();

            var s = Xvals.Length;
            for (var i = 0; i < s; i++)
            {
                Points.Add(new Point(Xvals[i], Yvals[i]));
            }

            UpdateBounds();
        }

        public SimpleDataSource(double[] xvals, int[] data)
        {
            Xvals = xvals;
            Yvals = new double[data.Length];

            for (var i = 0; i < Yvals.Length; i++)
            {
                Yvals[i] = data[i];
            }

            Points = new List<Point>();

            var s = Xvals.Length;
            for (var i = 0; i < s; i++)
            {
                Points.Add(new Point(Xvals[i], Yvals[i]));
            }

            UpdateBounds();
        }

        public double[] Xvals { get; }

        public double[] Yvals { get; }

        public int Length => Xvals?.Length ?? 0;

        public DataRect Bounds { get; private set; }

        public List<Point> Points { get; }

        public int GetIndexByX(double x)
        {
            var index = Array.BinarySearch(Xvals, x);

            if (index < 0)
                index = ~index;

            if (index == Xvals.Length)
                index = Xvals.Length - 1;

            return index;
        }

        public void UpdateBounds()
        {
            Bounds = new DataRect(this);
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
                integral += (Points[i].Y + Points[i - 1].Y)/2.0*(Points[i].X - Points[i - 1].X);
            }
            return integral;
        }
    }
}
using System;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.DataModel.Resampling
{
    public class LinearInterpolationResampler : IUniformResampler
    {
        private const int MinimalSize = 10;

        public double Step { get; set; }
        public double XMin  { get; set; }
        public double XMax { get; set; }
        public double? Default { get; set; }

        public double[] XVals { get; set; }

        public void InitXVals()
        {

            var start = XMin;
            var end = XMax;
            var size = (int)((end - start) / Step);
            XVals = new double[size];

            for (var i = 0; i < size; i++)
            {
                XVals[i] = start + i * this.Step;
            }
        }

        public double[] GetYVals(SimpleDataSource dataSource)
        {
             if(dataSource == null || dataSource.Length < MinimalSize)
                 return null;

            if (Step <= 0.0)
                return dataSource.Yvals;

            if(XVals == null)
                return dataSource.Yvals;
            
            var leftPad = dataSource.Yvals[0];
            var rightPad = dataSource.Yvals[dataSource.Length - 1];

            var xmin = dataSource.Xvals[0];
            var xmax = dataSource.Xvals[dataSource.Length - 1];
            
            if(Default.HasValue)
            {
                leftPad = Default.Value;
                rightPad = Default.Value;
            }

            var start = XMin;
            var end = XMax;
            var size = (int)((end - start) / Step);

            if (size < MinimalSize)
                return dataSource.Yvals;
            
            var y = new double[size];


            for (var i = 0; i < size; i++)
            {
                if (XVals[i] <= xmin)
                {
                    y[i] = leftPad;
                }
                else if (XVals[i] >= xmax)
                {
                    y[i] = rightPad;
                }
                else
                {
                    y[i] = GetValue(dataSource, XVals[i]);
                }
            }

            return y;
        }

        public int[] GetYValsAsInts(SimpleDataSource dataSource)
        {
            var doubles = GetYVals(dataSource);

            int[] retval = new int[doubles.Length];

            for (int i = 0; i < retval.Length; i++)
                retval[i] = (int)doubles[i];

            return retval;
        }

        public SimpleDataSource Resample(SimpleDataSource dataSource)
        {
            if(dataSource == null || dataSource.Length < MinimalSize)
                return dataSource;

            if (Step <= 0.0)
                return dataSource;
            
            var leftPad = dataSource.Yvals[0];
            var rightPad = dataSource.Yvals[dataSource.Length - 1];

            var xmin = dataSource.Xvals[0];
            var xmax = dataSource.Xvals[dataSource.Length - 1];
            
            if(Default.HasValue)
            {
                leftPad = Default.Value;
                rightPad = Default.Value;
            }

            var start = XMin;
            var end = XMax;
            var size = (int)((end - start) / Step);

            if (size < MinimalSize)
                return dataSource;

            var x = new double[size];
            var y = new double[size];

            for (var i = 0; i < size; i++)
            {
                x[i] = start + i * this.Step;
            }

            for (var i = 0; i < size; i++)
            {
                if (x[i] <= xmin)
                {
                    y[i] = leftPad;
                }
                else if (x[i] >= xmax)
                {
                    y[i] = rightPad;
                }
                else
                {
                    y[i] = GetValue(dataSource, x[i]);
                }
            }

            return new SimpleDataSource(x, y);
        }

        private double GetValue(SimpleDataSource values, double x)
        {
            var value = 0.0;

            var index = Array.BinarySearch(values.Xvals, x);

            if (index >= 0)
            {
                value = values.Yvals[index];
            }
            else
            {
                index = ~index;

                if (index < values.Length)
                {

                    var leftX = values.Xvals[index - 1];
                    var rightX = values.Xvals[index];

                    var leftY = values.Yvals[index - 1];
                    var rightY = values.Yvals[index];

                    value = leftY + (x - leftX)*(rightY - leftY)/(rightX - leftX);
                }
            }

            return value;
        }

    }
}

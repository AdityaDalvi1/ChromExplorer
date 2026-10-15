using System;
using System.Collections.Generic;
using ChromExplorer.DataModel.Helpres;

namespace ChromExplorer.DataModel.PeakPicking
{
    public class CentroidPeakAnalyzer : IPeakAnalyzer
    {
        double[] _xs = null;
        double[] _ys = null;

        double left_a;
        double left_b;

        double right_a;
        double right_b;

        double xLeft;
        double xRight;

        double position;
        double height;
        double width;

        public PeakInfo Analyze(List<SpectrumDataPoint> points, int left, int right)
        {
            List<double> xs = new List<double>();
            List<double> ys = new List<double>();

            for (int i = left; i <= right; i++)
            {
                xs.Add(points[i].X);
                ys.Add(points[i].Y);
            }

            _xs = xs.ToArray();
            _ys = ys.ToArray();

            height = EstimateHeight();
            position = EstimatePosition();
            width = EstimateWidth();

            return new PeakInfo() { Position = position, Intensity = height, HWHM = width / 2.0, LeftIndex = left, RightIndex = right };
        }

        private double EstimateHeight()
        {
            if (_xs == null || _ys == null)
                return 0.0;

            return MinMaxFinder<double>.MinMax(_ys).Max;
        }

        private double EstimatePosition()
        {
            try
            {
                if (_xs == null || _ys == null)
                    return 0.0;

                MinMax<double> peak = MinMaxFinder<double>.MinMax(_ys);

                int peakIndex = peak.MaxIndex;
                double halfMax = peak.Max / 2.0;

                if (peakIndex == 0)
                    return 0.0;

                int l_upperIndex = peakIndex;
                int l_lowerIndex = peakIndex - 1;

                while (_ys[l_lowerIndex] > halfMax && l_lowerIndex > 0)
                {
                    l_lowerIndex--;
                }

                if (peakIndex == _xs.Length - 1)
                    return 0.0;

                int r_upperIndex = peakIndex;
                int r_lowerIndex = peakIndex + 1;

                while (_ys[r_lowerIndex] > halfMax && r_lowerIndex < _xs.Length - 1)
                {
                    r_lowerIndex++;
                }

                int peakSize = r_lowerIndex - l_lowerIndex + 1;

                //Not enough points for build centroid
                //if (peakSize <= 3)
                //    return _xs[peak.MaxIndex];



                l_upperIndex = l_lowerIndex + (r_lowerIndex - l_lowerIndex) / 2;

                /*
                double l_sum_x = 0.0;
                double r_sum_x = 0.0;

                double l_sum_y = 0.0;
                double r_sum_y = 0.0;

                for (int i = l_lowerIndex; i < l_upperIndex; i++)
                {
                    l_sum_x += (_xs[i] + _xs[i + 1]) / 2.0 * (_ys[i + 1] - _ys[i]);
                    l_sum_y += (_ys[i + 1] - _ys[i]);
                }

                for (int i = l_upperIndex + 2; i <= r_lowerIndex; i++)
                {
                    r_sum_x += (_xs[i] + _xs[i - 1]) / 2.0 * (_ys[i - 1] - _ys[i]);
                    r_sum_y += (_ys[i - 1] - _ys[i]);

                }

                return (l_sum_x + r_sum_x) / (l_sum_y + r_sum_y);
                 * */

                double s1 = 0.0;
                double s2 = 0.0;
                for (int i = l_lowerIndex; i <= r_lowerIndex; i++)
                {
                    s1 += _xs[i] * _ys[i];
                    s2 += _ys[i];
                }

                return s1 / s2;

            }
            catch (Exception ex)
            {
                //TODO processing here
            }
            return 0.0;
        }

        private double EstimateWidth()
        {
            try
            {
                if (_xs == null || _ys == null)
                    return 0.0;

                MinMax<double> peak = MinMaxFinder<double>.MinMax(_ys);

                int peakIndex = peak.MaxIndex;
                double halfMax = peak.Max / 2.0;

                if (peakIndex == 0)
                    return 0.0;

                int upperIndex = peakIndex;
                int lowerIndex = peakIndex - 1;

                while (_ys[lowerIndex] > halfMax && lowerIndex > 0)
                {
                    upperIndex--;
                    lowerIndex--;
                }

                double a = (_ys[upperIndex] - _ys[lowerIndex]) /
                             (_xs[upperIndex] - _xs[lowerIndex]);

                double b = _ys[upperIndex] - a * _xs[upperIndex];

                left_a = a;
                left_b = b;


                xLeft = (halfMax - b) / a;

                if (peakIndex == _xs.Length - 1)
                    return 0.0;

                upperIndex = peakIndex;
                lowerIndex = peakIndex + 1;

                while (_ys[lowerIndex] > halfMax && lowerIndex < _xs.Length - 1)
                {
                    upperIndex++;
                    lowerIndex++;
                }

                a = (_ys[upperIndex] - _ys[lowerIndex]) /
                (_xs[upperIndex] - _xs[lowerIndex]);

                b = _ys[upperIndex] - a * _xs[upperIndex];

                right_a = a;
                right_b = b;

                xRight = (halfMax - b) / a;


                return Math.Abs(xRight - xLeft);
            }
            catch (Exception ex)
            {
                //TODO Processing here
            }
            return 0.0;
        }

    }
}

  


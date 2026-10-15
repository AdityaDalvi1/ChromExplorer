using System.Collections.Generic;

namespace ChromExplorer.DataModel.PeakPicking
{
    public class SimplePeakAnalyzer : IPeakAnalyzer
    {

        public PeakInfo Analyze(List<SpectrumDataPoint> points, int left, int right)
        {
            double max = points[left].Y;
            int max_index = left;

            for (int i = left; i < right; i++)
            {
                if (points[i].Y > max)
                {
                    max = points[i].Y;
                    max_index = i;
                }
            }

            return new PeakInfo() { Position = points[max_index].X, Intensity = points[max_index].Y, LeftIndex = left, RightIndex = right };


        }
    }
}

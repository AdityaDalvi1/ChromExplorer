using System.Collections.Generic;

namespace ChromExplorer.DataModel.PeakPicking
{
    public interface IPeakAnalyzer
    {
        PeakInfo Analyze(List<SpectrumDataPoint> points, int left, int right);
    }
}

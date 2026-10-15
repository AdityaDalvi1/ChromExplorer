using System.Collections.Generic;
using System.Windows.Controls;
using ChromExplorer.DataModel.PeakPicking;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public enum PeakAnalyzers
    {
        Simple,
        Centroid,
    }

    public enum ThresholdTypes
    {
        Absolute,
        Percentage
    }

    public interface IPeakPicker : IResourceDescriptor
    {
        List<PeakInfo> FindPeaks<T>(double[] xs, double[] ys, double min, double max, double threshold,
                                         bool percentage) where T : IPeakAnalyzer, new();

        UserControl ConfigView { get; }
    }
}

namespace ChromExplorer.OxyPlot.Demo
{
    public interface ISpectrumXRangeAware
    {
        void AddRange(SpectrumXRangeAnnotation annotation);
        void UpdateRange(SpectrumXRangeAnnotation annotation);
        void RemoveAllRanges();

        void RemoveRangeAt(double pos);
    }
}
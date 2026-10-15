using OxyPlot.Annotations;

namespace ChromExplorer.OxyPlot.Demo
{
    public interface IXRangeAware
    {
        void Add(XRangeAnnotation annotation);
        void UpdateRange(XRangeAnnotation annotation);
        void RemoveAll();

        void RemoveAt(double pos);
    }
}   
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.DataModel.Spectrum
{
    public enum SpectrumType
    {
        MS,
        MSMS
    }

    public interface ISpectrum
    {
        SimpleDataSource Points { get; }

        double TIC { get; }
        double Max { get; }
        double Min { get; }

        SpectrumType Type { get; }
        double Signal { get; }
        
        double SignalToNoise { get; }
        double ParentIon { get; }
        int ID { get; }
        int Index { get; }
        bool BaselineRemoved { get; }

        void UpdateStatistics();
    }
}

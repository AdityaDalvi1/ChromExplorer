using System.Collections.Generic;

namespace ChromExplorer.DataModel.Spectrum
{
    public interface ISpectrumCollection : IList<MsSpectrum>
    {
        int SpectrumSize { get; }
        IEnumerable<double> Positions { get; }
        
        MsSpectrum GetAggregateSpectrum(IEnumerable<int> indexes);
    }
}

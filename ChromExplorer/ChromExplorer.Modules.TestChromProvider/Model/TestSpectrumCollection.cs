using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Spectrum;

namespace ChromExplorer.Modules.TestChromProvider.Model
{
    public class TestSpectrumCollection : Collection<MsSpectrum>, ISpectrumCollection
    {
        public TestSpectrumCollection()
        {
            
        }

        public int SpectrumSize { get; set; }
        public IEnumerable<double> Positions { get; set; }

        public MsSpectrum GetAggregateSpectrum(IEnumerable<int> indexes)
        {
            if (indexes == null)
                return null;

            var aggregated = new double[this.SpectrumSize];
           
            foreach (var index in indexes)
            {
                if(index >= 0 && index < this.Count)
                {
                    for(int i = 0; i < this.SpectrumSize; i++)
                    {
                        aggregated[i] += this[index].Points.Yvals[i];
                    }
                }
            }

            return new MsSpectrum(new SimpleDataSource(this.Positions.ToArray(), aggregated));
        }

      
    }
}

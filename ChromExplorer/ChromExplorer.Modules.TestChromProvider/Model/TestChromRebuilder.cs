using System.Collections.Generic;
using System.Linq;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.Modules.TestChromProvider.Model
{
    public class TestChromRebuilder : IChromRebuilder
    {
        private readonly TestSpectrumCollection _spectrums;
        public TestChromRebuilder(TestSpectrumCollection spectrums)
        {
            _spectrums = spectrums;
        }

        public SimpleDataSource Rebuild(IList<int> indexes, IList<ChromatogramPoint> oldPoints)
        {
            int size = oldPoints.Count();
            var x = new double[size];
            var y = new double[size];

            int i = 0;

            foreach (var point in oldPoints)
            {
                x[i] = point.Time;
                y[i] = 0.0;

                if (indexes != null)
                {

                    foreach (var index in indexes)
                    {
                        if (_spectrums != null)
                        {
                            y[i] += _spectrums[i].Points.Yvals[index];
                        }
                    }
                }
                else
                {
                    y[i] = point.TIC;
                }

                i++;
            }
    

            return new SimpleDataSource(x, y);
        }

         
    }
}

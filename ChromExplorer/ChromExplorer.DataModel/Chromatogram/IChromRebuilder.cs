using System.Collections.Generic;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.DataModel.Chromatogram
{
    public interface IChromRebuilder
    {
        SimpleDataSource Rebuild(IList<int> indexes, IList<ChromatogramPoint> points);
    }
}

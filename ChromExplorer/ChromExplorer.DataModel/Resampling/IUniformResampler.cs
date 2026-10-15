using ChromExplorer.DataModel.Core;

namespace ChromExplorer.DataModel.Resampling
{
    public interface IUniformResampler
    {
        double Step { get; set; }
        double XMin { get; set; }
        double XMax { get; set; }
        double? Default { get; set; }
        double[] XVals { get; }

        void InitXVals();
        SimpleDataSource Resample(SimpleDataSource dataSource);
        double[] GetYVals(SimpleDataSource dataSource);


        int[] GetYValsAsInts(SimpleDataSource dataSource);
    }
}

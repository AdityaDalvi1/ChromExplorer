using ChromExplorer.DataModel.Core;

namespace ChromExplorer.DataModel.Resampling
{
    public class EmptyResampler : IUniformResampler
    {
        #region Implementation of IUniformResampler

        public double Step { get; set; }
        public double XMin { get; set; }
        public double XMax { get; set; }
        public double? Default { get; set; }
        public double[] XVals { get; private set; }
        public void InitXVals()
        {
            var start = XMin;
            var end = XMax;
            var size = (int)((end - start) / Step);
            XVals = new double[size];

            for (var i = 0; i < size; i++) {
                XVals[i] = start + i * this.Step;
            }
        }

        public SimpleDataSource Resample(SimpleDataSource dataSource)
        {
            return dataSource;
        }

        public double[] GetYVals(SimpleDataSource dataSource)
        {
            var y = new double[XVals.Length];

            for (int i = 0; i < y.Length; i++) {
                
                y[i] = i < dataSource.Length ? dataSource.Yvals[i] : 0;
            }

            return y;

        }

        public int[] GetYValsAsInts(SimpleDataSource dataSource)
        {
            var y = new int[dataSource.Length];

            for (int i = 0; i < dataSource.Length; i++) {
                y[i] = (int)dataSource.Yvals[i];
            }

            return y;
        }

        #endregion
    }
}

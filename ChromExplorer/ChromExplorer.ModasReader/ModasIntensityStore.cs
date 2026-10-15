using ChromExplorer.DataModel;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChromExplorer.ModasReader
{
    public class ModasIntensityStore : IDisposable
    {
        //private readonly SpectrumStore[] _spectrums;
        public ModasIntensityStore(int size)
        {
            Spectrums = new List<ModasSpectrumStore>(size);
        }

        public List<ModasSpectrumStore> Spectrums;

        public double GetValue(int spectrum, int point)
        {
            return Spectrums[spectrum].Data[point];
        }

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ModasIntensityStore()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Spectrums.Clear();
            }

            //free unmanaged resources
        }
        #endregion
    }

    public class ModasSpectrumStore
    {
        //private readonly AGCSpectrumEstimator _estimator = new AGCSpectrumEstimator();
        private readonly ModasSpectrumEstimator _estimator = new ModasSpectrumEstimator();
        public ModasSpectrumStore(double[] data)
        {
            Data = data;
        }

        public double[] Data;

        public double GetTicAndRefine(double normFactor, double sigmaMult)
        {
            var bkg = new double[Data.Length];
            var refined = new double[Data.Length];
            double sic;
            _estimator.SigmaMult = sigmaMult;
            //IList<double>) this.Data, (double[])null, refined, (IList<double>)null, this.Data.Length, (DataRect)null, out double _, false
            var tic = _estimator.IntegrateUsefulSignalVersion2(Data, null, refined, null, Data.Length, null, out sic, false);

            for (int i = 0; i < Data.Length; i++) {
                Data[i] = refined[i] * normFactor;
            }
            
            return tic * normFactor;
        }

        public double GetTic(double normFactor)
        {
            double sum = _estimator.IntegrateUsefulSignalVersion2(Data);

            return sum * normFactor;
        }

        public void RemoveBaseline()
        {
            var min = Data.Min();

            for(int i = 0; i < Data.Length; i++)
            {
                Data[i] -= min;
            }
        }
    }

}

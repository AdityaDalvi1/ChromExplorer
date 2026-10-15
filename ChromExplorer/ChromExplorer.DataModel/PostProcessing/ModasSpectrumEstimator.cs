using ChromExplorer.DataModel.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.DataModel.PostProcessing
{
    public class ModasSpectrumEstimator
    {
        private BaselineEstimator _estimator;

        public ModasSpectrumEstimator()
        {
            this.QuantSize = 512 /*0x0200*/;
            this.WindowSize = 5;
            this.Sigma = 1.0;
            this.SigmaMult = 3.0;
        }

        public int QuantSize { get; set; }

        public int WindowSize { get; set; }

        public double Sigma { get; set; }

        public double SigmaMult { get; set; }

        public double IntegrateUsefulSignalVersion2(IList<double> y)
        {
            return this.IntegrateUsefulSignalVersion2(y, (double[])null, (double[])null, (IList<double>)null, y.Count, (DataRect)null, out double _, false);
        }

        public double IntegrateUsefulSignalVersion2(
          IList<double> y,
          double[] baseline,
          double[] refined,
          IList<double> mz,
          int count,
          DataRect zoomRect,
          out double sic,
          bool normBySigma)
        {
            if (this._estimator == null || this._estimator.X.Length != count || this._estimator.BinSize != this.QuantSize || this._estimator.WindowSize != this.WindowSize)
                this._estimator = new BaselineEstimator(count, this.QuantSize, this.WindowSize);
            this._estimator.NoiseMult = this.SigmaMult;
            if (mz != null)
                this._estimator.Estimate(mz.ToArray<double>(), y.ToArray<double>());
            else
                this._estimator.Estimate(new double[count], y.ToArray<double>());
            if (zoomRect != null)
                this._estimator.SetSicRange(zoomRect.XMin, zoomRect.XMax);
            double num = normBySigma ? 1.0 / this._estimator.Sigma : 1.0;
            if (refined != null)
            {
                for (int index = 0; index < refined.Length; ++index)
                    refined[index] = num * this._estimator.YRefined[index];
            }
            sic = zoomRect == null ? 0.0 : this._estimator.Sic;
            this.Sigma = this._estimator.Sigma;
            return this._estimator.Tic;
        }

    }


}

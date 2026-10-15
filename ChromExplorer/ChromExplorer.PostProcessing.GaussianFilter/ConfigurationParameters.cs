using System.ComponentModel;

namespace ChromExplorer.PostProcessing.GaussianFilter
{
    public class ConfigurationParameters
    {
        double _sigma;
        int _kernelSize;

        public ConfigurationParameters()
        {
            _sigma = 1.0;
            _kernelSize = 5;
            IsActive = false;
        }

        public ConfigurationParameters(double sigma, int kernelSize)
        {
            _sigma = sigma;
            _kernelSize = kernelSize;
            IsActive = false;
        }

        public static ConfigurationParameters DefaultValue
        {
            get
            {
                return new ConfigurationParameters();
            }
        }

        [Category("General"), Description("Gaussian width (sigma)"), DefaultValue(1.0)]
        public double Sigma
        {
            get { return _sigma; }
            set { _sigma = value; }
        }

        [Category("General"), Description("Kernel size (window range)"), DefaultValue(5)]
        public int KernelSize
        {
            get { return _kernelSize; }
            set { _kernelSize = value; }
        }

        public bool IsActive { get; set; }

        public void Save()
        {
            // For now, persistence is not implemented without .settings files
        }
    }
}

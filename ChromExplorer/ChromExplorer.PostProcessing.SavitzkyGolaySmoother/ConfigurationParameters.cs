using System.ComponentModel;

namespace ChromExplorer.PostProcessing.SavitzkyGolaySmoother
{
    public class ConfigurationParameters
    {
        int _m;
        int _nL;
        int _nR;

        public ConfigurationParameters()
        {
            _m = Properties.Settings.Default.M;
            _nL = Properties.Settings.Default.nL;
            _nR = Properties.Settings.Default.nR;
            IsActive = Properties.Settings.Default.isActive;
        }

        public ConfigurationParameters(int m, int nl, int nr)
        {
            _m = m;
            _nL = nl;
            _nR = nr;
            IsActive = false;
        }

        public static ConfigurationParameters DefaultValue
        {
            get
            {
                return new ConfigurationParameters();
            }
        }

        [Category("General"), Description("Polynom power"), DefaultValue(2)]
        public int M
        {
            get
            {
                return _m;
            }

            set
            {
                _m = value;
            }
        }

        [Category("General"), Description("Number of points used to the left of data point"), DefaultValue(2)]
        public int nL
        {
            get
            {
                return _nL;
            }

            set
            {
                _nL = value;
            }
        }

        [Category("General"), Description("Number of points used to the right of data point"), DefaultValue(2)]
        public int nR
        {
            get
            {
                return _nR;
            }

            set
            {
                _nR = value;
            }
        }

        public bool IsActive { get; set; }


        public void Save()
        {
            Properties.Settings.Default.M = M;
            Properties.Settings.Default.nL = nL;
            Properties.Settings.Default.nR = nR;
            Properties.Settings.Default.isActive = IsActive;
            Properties.Settings.Default.Save();
        }


    }


}

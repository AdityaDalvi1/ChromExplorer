using System.ComponentModel;
using ChromExplorer.DataModel.Properties;

namespace ChromExplorer.DataModel.Builders
{
    public class WaveformConfigBase : INotifyPropertyChanged
    {
        private double _amplitude;
        private double _frequency;
        private double _offset;
        private double _phase;
        private int _samples;
        private double _samplingRate;

        public double SamplingRate
        {
            get { return _samplingRate; }
            set
            {
                if (value.Equals(_samplingRate)) return;
                _samplingRate = value;
                OnPropertyChanged("SamplingRate");
            }
        }

        public double Frequency
        {
            get { return _frequency; }
            set
            {
                if (value.Equals(_frequency)) return;
                _frequency = value;
                OnPropertyChanged("Frequency");
            }
        }

        public double Phase
        {
            get { return _phase; }
            set
            {
                if (value.Equals(_phase)) return;
                _phase = value;
                OnPropertyChanged("Phase");
            }
        }

        public double Amplitude
        {
            get { return _amplitude; }
            set
            {
                if (value.Equals(_amplitude)) return;
                _amplitude = value;
                OnPropertyChanged("Amplitude");
            }
        }

        public double Offset
        {
            get { return _offset; }
            set
            {
                if (value.Equals(_offset)) return;
                _offset = value;
                OnPropertyChanged("Offset");
            }
        }

        public int Samples
        {
            get { return _samples; }
            set
            {
                if (value == _samples) return;
                _samples = value;
                OnPropertyChanged("Samples");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
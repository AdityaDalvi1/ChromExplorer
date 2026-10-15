using System;
using System.Collections.Generic;
using System.ComponentModel;
using ChromExplorer.DataModel.DAL;
using ChromExplorer.DataModel.Properties;

namespace ChromExplorer.DataModel.Chromatogram
{
    public class ChromatogramPoint : INotifyPropertyChanged
    {
        private int _methodID;
        private double _mFrom;
        private double _mTo;
        private int _calibrationID;
        private string _timeUnits;
        private int _index;
        private double _time;
        private double _tic;
        private int _spectrumID;
        private int _spectrumSize;
        private FileMappedSpectrumData.SpectrumMetaData _spectrumMeta;
        private double _digitizerSampleRate;
        private DateTime _timestamp;
        private List<int> _indexes = new List<int>();

        public ChromatogramPoint()
        {

        }
        public ChromatogramPoint(ChromatogramPoint chromPoint)
        {
            _methodID = chromPoint._methodID;
            _mFrom = chromPoint._mFrom;
            _mTo = chromPoint._mTo;
            _calibrationID = chromPoint._calibrationID;
            _timeUnits = chromPoint._timeUnits;
            _index = chromPoint._index;
            _time = chromPoint._time;
            _tic = chromPoint._tic;
            _spectrumID = chromPoint._spectrumID;
            _spectrumSize = chromPoint._spectrumSize;
            _spectrumMeta = new FileMappedSpectrumData.SpectrumMetaData(chromPoint._spectrumMeta);
            _digitizerSampleRate = chromPoint._digitizerSampleRate;
            _timestamp = chromPoint._timestamp;
        }

        public int MethodID
        {
            get { return _methodID; }
            set
            {
                if (value == _methodID) return;
                _methodID = value;
                OnPropertyChanged("MethodID");
            }
        }

        // The scan mass range (from the acquisition Method) this point's spectrum was taken
        // over — e.g. MFrom=270, MTo=620. Provider-specific like MethodID; defaults to 0 for
        // providers/points that don't have this info.
        public double MFrom
        {
            get { return _mFrom; }
            set
            {
                if (value.Equals(_mFrom)) return;
                _mFrom = value;
                OnPropertyChanged("MFrom");
            }
        }

        public double MTo
        {
            get { return _mTo; }
            set
            {
                if (value.Equals(_mTo)) return;
                _mTo = value;
                OnPropertyChanged("MTo");
            }
        }

        public int CalibrationID
        {
            get { return _calibrationID; }
            set
            {
                if (value == _calibrationID) return;
                _calibrationID = value;
                OnPropertyChanged("CalibrationID");
            }
        }

        public string TimeUnits
        {
            get { return _timeUnits; }
            set
            {
                if (value == _timeUnits) return;
                _timeUnits = value;
                OnPropertyChanged("TimeUnits");
            }
        }

        public int Index
        {
            get { return _index; }
            set
            {
                if (value == _index) return;
                _index = value;
                OnPropertyChanged("Index");
            }
        }

        public double Time
        {
            get { return _time; }
            set
            {
                if (value.Equals(_time)) return;
                _time = value;
                OnPropertyChanged("Time");
            }
        }

        public double TIC
        {
            get { return _tic; }
            set
            {
                if (value.Equals(_tic)) return;
                _tic = value;
                OnPropertyChanged("TIC");
            }
        }

        public int SpectrumID
        {
            get { return _spectrumID; }
            set
            {
                if (value == _spectrumID) return;
                _spectrumID = value;
                OnPropertyChanged("SpectrumID");
            }
        }

        public int SpectrumSize
        {
            get { return _spectrumSize; }
            set
            {
                if (value == _spectrumSize) return;
                _spectrumSize = value;
                OnPropertyChanged("SpectrumSize");
            }
        }

        public FileMappedSpectrumData.SpectrumMetaData SpectrumMeta
        {
            get { return _spectrumMeta; }
            set
            {
                if (Equals(value, _spectrumMeta)) return;
                _spectrumMeta = value;
                OnPropertyChanged("SpectrumMeta");
            }
        }

        public double DigitizerSampleRate
        {
            get { return _digitizerSampleRate; }
            set
            {
                if (value.Equals(_digitizerSampleRate)) return;
                _digitizerSampleRate = value;
                OnPropertyChanged("DigitizerSampleRate");
            }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set
            {
                if (value.Equals(_timestamp)) return;
                _timestamp = value;
                OnPropertyChanged("Timestamp");
            }
        }

        public List<int> SpectrumIndexes
        {
            get => _indexes;
            set => _indexes = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

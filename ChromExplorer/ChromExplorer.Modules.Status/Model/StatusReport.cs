using System.ComponentModel;
using ChromExplorer.Modules.Status.Properties;

namespace ChromExplorer.Modules.Status.Model
{
    public class StatusReport : INotifyPropertyChanged
    {
        private string _appStatus;
        private double _progressMin;
        private double _progressMax;
        private double _currentProgress;

        public StatusReport()
        {
            ProgressMin = 0.0;
            ProgressMax = 100.0;
            CurrentProgress = 0.0;
        }

        public string AppStatus
        {
            get { return _appStatus; }
            set
            {
                if (value == _appStatus) return;
                _appStatus = value;
                OnPropertyChanged("AppStatus");
            }
        }

        public double ProgressMin
        {
            get { return _progressMin; }
            set
            {
                if (value.Equals(_progressMin)) return;
                _progressMin = value;
                OnPropertyChanged("ProgressMin");
            }
        }

        public double ProgressMax
        {
            get { return _progressMax; }
            set
            {
                if (value.Equals(_progressMax)) return;
                _progressMax = value;
                OnPropertyChanged("ProgressMax");
            }
        }

        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                if (value.Equals(_currentProgress)) return;
                _currentProgress = value;
                OnPropertyChanged("CurrentProgress");
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
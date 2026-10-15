using System.ComponentModel;
using ChromExplorer.DataModel.Properties;

namespace ChromExplorer.Infrastructure
{
    public interface IExportSettings
    {
        string Filename { get; }
    }

    public class MascotExportSettings : IExportSettings, INotifyPropertyChanged
    {
        private MassTypes _massType;
        private bool _selectedSpectraOnly;
        private bool _normalize;
        private double _normLevel;
        private bool _writeZero;
        private bool _shiftToZero;
        private string _filename;

        public MascotExportSettings()
        {
            MassType = MassTypes.Exact;
            NormLevel = 100.0;
            WriteZero = true;
        }

        public MassTypes MassType
        {
            get { return _massType; }
            set
            {
                if (value == _massType) return;
                _massType = value;
                OnPropertyChanged("MassType");
            }
        }

        public bool SelectedSpectraOnly
        {
            get { return _selectedSpectraOnly; }
            set
            {
                if (value.Equals(_selectedSpectraOnly)) return;
                _selectedSpectraOnly = value;
                OnPropertyChanged("SelectedSpectraOnly");
            }
        }

        public bool Normalize
        {
            get { return _normalize; }
            set
            {
                if (value.Equals(_normalize)) return;
                _normalize = value;
                OnPropertyChanged("Normalize");
            }
        }

        public double NormLevel
        {
            get { return _normLevel; }
            set
            {
                if (value.Equals(_normLevel)) return;
                _normLevel = value;
                OnPropertyChanged("NormLevel");
            }
        }

        public bool WriteZero
        {
            get { return _writeZero; }
            set
            {
                if (value.Equals(_writeZero)) return;
                _writeZero = value;
                OnPropertyChanged("WriteZero");
            }
        }

        public bool ShiftToZero
        {
            get { return _shiftToZero; }
            set
            {
                if (value.Equals(_shiftToZero)) return;
                _shiftToZero = value;
                OnPropertyChanged("ShiftToZero");
            }
        }

        public string Filename
        {
            get { return _filename; }
            set
            {
                if (value == _filename) return;
                _filename = value;
                OnPropertyChanged("Filename");
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
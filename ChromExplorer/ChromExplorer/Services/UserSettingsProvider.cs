using System.ComponentModel;
using System.ComponentModel.Composition;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Properties;

namespace ChromExplorer.Services
{
    public class ModasImportUserSettings : IModasImportUserSettings, INotifyPropertyChanged
    {
        private TimeUnits _timeUnits;
        private bool _rebuildTic;
        private double _ppd;
        private bool _removeBaseLine;
        private double _noiseFilterLevel;
        private bool _normalizeByIT;
        private double _referenceIT;
        public event PropertyChangedEventHandler PropertyChanged;

        public void ReadFromSettigns()
        {
            var settings = Settings.Default;

            TimeUnits = settings.TimeUnits;
            RebuildTic = settings.RebuildTic;
            PPD = settings.PPD;
            RemoveBaseLine = settings.RemoveBaseline;
            NoiseFilterLevel = settings.NoiseFilterLevel;
            NormalizeByIT = settings.NormalizeByIT;
            ReferenceIT = settings.ReferenceIT;
        }

        public void SaveToSettings()
        {
            Settings.Default.TimeUnits = TimeUnits;
            Settings.Default.RebuildTic = RebuildTic;
            Settings.Default.PPD = PPD;
            Settings.Default.RemoveBaseline = RemoveBaseLine;
            Settings.Default.NoiseFilterLevel = NoiseFilterLevel;
            Settings.Default.NormalizeByIT = NormalizeByIT;
            Settings.Default.ReferenceIT = ReferenceIT;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Implementation of IModasImportUserSettings

        public TimeUnits TimeUnits
        {
            get { return _timeUnits; }
            set
            {
                if (value == _timeUnits) return;
                _timeUnits = value;
                OnPropertyChanged("TimeUnits");
            }
        }

        public bool RebuildTic
        {
            get { return _rebuildTic; }
            set
            {
                if (value.Equals(_rebuildTic)) return;
                _rebuildTic = value;
                OnPropertyChanged("RebuildTic");
            }
        }

        public double PPD
        {
            get { return _ppd; }
            set
            {
                if (value.Equals(_ppd)) return;
                _ppd = value;
                OnPropertyChanged("PPD");
            }
        }

        public bool RemoveBaseLine
        {
            get { return _removeBaseLine; }
            set
            {
                if (value.Equals(_removeBaseLine)) return;
                _removeBaseLine = value;
                OnPropertyChanged("RemoveBaseLine");
            }
        }

        public double NoiseFilterLevel
        {
            get { return _noiseFilterLevel; }
            set
            {
                if (value.Equals(_noiseFilterLevel)) return;
                _noiseFilterLevel = value;
                OnPropertyChanged("NoiseFilterLevel");
            }
        }

        public bool NormalizeByIT
        {
            get => this._normalizeByIT;
            set
            {
                if (value.Equals(this._normalizeByIT))
                    return;
                this._normalizeByIT = value;
                this.OnPropertyChanged(nameof(NormalizeByIT));
            }
        }

        public double ReferenceIT
        {
            get => this._referenceIT;
            set
            {
                if (value.Equals(this._referenceIT))
                    return;
                this._referenceIT = value;
                this.OnPropertyChanged(nameof(ReferenceIT));
            }
        }

        #endregion
    }

    [Export(typeof (IUserSettingsProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class UserSettingsProvider : IUserSettingsProvider
    {
        private readonly ModasImportUserSettings _importUserSettings;

        [ImportingConstructor]
        public UserSettingsProvider()
        {
            _importUserSettings = new ModasImportUserSettings();
            _importUserSettings.ReadFromSettigns();
        }

        #region Implementation of IUserSettingsProvider

        public void Save()
        {
            _importUserSettings.SaveToSettings();
            Settings.Default.Save();
        }

        public IModasImportUserSettings ModasImportSettings
        {
            get { return _importUserSettings; }
        }

        #endregion
    }
}
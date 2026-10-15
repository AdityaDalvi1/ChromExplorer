using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Plotter.Markers;
using ChromExplorer.Services.PickPeakerService.Services;
using Microsoft.Practices.Prism.Commands;

namespace ChromExplorer.Modules.Settings.ViewModels
{
    //TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
    [Export(typeof (PeakPickingSettingsViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PeakPickingSettingsViewViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        [Import] private PeakPickerService _service;
        private IPeakPicker _current;
        private ThresholdTypes _thresholdType;
        private double _threshold;
        private PeakAnalyzers _analyzer;
        private string _labelFormat;
        private MarkerOrientation _labelOrientation;
        private MarkerPlacement _labelPlacement;
        private string _testData;

        public PeakPickingSettingsViewViewModel()
        {
            ApplyCommand = new DelegateCommand<object>(OnApplyCommandExecute);
            TestData = "Test Data";
            LabelOrientation = MarkerOrientation.Horizontal;
            LabelPlacement = MarkerPlacement.Top;
        }

        public DelegateCommand<object> ApplyCommand { get; private set; }
        public ObservableCollection<IPeakPicker> Algorithms { get; set; }

        public IPeakPicker Current
        {
            get { return _current; }
            set
            {
                if (Equals(value, _current)) return;
                _current = value;
                OnPropertyChanged("Current");
            }
        }

        public ThresholdTypes ThresholdType
        {
            get { return _thresholdType; }
            set
            {
                if (value == _thresholdType) return;
                _thresholdType = value;
                OnPropertyChanged("ThresholdType");
            }
        }

        public double Threshold
        {
            get { return _threshold; }
            set
            {
                if (value.Equals(_threshold)) return;
                _threshold = value;
                OnPropertyChanged("Threshold");
            }
        }

        public PeakAnalyzers Analyzer
        {
            get { return _analyzer; }
            set
            {
                if (value == _analyzer) return;
                _analyzer = value;
                OnPropertyChanged("Analyzer");
            }
        }

        public string LabelFormat
        {
            get { return _labelFormat; }
            set
            {
                if (value == _labelFormat) return;
                _labelFormat = value;
                OnPropertyChanged("LabelFormat");
            }
        }

        public MarkerOrientation LabelOrientation
        {
            get { return _labelOrientation; }
            set
            {
                if (value == _labelOrientation) return;
                _labelOrientation = value;
                OnPropertyChanged("LabelOrientation");
            }
        }

        public MarkerPlacement LabelPlacement
        {
            get { return _labelPlacement; }
            set
            {
                if (value == _labelPlacement) return;
                _labelPlacement = value;
                OnPropertyChanged("LabelPlacement");
            }
        }

        public string TestData
        {
            get { return _testData; }
            set
            {
                if (value == _testData) return;
                _testData = value;
                OnPropertyChanged("TestData");
            }
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            if (_service != null)
            {
                Algorithms = _service.Algorithms;
                Current = _service.SelectedAlgorithm;
                ThresholdType = _service.ThresholdType;
                Threshold = _service.Threshold;
                Analyzer = _service.Analyzer;
                LabelFormat = _service.LabelFormat;
                LabelPlacement = _service.LabelPlacement;
                LabelOrientation = _service.LabelOrientation;
            }
        }

        private void OnApplyCommandExecute(object obj)
        {
            if (Current == null || _service == null)
                return;

            _service.SelectedAlgorithm = Current;
            _service.ThresholdType = ThresholdType;
            _service.Threshold = Threshold;
            _service.Analyzer = Analyzer;
            _service.LabelFormat = LabelFormat;
            _service.LabelOrientation = LabelOrientation;
            _service.LabelPlacement = LabelPlacement;

            _service.RaisePerformPeakickingEvent();
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
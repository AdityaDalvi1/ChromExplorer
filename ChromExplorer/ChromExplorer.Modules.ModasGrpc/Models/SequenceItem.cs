using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChromExplorer.Modules.ModasGrpc.Models
{
    // One row of a Sample Sequence — plain, directly grid-editable fields. The actual run-time
    // settings (Acquisition Length, Ion Source, Overrides) aren't stored here; Run Sequence
    // builds them fresh from whichever AnalysisSampleType preset is currently set, with
    // InstrumentMethod/DataFolder applied on top, at the moment each row runs.
    public class SequenceItem : INotifyPropertyChanged
    {
        public static readonly string[] SampleClassifications = { "Blank", "Calibrant", "Standard", "Sample" };

        // Shown as the initial, disabled "pick one" entry in the Sample Type/Analysis Sample
        // Type dropdowns — never a real selectable value.
        public const string SelectSampleTypePlaceholder = "Select Sample Type";
        public const string SelectAnalysisSampleTypePlaceholder = "Select Analysis Sample Type";

        private string _sampleName = "";
        private string _sampleClassification = SelectSampleTypePlaceholder;
        private string _analysisSampleType = SelectAnalysisSampleTypePlaceholder;
        private string _instrumentMethod = "";
        private string _dataFolder = "";
        private string _status = "Pending";
        private string _filename = "";

        public string SampleName
        {
            get => _sampleName;
            set => SetField(ref _sampleName, value);
        }

        // QC classification of this sample: Blank / Calibrant / Standard / Sample.
        public string SampleClassification
        {
            get => _sampleClassification;
            set => SetField(ref _sampleClassification, value);
        }

        // Which Analysis Method preset (e.g. "DSAP MS") this row runs with — its Acquisition
        // Length/Ion Source/Overrides are pulled from that preset at Run Sequence time.
        public string AnalysisSampleType
        {
            get => _analysisSampleType;
            set => SetField(ref _analysisSampleType, value);
        }

        public string InstrumentMethod
        {
            get => _instrumentMethod;
            set => SetField(ref _instrumentMethod, value);
        }

        public string DataFolder
        {
            get => _dataFolder;
            set => SetField(ref _dataFolder, value);
        }

        // Pending -> Acquiring -> Completed, or Aborted (stopped by the user or a run error).
        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        public string Filename
        {
            get => _filename;
            set => SetField(ref _filename, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}

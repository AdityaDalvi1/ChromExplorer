using System.ComponentModel;
using System.ComponentModel.Composition;
using ChromExplorer.Modules.MzMLExporter.Properties;

namespace ChromExplorer.Modules.MzMLExporter.ViewModels
{
    
    [Export(typeof(MzMLExportSettingsViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MzMLExportSettingsViewViewModel : INotifyPropertyChanged
    {
        private string _messageText;

        [ImportingConstructor]
        public MzMLExportSettingsViewViewModel()
        {
            MessageText = "To be implemented";
        }

        public string MessageText
        {
            get { return _messageText; }
            set
            {
                if (value == _messageText) return;
                _messageText = value;
                OnPropertyChanged("MessageText");
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Modas.Grpc.Client.Shared;
using Modas.Grpc.Models;

namespace Modas.Grpc.Controls
{
    public class IonSourceControlViewViewModel : INotifyPropertyChanged
    {
        private readonly IModasRemoteClient _client;
        private string _heaterButtonText;
        private string _heaterButtonText1;
        private IonSourceConfig _config;

        public IonSourceControlViewViewModel(IModasRemoteClient client)
        {
            _client = client;
            Config = new IonSourceConfig();
            SetSprayVoltageCommand = new AsyncDelegateCommand(OnSetSprayVoltage);
            EnableHeaterCommand = new AsyncDelegateCommand(OnEnableHeater);
            UpdateButtonText();
        }

        private Task OnEnableHeater()
        {
            // The ToggleButton's two-way IsChecked binding has already flipped
            // DsapHeaterConfig.Enabled before this command runs — flipping it again here
            // would undo the click and send the old state to the instrument.
            UpdateButtonText();

            _client.UpdateDsapHeater(Config);

            return Task.CompletedTask;
        }

        private async Task OnSetSprayVoltage()
        {
            var msg = await _client.UpdateSprayVoltage(Config);
            //TODO : Handle response
        }

        public IonSourceConfig Config
        {
            get => _config;
            set
            {
                if (SetField(ref _config, value))
                    UpdateButtonText();
            }
        }

        public string HeaterButtonText
        {
            get => _heaterButtonText1;
            set
            {
                if (value == _heaterButtonText1) return;
                _heaterButtonText1 = value;
                OnPropertyChanged();
            }
        }

        private void UpdateButtonText()
        {
            HeaterButtonText = Config?.DsapHeaterConfig?.Enabled == true ? "Off" : "On";
        }

        public AsyncDelegateCommand SetSprayVoltageCommand { get; private set; }
        public AsyncDelegateCommand EnableHeaterCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
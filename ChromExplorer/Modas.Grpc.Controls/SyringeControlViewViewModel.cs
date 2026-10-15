using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Modas.Grpc.Client.Shared;
using Modas.Grpc.Models;
using System.Windows;

namespace Modas.Grpc.Controls
{
    public class SyringeActionEventArgs : EventArgs
    {
        public SyringeActionEventArgs(MTSyringeAction action, bool success, string message, SyringeConfig config)
        {
            Action = action;
            Success = success;
            Message = message;
            Diameter = config.Diameter;
            Rate = config.Rate;
            Volume = config.Volume;
        }

        public MTSyringeAction Action { get; }
        public bool Success { get; }
        public string Message { get; }
        public double Diameter { get; }
        public double Rate { get; }
        public double Volume { get; }
    }

    public class SyringeControlViewViewModel : INotifyPropertyChanged
    {
        private readonly IModasRemoteClient _client;
        private SyringeConfig _syringeConfig = new SyringeConfig();
        private Visibility _visibility;

        public SyringeControlViewViewModel(IModasRemoteClient client)
        {
            _client = client;
            StartCommand = new AsyncDelegateCommand(OnStart);
            StopCommand = new AsyncDelegateCommand(OnStop);
        }

        public event EventHandler<SyringeActionEventArgs> ActionCompleted;

        private Task OnStop() => RunAction(MTSyringeAction.Stop);

        private Task OnStart() => RunAction(MTSyringeAction.Start);

        private async Task RunAction(MTSyringeAction action)
        {
            SyringeConfig.Action = action;
            bool success;
            string message;
            try
            {
                var msg = await _client.SyringeControl(SyringeConfig);

                // MTRemoteServiceClientImpl swallows transport exceptions and hands back a blank
                // RemoteMessage (never populated, OccuredAt == DateTime.MinValue) — that means no
                // reply came back from the instrument at all.
                bool noReply = msg == null || (string.IsNullOrEmpty(msg.Message) && msg.OccuredAt == default(DateTime));
                if (noReply)
                {
                    success = false;
                    message = "No reply from the instrument. Check that the instrument is connected.";
                }
                else if (msg.Severity == MessageSeverity.Error || msg.Error != ErrorCode.None)
                {
                    success = false;
                    message = string.IsNullOrWhiteSpace(msg.Message) ? ("Instrument error: " + msg.Error) : msg.Message;
                }
                else
                {
                    success = true;
                    message = msg.Message ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            ActionCompleted?.Invoke(this, new SyringeActionEventArgs(action, success, message, SyringeConfig));
        }

        public SyringeConfig SyringeConfig
        {
            get => _syringeConfig;
            set
            {
                Visibility = value.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                SetField(ref _syringeConfig, value);
            }
        }

        public Visibility Visibility
        {
            get => _visibility;
            set => SetField(ref _visibility, value);
        }

        public AsyncDelegateCommand StartCommand { get; private set; }

        public AsyncDelegateCommand StopCommand { get; private set; }


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


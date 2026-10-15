using System;
using System.Collections.Generic;
using System.ComponentModel;
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Timers;
using ChromExplorer.Modules.Syringe.Settings;
using ChromExplorer.Syringe;

namespace ChromExplorer.Modules.Syringe.Services
{
    [Export(typeof(ISyringeControl))]
    [ExactTypeMetadata(typeof(ChemyxSyringeControlService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChemyxSyringeControlService : ISyringeControl
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISyringeClient _controller;
        private bool _isRunning = false;
        private bool _isConnected = false;
        private bool _verifyPending = false;

        private readonly Timer _statusTimer;

        [ImportingConstructor]
        public ChemyxSyringeControlService()
        {
            log.Info("ChemyxSyringeControlService constructed");
            _controller = new SyringeControllerSerial();
            _controller.MessageReceived += OnSyringeMessageReceived;

            _statusTimer = new Timer { AutoReset = true, Enabled = false, Interval = 1000 };
            _statusTimer.Elapsed += _statusTimer_Elapsed;
        }

        private void _statusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(_isConnected)
                _controller.Write("pump status");
        }

        private void OnSyringeMessageReceived(object sender, SyringeMessageReceivedEventArgs e)
        {
            log.Info($"Syringe Message Received: {e.Message}");
            if (this._verifyPending && this.Verify(e.Message))
            {
                this.IsConnected = true;
                this._verifyPending = false;
            }
            if (e.Message.Contains("Pump start running..."))
                this.IsRunning = true;

            if (e.Message.Contains("pump status"))
            {
                //pump status\r\n0\r\n>

                var parts = e.Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && int.TryParse(parts[1], out int status))
                {
                    this.IsRunning = status == 1;

                    if(!IsRunning)
                        _statusTimer.Stop();
                }
                
            }

            if (e.Message.Contains("Pump stop!"))
            {
                this.IsRunning = false;
                _statusTimer.Stop();
            }

        }

        public void Connect()
        {
            _controller.Connect(SyringeModuleSettings.Default.Port);
            VerifyRequest();
        }

        public void Start(double diameter, double rate, double volume)
        {
            log.Info($"Syringe started with Diameter: {diameter}, Rate: {rate}, Volume: {volume}");

            _controller.Write($"hexw2 {2} {0} {diameter} {volume} {rate} {0} start");
            _statusTimer.Start();
        }


       

        public void Stop()
        {
            log.Info("Syringe stopped");
            _controller.Write("stop");
            _statusTimer.Stop();
        }

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

        public bool IsConnected
        {
            get => _isConnected;
            private set => SetField(ref _isConnected, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set => SetField(ref _isRunning, value);
        }

        public void VerifyRequest()
        {
            this._verifyPending = true;
            this._controller.Write("help"); ;
        }

        public void Write(string msg)
        {
            _controller.Write(msg);
        }

        private bool Verify(string msg)
        {
            return msg.Contains("-------------------------------------") && msg.Contains("-Show Help Information");
        }

    }
}
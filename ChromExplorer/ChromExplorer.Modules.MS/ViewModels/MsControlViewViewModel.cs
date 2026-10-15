using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Ms.Services;
using log4net;
using Microsoft.Practices.Prism.Events;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Modas.Grpc.Controls;
using DelegateCommand = Microsoft.Practices.Prism.Commands.DelegateCommand;

namespace ChromExplorer.Modules.Ms.ViewModels
{
    [Export(typeof(MsControlViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MsControlViewViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged 
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly MsControlService _msControl;
        private bool _deviceDataEnabled = false;
        private bool _connected = false;
        private bool _connecting = false;
        private readonly IEventAggregator _eventAggregator;


        [Import] private CompositionContainer _container;
      
        private string _logText;

        [ImportingConstructor]
        public MsControlViewViewModel(IEventAggregator eventAggregator, MsControlService service)
        {
            _eventAggregator = eventAggregator;
            _msControl = service;
            _msControl.StatusReceived += _msControl_StatusReceived;
           
            
            ConnectCommand = new AsyncDelegateCommand(OnConnect);

            UpdateCommands();
        }

        private void _msControl_StatusReceived(object sender, Modas.Grpc.Client.Shared.StatusReceivedEventArgs e)
        {
            StatusViewVM.UpdateStatus(e.StatusItems);
        }

        private async Task OnConnect()
        {
            var result = await _msControl.Connect();
        }


        private bool ConnectCanExecute()
        {
            return !_connected && !_connecting;
        }

    

        public AsyncDelegateCommand ConnectCommand { get; private set; }


       
     

        public string LogText
        {
            get => _logText;
            set => SetField(ref _logText, value);
        }

        public StatusViewViewModel StatusViewVM { get; } = new StatusViewViewModel();

        private void AppendLogMessage(string message)
        {
            LogText += $"{DateTime.Now} : {message}\n";
        }

        void UpdateCommands()
        {
           
        }

        public void OnImportsSatisfied()
        {
            
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
    }
}

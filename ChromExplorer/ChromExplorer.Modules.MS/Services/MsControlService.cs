using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using Microsoft.Practices.Prism.Events;
using Modas.Grpc.Client.Shared;
using Modas.Grpc.Models;
using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;

namespace ChromExplorer.Modules.Ms.Services
{
    [Export(typeof(MsControlService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MsControlService 
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IEventAggregator _eventAggregator;
        private string _grpcHost = "127.0.0.1";
        private int _grpcPort = 5001;

        private IModasRemoteClient _client;

        private bool _inSampleRun = false;
        private DateTime _startTime; 
        private double _sampleRunDurationSec = 0;
        private bool _connected = false;

        [ImportingConstructor]
        public MsControlService(IModasRemoteClientFactory factory, IEventAggregator eventAggregator)
        {
            log.Info("MsControlService constructed");
            _eventAggregator = eventAggregator;

            _client = factory.Create(_grpcHost, _grpcPort, ChannelCredentials.Insecure);


            _eventAggregator.GetEvent<MTE30AcquisitionRequest>().Subscribe(OnStartRequested);
            _eventAggregator.GetEvent<MTE30StatusRequest>().Subscribe(OnStatusRequest);
        }
            
        public event EventHandler<MeasurementsReceivedEventArgs> MeasurementsReceived
        {
            // Custom add and remove accessors can be used, but generally not needed for simple forwarding
            add
            {
                if (_client != null) _client.MeasurementReceived += value;
            }
            remove
            {
                if (_client != null) _client.MeasurementReceived -= value;
            }
        }


        public event EventHandler<StatusReceivedEventArgs> StatusReceived
        {
            // Custom add and remove accessors can be used, but generally not needed for simple forwarding
            add
            {
                if (_client != null) _client.StatusReceived += value;
            }
            remove
            {
                if (_client != null) _client.StatusReceived -= value;
            }
        }



        private void OnStatusRequest(object obj)
        {
            _eventAggregator.GetEvent<MTE30StatusUpdatedEvent>().Publish(new MTE30StatusData() { Connected = _connected });
        }

        private void OnClientDisconnect(object sender, EventArgs e)
        {
            _connected = false;
        }


        public async Task Start()
        {
            var config = new AcquisitionConfig();

            config.Filename = string.Empty;
            config.ScanCount = -1;
            config.ScanDurationSec = -1;

            var type = MTSpectrumType.Profile;

            config.Type = type;

            await _client.StartMeasurements(config);
        }


        public void Stop()
        {
            _client.StopMeasurements();
        }

       
        
        public async Task<RequestControlStatus> Connect()
        {
            var result = await _client.RequestControl();

            _connected = result.IsControlGranted;
            _eventAggregator.GetEvent<MTE30StatusUpdatedEvent>().Publish(new MTE30StatusData() { Connected = _connected });

            if (result.IsControlGranted)
            {
                log.DebugFormat("Connect: Connected to {0}, Modas {1}, Control granted", result.SerialNumber, result.SoftwareVersion);

                _ = _client.UpdateStatusFeed();

            }

            return await Task.FromResult(result);
        }

        public void Disconnect()
        {
        }

        public string Ping()
        {
            return string.Empty;
        }

        public void EnableDeviceData(bool enable, int intervalMsec)
        {
        }

        public void ToPowerOff()
        {
        }

        public void ToStandby()
        {
        }

        public void ToReady()
        {
        }

        public async void OnStartRequested(MTE30AcquisitionRequestData data)
        {
            try
            {
                var config = new AcquisitionConfig();

                config.Filename = string.Empty;
                config.ScanCount = -1;
                config.ScanDurationSec = -1;

                var type = MTSpectrumType.Centroid;

                config.Type = type;

                await _client.StartMeasurements(config);
            }
            catch (Exception e)
            {
                //throw; // TODO handle exception
            }
        }

        
    }
}
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using Modas.MTRemote.Data;
using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ChromExplorer.ModasClient
{
    [Export(typeof(MTRemoteClientHost))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MTRemoteClientHost : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Guid _clientID = Guid.NewGuid();
        private readonly ModasServiceCallbackHandler _callbackHandler;
        private MTRemoteControlServiceClient _client = null;

        private bool _connected = false;
        private MTRegistrationResponse _systemInfo = null;

        /// <summary>
        /// Constructor
        ///
        /// Creates callback handler and set default service address 
        /// </summary>
        public MTRemoteClientHost()
        {
            _callbackHandler = new ModasServiceCallbackHandler();
            HttpBaseAddress = "http://localhost:8881/Modas/Server";
            log.InfoFormat("Remote client has been constructed. ID = {0}", _clientID.ToString());
        }

        /// <summary>
        /// WCF service address
        /// </summary>
        public string HttpBaseAddress { get; set; }

        public IMTFeedbackService Callback
        {
            get { return _callbackHandler; }
        }

        /// <summary>
        /// Creates and configures service binding
        /// </summary>
        public void Connect()
        {
            log.InfoFormat("Connect(): connecting to {0}", HttpBaseAddress);
            try
            {
                WSDualHttpBinding binding = new WSDualHttpBinding();
                //NetHttpBinding binding = new NetHttpBinding();
                binding.MessageEncoding = WSMessageEncoding.Mtom;
                binding.MaxReceivedMessageSize = 2 * 1024 * 1024;
                binding.MaxBufferPoolSize = 2 * 1024 * 1024;
                binding.ReaderQuotas.MaxArrayLength = 2 * 1024 * 1024;
                binding.ReaderQuotas.MaxBytesPerRead = 2 * 1024 * 1024;



                System.ServiceModel.EndpointAddress endpoint = new
                    System.ServiceModel.EndpointAddress(new Uri(HttpBaseAddress));

                _client = new MTRemoteControlServiceClient(new InstanceContext(_callbackHandler), binding, endpoint);
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                _client = null;
            }
        }

        /// <summary>
        /// Disconnects from WCF service
        /// </summary>
        public void Disconnect()
        {
            if (_client == null)
                return;


            log.InfoFormat("Disconnect() called");

            try
            {
                _client.Close();
                _client = null;
                _systemInfo = null;
                _connected = false;

                OnDisconnected();
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }
        }

        /// <summary>
        /// Requests instrument control. WCF service supports single client for now
        /// </summary>
        public void RequestControl()
        {
            if (_client == null)
                return;

            try
            {
                _client.RequestControlAsync(_clientID, true).ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion && task.Result.Success)
                    {
                        _systemInfo = task.Result;
                        _connected = true;
                        OnConnected(new MTConnectedEventArgs(_systemInfo));

                        log.InfoFormat("RequestControl(): response received: {0}", task.Result);
                    }
                });
            }
            catch (Exception e)
            {
                log.Warn(e);
            }
        }

        /// <summary>
        /// Ping the service.
        /// </summary>
        /// <returns>String with software name, version and current date and time</returns>
        public string Ping()
        {
            if (_client == null)
                return string.Empty;

            log.Debug("Ping() called");

            try
            {
                var retval = _client.Ping(_clientID);

                log.DebugFormat("Ping(): response {0}", retval);

                return retval;
            }
            catch (Exception e)
            {
                log.Warn(e);
            }

            return string.Empty;
        }

        /// <summary>
        /// Enables or disables events being fired which send current device setup parameter readbacks
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="intervalMsec"></param>
        public void SetDeviceDataReadbackEnabled(bool enabled, int intervalMsec)
        {
            if (_client == null)
                return;

            log.DebugFormat("SetDeviceDataReadbackEnabled() called, enabled = {0}, interval (msec) {1}", enabled, intervalMsec);

            try
            {
                _client.SetDeviceDataReadbackEnabled(_clientID, enabled, intervalMsec);
            }
            catch (Exception e)
            {
                log.Warn(e);
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// Fires after successful <paramref name="RequestControl"/> call
        /// </summary>
        public event EventHandler<MTConnectedEventArgs> Connected;


        /// <summary>
        /// Fires after <paramref name="Disconnect"/> call
        /// </summary>
        public event EventHandler Disconnected;

        protected virtual void OnConnected(MTConnectedEventArgs e)
        {
            var handler = Connected;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnDisconnected()
        {
            var handler = Disconnected;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
using log4net;
using Modas.MTRemote.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.ModasClient
{
    public class ModasServiceCallbackHandler : IMTRemoteControlServiceCallback, IMTFeedbackService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public void MeasurementResultCallback(MTDataContainer data)
        {
            log.DebugFormat($"DeviceDataUpdated: {data.FullTypeName}, size {data.Payload.Length}, md5 {data.MD5Hash}");

            var d = MTMeasurementDataSet.Restore(data);

            OnMeasurementDataReceived(new MTMeasurementDataReceivedEventArgs(d));
        }

        public void MassSpecStateChangedCallback(MTMassSpecState state)
        {
            log.DebugFormat("MassSpecStateChanged state: {0}", state);

            OnStateChanged(new MTMassSpecStateChangedEventArgs(state));
        }

        public void DeviceDataUpdatedCallback(MTDataContainer data)
        {
            log.DebugFormat($"DeviceDataUpdated: {data.FullTypeName}, size {data.Payload.Length}, md5 {data.MD5Hash}");

            var dd = MTDeviceDataCollection.Restore(data);

            OnDeviceDataReceived(new MTDeviceDataReceivedEventArgs(dd));
        }

        public void IonSourceConnectedCallback(MTIonSourceInfo info)
        {
            log.DebugFormat($"IonSourceConnectedCallback: {info.ID}");
            OnIonSourceConnected(new MTIonSourceConnectedEventArgs(info));
        }

        public void MessageReceivedCallback(MTMessage msg)
        {
            log.DebugFormat($"MessageReceivedCallback: {msg.Message}");
            OnMessageReceived(new MTMessageReceivedEventArgs(msg));
        }

        /// <summary>
        /// Fires when the Mass Spectrometer operating state has changed
        /// </summary>
        public event EventHandler<MTMassSpecStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Fires when measurement data has been received from the mass spectrometer
        /// </summary>
        public event EventHandler<MTMeasurementDataReceivedEventArgs> MeasurementDataReceived;

        /// <summary>
        /// Fires at least once per second, informing the external software suite of current device values (not measurements, but things like gas flow, voltages, etc.) 
        /// </summary>
        public event EventHandler<MTDeviceDataReceivedEventArgs> DeviceDataReceived;

        /// <summary>
        /// Fires when there’s an error, warning, or info that should be given to the user
        /// </summary>
        public event EventHandler<MTMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Fires when new ion source has been connected/disconnected
        /// </summary>
        public event EventHandler<MTIonSourceConnectedEventArgs> IonSourceConnected;

        protected virtual void OnStateChanged(MTMassSpecStateChangedEventArgs e)
        {
            var handler = StateChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnMeasurementDataReceived(MTMeasurementDataReceivedEventArgs e)
        {
            var handler = MeasurementDataReceived;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnDeviceDataReceived(MTDeviceDataReceivedEventArgs e)
        {
            var handler = DeviceDataReceived;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnMessageReceived(MTMessageReceivedEventArgs e)
        {
            EventHandler<MTMessageReceivedEventArgs> handler = MessageReceived;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnIonSourceConnected(MTIonSourceConnectedEventArgs e)
        {
            var handler = IonSourceConnected;
            if (handler != null) handler(this, e);
        }
    }
}

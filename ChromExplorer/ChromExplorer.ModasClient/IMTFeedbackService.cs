using System;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// Interface with events emitted by host to client communications
    /// </summary>
    public interface IMTFeedbackService
    {
        /// <summary>
        /// Fires when the Mass Spectrometer operating state has changed
        /// </summary>
        event EventHandler<MTMassSpecStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Fires when measurement data has been received from the mass spectrometer
        /// </summary>
        event EventHandler<MTMeasurementDataReceivedEventArgs> MeasurementDataReceived;

        /// <summary>
        /// Fires at least once per second, informing the external software suite of current device values (not measurements, but things like gas flow, voltages, etc.) 
        /// </summary>
        event EventHandler<MTDeviceDataReceivedEventArgs> DeviceDataReceived;

        /// <summary>
        /// Fires when there’s an error, warning, or info that should be given to the user
        /// </summary>
        event EventHandler<MTMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Fires when new ion source has been connected/disconnected
        /// </summary>
        event EventHandler<MTIonSourceConnectedEventArgs> IonSourceConnected;
    }
}
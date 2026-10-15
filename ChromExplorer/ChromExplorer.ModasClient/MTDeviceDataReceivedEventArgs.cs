using System;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// A DeviceDataReceived event is raised periodically when device data are received from mass spectrometer
    /// </summary>
    public class MTDeviceDataReceivedEventArgs : EventArgs
    {
        public MTDeviceDataReceivedEventArgs(MTDeviceDataCollection dd)
        {
            DeviceData = dd;
        }
        public MTDeviceDataCollection DeviceData { get; private set; }
    }
}
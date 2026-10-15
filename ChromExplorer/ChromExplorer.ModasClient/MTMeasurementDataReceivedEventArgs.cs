using System;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// A MeasurementDataReceived event is raised periodically when spectrum is acquired by the mass spectrometer.
    /// MTMeasurementDataReceivedEventArgs provides corresponding data set
    /// </summary>
    public class MTMeasurementDataReceivedEventArgs : EventArgs
    {
        public MTMeasurementDataReceivedEventArgs()
        {
            DataSet = null;
        }

        public MTMeasurementDataReceivedEventArgs(MTMeasurementDataSet dataSet)
        {
            DataSet = dataSet;
        }

        public MTMeasurementDataSet DataSet { get; private set; }
    }
}
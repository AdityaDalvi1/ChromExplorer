using System;
using System.Runtime.Serialization;

namespace Modas.MTRemote.Data
{
    [Serializable]
    public enum MTMeasurementDataSetType
    {
        MSScan,  // MS only
        MS2Scan, // MSMS only
        DDScan,  // Data dependent scan (MS followed by optional MSMS scans)
    }
}
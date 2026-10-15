using System;
using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// Header / metadata for the measurements in the dataset 
    /// </summary>
    [Serializable]
    public class MTMeasurementDataSetHeader
    {
        /// <summary>
        /// Scan number in current run (each acquisition start will reset this number)
        /// </summary>
        public int ScanNumber { get; set; }

        /// <summary>
        /// Acquisition time in ms counting from the run start 
        /// </summary>
        public int RetentionTimeMs { get; set; }

        /// <summary>
        /// Lowest detected m/z
        /// </summary>
        public double LowMass { get; set; }

        /// <summary>
        /// Highest detected m/z
        /// </summary>
        public double HighMass { get; set; }

        /// <summary>
        /// Scan type (MS or MS2)
        /// </summary>
        public MTMeasurementTypes MeasurementType { get; set; }
        
        /// <summary>
        /// Mass range mode 
        /// </summary>
        public MTMassRanges MassRange { get; set; }

        /// <summary>
        /// Injection time used. Typically it is constant, but may be affected by AGC
        /// </summary>
        public int InjectionTimeMs { get; set; }

        /// <summary>
        /// Microscan count
        /// </summary>
        public int MicroscanCount { get; set; }

        /// <summary>
        /// Parent ion mass to charge ration. Should be ignored for MS2
        /// </summary>
        public double ParentIon { get; set; }

        /// <summary>
        /// Isolation width in m/z
        /// </summary>
        public double IsolationWidth { get; set; }

        /// <summary>
        /// Fragmentation width in m/z
        /// </summary>
        public double FragmentationWidth { get; set; }


        /// <summary>
        /// Fragmentation energy (0-100%)
        /// </summary>
        public double FragmentationEnergy { get; set; }

        public override string ToString()
        {
            if (MeasurementType == MTMeasurementTypes.MS)
                return string.Format("#{0}:{1} [{2:0}-{3:0}] {4}", ScanNumber, RetentionTimeMs, LowMass, HighMass, MeasurementType);

            return string.Format("#{0}:{1} [{2:0}-{3:0}] {4}. PI {5}, IW {6:0.00} FW {7:0.00}, FE {8:0.00}", ScanNumber, RetentionTimeMs, LowMass, HighMass, MeasurementType,
            ParentIon, IsolationWidth, FragmentationWidth, FragmentationEnergy);
        }
    }
}
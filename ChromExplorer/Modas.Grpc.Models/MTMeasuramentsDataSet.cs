using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Modas.Grpc.Models
{
    
    public enum MTMeasurementDataSetType
    {
        MSScan,  // MS only
        MS2Scan, // MSMS only
        DDScan,  // Data dependent scan (MS followed by optional MSMS scans)
    }

    public enum MTMeasurementTypes
    {
        MS,
        MS2,
        MS3
    }

    public enum MTMassRanges
    {
        Low,
        Normal
    }

    public class MTMeasurement
    {
        public MTMeasurement()
        {
            MassChargeRatio = 0.0f;
            Abundance = 0.0f;
        }

        public MTMeasurement(float mz, float abundance)
        {
            MassChargeRatio = mz;
            Abundance = abundance;
        }

        public float MassChargeRatio { get; set; }

        public float Abundance { get; set; }
    }

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


        public double IsolationWidth { get; set; }
        public double FragmentationWidth { get; set; }
        public double FragmentationEnergy { get; set; }

        public MTSpectrumType SpectrumType { get; set; }

        public override string ToString()
        {
            if (MeasurementType == MTMeasurementTypes.MS)
                return string.Format("#{0}:{1} [{2:0}-{3:0}] {4}", ScanNumber, RetentionTimeMs, LowMass, HighMass, MeasurementType);

            return string.Format("#{0}:{1} [{2:0}-{3:0}] {4}. PI {5}, IW {6:0.00} FW {7:0.00}, FE {8:0.00}", ScanNumber, RetentionTimeMs, LowMass, HighMass, MeasurementType,
            ParentIon, IsolationWidth, FragmentationWidth, FragmentationEnergy);
        }
    }

    public class MTMeasurementDataSet
    {
        public MTMeasurementDataSet()
        {
            TimeStamp = DateTime.Now;
            Type = MTMeasurementDataSetType.MSScan;
            Header = new MTMeasurementDataSetHeader();
            Measurements = new List<MTMeasurement>();
        }

        public MTMeasurementDataSet(MTMeasurementDataSetType type)
        {
            TimeStamp = DateTime.Now;
            Type = type;
            Header = new MTMeasurementDataSetHeader();
            Measurements = new List<MTMeasurement>();
        }



        public DateTime TimeStamp { get; set; }

        public MTMeasurementDataSetType Type { get; set; }

        public MTMeasurementDataSetHeader Header { get; set; }

        public List<MTMeasurement> Measurements { get; set; }

        
    }

    
}
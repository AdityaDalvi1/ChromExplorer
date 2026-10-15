using System.Collections.Generic;

namespace ChromExplorer.ModasReader
{
    /// <summary>What the Method Report needs to know about the acquisition stored in a loaded
    /// .mtd file. Every value is optional: null / empty means "the file does not record it",
    /// and the report simply leaves that row out.</summary>
    public class MtdMethodInfo
    {
        public string FilePath { get; set; }

        // Sample / analysis (parsed from the acquisition file name
        // "{SampleName}_{Analysis_Type}_{ddMMyyyy}_{HHmm}")
        public string SampleName { get; set; }
        public string AnalysisType { get; set; }

        // General
        public string MethodName { get; set; }
        public string MethodFile { get; set; }
        public string SerialNumber { get; set; }
        public double? DurationSeconds { get; set; }
        public System.DateTime? AcquiredAt { get; set; }
        public int ScanCount { get; set; }

        // Ion source (only when the file records it)
        public double? SprayVoltage { get; set; }
        public double? HeaterValue { get; set; }

        // Acquisition parameters
        public string Polarity { get; set; }
        /// <summary>"MS", "MS2" or "MS3".</summary>
        public string MeasurementType { get; set; }
        public int MsPower { get; set; }
        public double? MassFrom { get; set; }
        public double? MassTo { get; set; }
        public int? Microscans { get; set; }
        public List<double> ParentIons { get; set; } = new List<double>();
        public double? SecondaryParentIon { get; set; }
        public double? FragmentationAmplitude { get; set; }
        public double? SecondaryFragmentationAmplitude { get; set; }
        /// <summary>Per-scan injection time in ms; Min == Max unless AGC varies it.</summary>
        public double? InjectionTimeMinMs { get; set; }
        public double? InjectionTimeMaxMs { get; set; }
        public string AgcMode { get; set; }
    }
}

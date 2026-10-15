using System.Collections.Generic;
using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface ISNRService
    {
        SNRResult CalculateSNR(
            SimpleDataSource points,
            List<PeakInfo> peaks,
            PeakInfo targetPeak,
            SNRCalculationSettings settings);
    }

    public class SNRCalculationSettings
    {
        public string SignalDefinition { get; set; } // "Height" or "Area"
        public string NoiseDefinition { get; set; } // "Peak-to-Peak", "RMS", "AutoRMS"
        public double Multiplier { get; set; } = 1.0;
        public bool IsAutomaticDetection { get; set; }
        public double DesiredLength { get; set; }
        public double MinimumLength { get; set; }
        public string SpecificTimeRange { get; set; }

        public bool IsSignalIntegrationActive { get; set; }
        public double SignalIntegrationStartTime { get; set; }
        public double SignalIntegrationEndTime { get; set; }

        public double AutoRmsNoiseStartTime { get; set; }
        public double AutoRmsNoiseEndTime { get; set; }
        public double AutoRmsNoiseWidth { get; set; }
    }

    public class SNRResult
    {
        public double SignalValue { get; set; }
        public double NoiseValue { get; set; }
        public double SNRatio { get; set; }
        public string NoiseRegionUsed { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public double NoiseRegionStartTime { get; set; }
        public double NoiseRegionEndTime { get; set; }
        public int NoiseRegionStartIndex { get; set; }
        public int NoiseRegionEndIndex { get; set; }
        public string SignalDefinition { get; set; }
        public string NoiseDefinition { get; set; }
    }
}

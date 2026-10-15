using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Infrastructure.Events
{
    // Published by "Apply Saved Method" (graph footer) after loading a .cmethod file, so the SNR
    // tab — if/when it's opened — shows the exact inputs and last-calculated result that were
    // saved, the same way clicking "Calculate S/N" would leave it, without re-running the
    // calculation (that needs the original peak/spectrum selection, which isn't restored).
    public class SNRMethodAppliedArgs
    {
        public string SignalDefinition { get; set; }
        public string NoiseDefinition { get; set; }
        public double Multiplier { get; set; }
        public bool IsAutomaticDetection { get; set; }
        public double DesiredLength { get; set; }
        public double MinimumLength { get; set; }
        public string SpecificTimeRange { get; set; }
        public bool AutoCompute { get; set; }

        public double SignalValue { get; set; }
        public double NoiseValue { get; set; }
        public double SNRatio { get; set; }
        public string NoiseRegionUsed { get; set; }
    }

    public class SNRMethodAppliedEvent : CompositePresentationEvent<SNRMethodAppliedArgs>
    {
    }
}

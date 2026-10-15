using System;
using System.Collections.Generic;

namespace ChromExplorer.DataModel.MethodFile
{
    // Everything captured by "Create Method" (Settings popup) and restored by "Apply Saved Method"
    // (graph footer). Enums from the various settings tabs are stored as plain strings so this DTO
    // doesn't need to reference Infrastructure/Plotter — the ViewModels that already know those
    // enum types do the Enum.Parse/ToString conversion at the edges.
    public class ChromMethodFile
    {
        public string MethodName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public PeakPickingMethodSettings PeakPicking { get; set; } = new PeakPickingMethodSettings();
        public List<PostProcessingOperationSettings> PostProcessingOperations { get; set; } = new List<PostProcessingOperationSettings>();
        public PeakIntegrationMethodSettings PeakIntegration { get; set; } = new PeakIntegrationMethodSettings();
        public SNRMethodSettings SNR { get; set; } = new SNRMethodSettings();
        public NistLibraryMethodSettings NistLibrary { get; set; } = new NistLibraryMethodSettings();
    }

    public class PeakPickingMethodSettings
    {
        // IPeakPicker.ObjectKey of the selected algorithm (e.g. "ModasPeakPicker") — used to find
        // the matching instance in PeakPickerService.Algorithms again on load.
        public string AlgorithmObjectKey { get; set; }
        public string ThresholdType { get; set; }
        public double Threshold { get; set; }
        public string Analyzer { get; set; }
        public string LabelFormat { get; set; }
        public string LabelOrientation { get; set; }
        public string LabelPlacement { get; set; }

        // Only meaningful for algorithms exposing a "Settings" property with these two members
        // (ModasPeakPicker); read/written via reflection since IPeakPicker doesn't declare them.
        public int MinPeakHalfSize { get; set; }
        public int PeakHalfSize { get; set; }
    }

    // One entry per IPostProcessingOperation (MS or EIC) — identified by ObjectKey since operations
    // aren't otherwise addressable outside the live MEF-composed collection.
    public class PostProcessingOperationSettings
    {
        public string ObjectKey { get; set; }
        public bool IsActive { get; set; }
        public List<ParamEntry> Parameters { get; set; } = new List<ParamEntry>();
    }

    public class ParamEntry
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class PeakIntegrationMethodSettings
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }

    public class SNRMethodSettings
    {
        public string SignalDefinition { get; set; }
        public string NoiseDefinition { get; set; }
        public double Multiplier { get; set; }
        public bool IsAutomaticDetection { get; set; }
        public double DesiredLength { get; set; }
        public double MinimumLength { get; set; }
        public string SpecificTimeRange { get; set; }
        public bool AutoCompute { get; set; }

        // Whatever the SNR tab's "Calculate" button last computed (if it was ever clicked), so a
        // "Create Method" done right after calculating captures the result, not just the inputs.
        public double SignalValue { get; set; }
        public double NoiseValue { get; set; }
        public double SNRatio { get; set; }
        public string NoiseRegionUsed { get; set; }
    }

    public class NistLibraryMethodSettings
    {
        public string NistExecutablePath { get; set; }
        public string FirstLocatorFile { get; set; }
        public string SecondLocatorFile { get; set; }
        public string WorkingDir { get; set; }
    }
}

namespace Modas.Grpc.Models
{
    public enum MTSpectrumType
    {
        None = 0,
        Profile = 1,
        Centroid = 2,
    }
    public class AcquisitionConfig
    {
        public int  ScanCount { get; set; } = -1;
        public int ScanDurationSec { get; set; } = -1;
        public MTSpectrumType Type { get; set; } = MTSpectrumType.None;
        public string Filename { get; set; } = string.Empty;

        public string SampleReference { get; set; } = string.Empty;
        public string SampleDescription { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
    
}
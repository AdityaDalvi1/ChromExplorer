namespace ChromExplorer.DataModel.PeakPicking
{
    public class PeakPickingSettings
    {
        public PeakPickingSettings()
        {
            this.MinPeakHalfSize = 10;
            this.PeakHalfSize = 70;
        }

        public int MinPeakHalfSize { get; set; }
        public int PeakHalfSize { get; set; } 
    }
}

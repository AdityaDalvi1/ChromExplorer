namespace ChromExplorer.DataModel.PeakPicking
{
    public class PeakInfo
    {
        public double Position { get; set; }
        public double Intensity { get; set; }
        public double HWHM { get; set; }
        public int LeftIndex { get; set; }
        public int RightIndex { get; set; }

        public override string ToString()
        {
            return string.Format("{0:0.00}:{1:0.} HWHM {2:0.000}", Position, Intensity, HWHM);
        }
    }
}

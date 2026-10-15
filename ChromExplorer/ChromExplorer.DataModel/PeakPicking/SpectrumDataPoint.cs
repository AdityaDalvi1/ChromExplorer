using System;

namespace ChromExplorer.DataModel.PeakPicking
{
    public class SpectrumDataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Index { get; set; }
        public bool InPeak { get; set; }

        public override string ToString()
        {
            string mark = " ";
            if (InPeak)
                mark = "+";
            return String.Format("{0}:{1:0.0000},{2:0.0000}{3}", Index, X, Y, mark);
        }
    }
}

namespace ChromExplorer.Modules.Quan.Model
{
    public class FitResult
    {
        public double Slope { get; set; }
        public double Offset { get; set; }
        public double RSquared { get; set; }

        public double GetY(double x)
        {
            return Slope*x + Offset;
        }
    }
}
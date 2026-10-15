namespace ChromExplorer.PostProcessing.RemoveSpikes
{
    public class ConfigurationParameters
    {
        public ConfigurationParameters()
        {
            //WidthThreshold = 0.1;
            //MedianMultiplier = 10.0;
            //MinWidth = 0.02;
            //MaxWidth = 2.0;
            //PPThreshold = 5;

            WidthThreshold = Properties.Settings.Default.WidthThreshold;
            MedianMultiplier = Properties.Settings.Default.MedianMultiplier;
            MinWidth = Properties.Settings.Default.MinWidth;
            MaxWidth = Properties.Settings.Default.MaxWidth;
            PPThreshold = Properties.Settings.Default.PPThreshold;
            IsActive = Properties.Settings.Default.IsActive;
        }

        public double MedianMultiplier { get; set; }
        public double WidthThreshold { get; set; }
        public double MinWidth { get; set; }
        public double MaxWidth { get; set; }
        public double PPThreshold { get; set; }
        public bool IsActive { get; set; }

        public void Save()
        {
            Properties.Settings.Default.WidthThreshold = WidthThreshold;
            Properties.Settings.Default.MedianMultiplier = MedianMultiplier;
            Properties.Settings.Default.MinWidth = MinWidth;
            Properties.Settings.Default.MaxWidth = MaxWidth;
            Properties.Settings.Default.PPThreshold = PPThreshold;
            Properties.Settings.Default.IsActive = IsActive;
        }
    }
}

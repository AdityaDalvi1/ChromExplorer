namespace ChromExplorer.PostProcessing.MedianFilter
{
    public class ConfigurationParameters
    {
        public ConfigurationParameters()
        {
            MedianWindowSize = Properties.Settings.Default.WindowSize;
            IsActive = Properties.Settings.Default.IsActive;
        }

        public int MedianWindowSize { get; set; }
        public bool IsActive { get; set; }

        public void Save()
        {
            Properties.Settings.Default.WindowSize = MedianWindowSize;
            Properties.Settings.Default.IsActive = IsActive;
            Properties.Settings.Default.Save();
        }
    }
}
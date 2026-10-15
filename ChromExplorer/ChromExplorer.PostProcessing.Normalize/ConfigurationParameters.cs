namespace ChromExplorer.PostProcessing.Normalize
{
    public class ConfigurationParameters
    {
        public ConfigurationParameters()
        {
            NormalizeTo = Properties.Settings.Default.NormalizeTo;
            ShiftToZero = Properties.Settings.Default.ShiftToZero;
            IsActive = Properties.Settings.Default.IsActive;
        }

        public double NormalizeTo { get; set; }
        public bool ShiftToZero { get; set; }
        public bool IsActive { get; set; }

        public void Save()
        {
            Properties.Settings.Default.NormalizeTo = NormalizeTo;
            Properties.Settings.Default.ShiftToZero = ShiftToZero;
            Properties.Settings.Default.IsActive = IsActive;
            Properties.Settings.Default.Save();
        }
    }
}

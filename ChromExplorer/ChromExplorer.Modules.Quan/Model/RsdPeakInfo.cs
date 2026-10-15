namespace ChromExplorer.Modules.Quan.Model
{
    /// <summary>One peak-area value queued for the RSD (Relative Standard Deviation)
    /// calculation, added via "Add to RSD" on a loaded chromatogram. Entries sharing the same
    /// SampleID are grouped together — RsdService.ComputeGroupSummaries() turns each group's
    /// Area values into a %RSD once the group has at least 2 entries.
    ///
    /// Plain get/set properties, not fields — this is bound directly by RsdView's DataGrid, and
    /// WPF's {Binding} can only resolve properties (a field silently binds to nothing).</summary>
    public class RsdPeakInfo
    {
        public int ID { get; set; }
        public string FilePath { get; set; }
        public string SampleID { get; set; }
        public double? Concentration { get; set; }
        public double Area { get; set; }
        public string InsertedDate { get; set; }
    }
}

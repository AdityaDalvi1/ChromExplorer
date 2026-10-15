namespace ChromExplorer.Modules.Quan.Model
{
    /// <summary>One row of the %RSD summary table: every queued RsdPeakInfo entry sharing the
    /// same SampleID, reduced to Mean/StdDev/%RSD. %RSD needs at least 2 replicate values to
    /// mean anything (sample standard deviation, using n-1, is undefined for a single point) —
    /// a group with only one entry still appears here (Count == 1) so the UI can explain why via
    /// Note, rather than silently showing 0%.</summary>
    public class RsdGroupSummary
    {
        /// <summary>Group key; null = entries added without a concentration.</summary>
        public double? Concentration { get; set; }
        public int Count { get; set; }
        public double MeanArea { get; set; }
        public double StdDev { get; set; }
        public double RsdPercent { get; set; }
        public string Note { get; set; }
    }
}
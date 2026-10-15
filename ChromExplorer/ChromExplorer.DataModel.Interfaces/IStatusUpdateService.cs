namespace ChromExplorer.DataModel.Interfaces
{
    public interface IStatusUpdateService
    {
        string AppStatus { get; set; }
        void SetReadyStatus();
        void SetWaitCursor(bool wait);
        void UpdateProgressRange(double min, double max);
        void ReportProgress(double progress);
        double ProgressMin { get; }
        double ProgressMax { get; }
    }
}

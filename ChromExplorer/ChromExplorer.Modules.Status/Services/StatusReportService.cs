using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.Modules.Status.Model;

namespace ChromExplorer.Modules.Status.Services
{
    [Export(typeof(IStatusUpdateService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
    public class StatusReportService : IStatusUpdateService
    {
        private readonly StatusReport _report;

        [ImportingConstructor]
        public StatusReportService()
        {
            _report = new StatusReport();
            SetReadyStatus();
        }

        public StatusReport Report { get { return _report; } }
      

        public double ProgressMin { get { return _report.ProgressMin; } }
        public double ProgressMax { get { return _report.ProgressMax; } }

        public string AppStatus
        {
            get { return _report.AppStatus; }
            set { _report.AppStatus = value; }
        }

        public void SetReadyStatus()
        {
            _report.AppStatus = "Ready";
            _report.CurrentProgress = _report.ProgressMin;
        }

        public void SetWaitCursor(bool wait)
        {
            Mouse.OverrideCursor = wait ? Cursors.Wait : null;

            //Quick fix for blocking user input
            Application.Current.MainWindow.IsHitTestVisible = !wait;
        }

        public void UpdateProgressRange(double min, double max)
        {
            _report.ProgressMin = min;
            _report.ProgressMax = max;
        }

        public void ReportProgress(double progress)
        {
            _report.CurrentProgress = progress;
        }
    }
}

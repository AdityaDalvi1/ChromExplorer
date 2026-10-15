using System;
using ChromExplorer.DataModel.Interfaces;

namespace ChromExplorer.Mocks
{
    public class MockStatusUpdateService : IStatusUpdateService
    {
        public string AppStatus { get; set; }
 
        public void SetReadyStatus()
        {
            Console.WriteLine("SetReadyStatus()");
        }

        public void SetWaitCursor(bool wait)
        {
            Console.WriteLine("etWaitCursor(wait: {0})", wait);
        }

        public void UpdateProgressRange(double min, double max)
        {
            throw new NotImplementedException();
        }

        public void ReportProgress(double progress)
        {
            throw new NotImplementedException();
        }

        public double ProgressMin
        {
            get { throw new NotImplementedException(); }
        }

        public double ProgressMax
        {
            get { throw new NotImplementedException(); }
        }
    }
}

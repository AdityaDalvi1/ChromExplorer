using System;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using ChromExplorer.Modules.Status.Model;

namespace ChromExplorer.Modules.Status.Services
{
    [Export(typeof(PerformanceReportService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PerformanceReportService
    {
        private readonly PerformanceReport _report;
        private readonly DispatcherTimer _timer;

        private bool _forceFullCollection;
        private double _previousUsage;
        private MemUsageUnit _units;
        
        [ImportingConstructor]
        public PerformanceReportService()
        {
            _forceFullCollection = true;
            _report = new PerformanceReport();
            _previousUsage = 0;
            _units = new MemUsageMB();
            
            Update();

            _timer = new DispatcherTimer();
            
            _timer.Interval = new TimeSpan(0, 0, 1);
            _timer.Tick += new EventHandler(TimerTick);
            _timer.Start();

        }

        public PerformanceReport Report
        {
            get { return _report; }
        }

        public MemUsageUnit Units
        {
            get { return _units;  }
            set { _units = value; }
        }

        public bool ForceFullCollection
        {
            get { return _forceFullCollection; }
            set { _forceFullCollection = value; }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            Update();
        }

        private void Update()
        {
            var currentUsage = GC.GetTotalMemory(_forceFullCollection);
            var change = currentUsage - _previousUsage;

            _report.MemUnits = _units.Name;
            _report.MemUsage = currentUsage/_units.Multiplier;
            _report.MemUsageChange = change/_units.Multiplier;

            _previousUsage = currentUsage;

        }
    }
}

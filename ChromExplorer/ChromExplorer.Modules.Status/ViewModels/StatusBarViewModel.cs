using System;
using System.ComponentModel.Composition;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.Modules.Status.Model;
using ChromExplorer.Modules.Status.Services;

namespace ChromExplorer.Modules.Status.ViewModels
{
    [Export(typeof(StatusBarViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class StatusBarViewModel
    {
        private readonly PerformanceReportService _performanceService;
        private readonly StatusReportService _statusUpdateService;
            

        [ImportingConstructor]
        public StatusBarViewModel(PerformanceReportService performanceService, IStatusUpdateService statusUpdateService)
        {
            if(performanceService == null)
            {
                throw new NullReferenceException("performanceService");
            }

            if (statusUpdateService == null)
            {
                throw new NullReferenceException("statusUpdateService");
            }

            _performanceService = performanceService;
            _statusUpdateService = (StatusReportService)statusUpdateService;

            if (_statusUpdateService == null)
            {
                throw new InvalidCastException("statusUpdateService");
            }
        }

        public PerformanceReport Performance
        {
            get { return _performanceService.Report; }
        }

        public StatusReport Status
        {
            get { return _statusUpdateService.Report; }
        }

        public string TextData { get; set; }
    }
}

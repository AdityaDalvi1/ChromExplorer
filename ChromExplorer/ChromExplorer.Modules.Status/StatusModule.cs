using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using ModuleTracking;

namespace ChromExplorer.Modules.Status
{
    [ModuleExport(typeof(StatusModule))]
    public class StatusModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [ImportingConstructor]
        public StatusModule(ILoggerFacade logger, IModuleTracker moduleTracker)
        {
            
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            if (moduleTracker == null)
            {
                throw new ArgumentNullException("moduleTracker");
            }

            this._logger = logger;
            this._moduleTracker = moduleTracker;
            this._moduleTracker.RecordModuleConstructed(WellKnownModuleNames.StatusModule);
        }

        public void Initialize()
        {
            this._logger.Log("StatusModule demonstrates logging during Initialize().", Category.Info, Priority.Medium);
            this._moduleTracker.RecordModuleInitialized(WellKnownModuleNames.StatusModule);
        }
    }
}

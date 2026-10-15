using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using ModuleTracking;


namespace ChromExplorer.Modules.Lc
{

    [ModuleExport(typeof(LcModule))]
    public class LcModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [ImportingConstructor]
        public LcModule(ILoggerFacade logger, IModuleTracker moduleTracker)
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
            this._moduleTracker.RecordModuleConstructed(WellKnownModuleNames.LcModule);
        }

        public void Initialize()
        {
            this._logger.Log("LcModule demonstrates logging during Initialize().", Category.Info, Priority.Medium);
            this._moduleTracker.RecordModuleInitialized(WellKnownModuleNames.LcModule);
        }
    }
}
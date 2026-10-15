using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using ModuleTracking;

namespace ChromExplorer.Modules.Quan
{
    [ModuleExport(typeof(QuanModule))]
    public class QuanModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public QuanModule(ILoggerFacade logger, IModuleTracker moduleTracker)
        {

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (moduleTracker == null)
            {
                throw new ArgumentNullException(nameof(moduleTracker));
            }

            this._logger = logger;
            this._moduleTracker = moduleTracker;
            this._moduleTracker.RecordModuleConstructed(WellKnownModuleNames.HelpModule);
        }

        public void Initialize()
        {
            this._logger.Log("HelpModule demonstrates logging during Initialize().", Category.Info, Priority.Medium);
            this._moduleTracker.RecordModuleInitialized(WellKnownModuleNames.HelpModule);
        }




    }
}

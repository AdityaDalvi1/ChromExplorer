using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using ModuleTracking;


namespace ChromExplorer.Modules.Licensing
{
    [ModuleExport(typeof(LicensingModule))]
    public class LicensingModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public LicensingModule(ILoggerFacade logger, IModuleTracker moduleTracker)
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
            this._moduleTracker.RecordModuleConstructed(WellKnownModuleNames.LicensingModule);
        }

        public void Initialize()
        {
            this._logger.Log("SettingsModule demonstrates logging during Initialize().", Category.Info, Priority.Medium);
            this._moduleTracker.RecordModuleInitialized(WellKnownModuleNames.LicensingModule);
        }
    }
}
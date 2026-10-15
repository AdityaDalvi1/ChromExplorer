using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using ModuleTracking;

namespace ChromExplorer.Modules.Ms
{
    [ModuleExport(typeof(MsModule))]
    public class MsModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public MsModule(ILoggerFacade logger, IModuleTracker moduleTracker)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            if (moduleTracker == null)
            {
                throw new ArgumentNullException("moduleTracker");
            }

            _logger = logger;
            _moduleTracker = moduleTracker;
            _moduleTracker.RecordModuleConstructed(WellKnownModuleNames.MsModule);
        }

        public void Initialize()
        {
            _logger.Log("Initializing Syringe Module", Category.Info, Priority.None);
           _moduleTracker.RecordModuleInitialized(WellKnownModuleNames.MsModule);
        }   
    }
}

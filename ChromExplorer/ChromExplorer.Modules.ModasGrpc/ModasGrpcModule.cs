using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using ModuleTracking;

namespace ChromExplorer.Modules.ModasGrpc
{
    [ModuleExport(typeof(ModasGrpcModule))]
    public class ModasGrpcModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public ModasGrpcModule(ILoggerFacade logger, IModuleTracker moduleTracker)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (moduleTracker == null)
            {
                throw new ArgumentNullException(nameof(moduleTracker));
            }

            _logger = logger;
            _moduleTracker = moduleTracker;
            _moduleTracker.RecordModuleConstructed(WellKnownModuleNames.ModasGrpcModule);
        }

        public void Initialize()
        {
            _logger.Log("ModasGrpcModule initialized (embedded MT Remote gRPC client UI).", Category.Info, Priority.Medium);
            _moduleTracker.RecordModuleInitialized(WellKnownModuleNames.ModasGrpcModule);
        }
    }
}

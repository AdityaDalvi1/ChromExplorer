using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using ModuleTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

namespace ChromExplorer.Modules.Auth
{
   

    [ModuleExport(typeof(AuthModule))]
    public class AuthModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public AuthModule(ILoggerFacade logger, IModuleTracker moduleTracker)
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
            _moduleTracker.RecordModuleConstructed(WellKnownModuleNames.SyringeModule);
        }

        public void Initialize()
        {
            _logger.Log("Initializing Syringe Module", Category.Info, Priority.None);

            _moduleTracker.RecordModuleInitialized(WellKnownModuleNames.SyringeModule);
        }
    }
}

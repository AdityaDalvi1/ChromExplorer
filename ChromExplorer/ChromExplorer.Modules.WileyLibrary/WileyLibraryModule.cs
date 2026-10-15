using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using ModuleTracking;

namespace ChromExplorer.Modules.WileyLibrary
{
  
    [ModuleExport(typeof(WileyLibraryModule))]
    public class WileyLibraryModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public WileyLibraryModule(ILoggerFacade logger, IModuleTracker moduleTracker)
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
            this._moduleTracker.RecordModuleConstructed(WellKnownModuleNames.WileyLibrary);
        }

        public void Initialize()
        {
            this._logger.Log("SettingsModule demonstrates logging during Initialize().", Category.Info, Priority.Medium);
            this._moduleTracker.RecordModuleInitialized(WellKnownModuleNames.WileyLibrary);
        }




    }
}

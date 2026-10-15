using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using ModuleTracking;

namespace ChromExplorer.Modules.Viewer
{
    [ModuleExport(typeof(ViewerModule), DependsOnModuleNames = new []{ "QuanModule" })]
    public class ViewerModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public ViewerModule(ILoggerFacade logger, IModuleTracker moduleTracker)
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
            this._moduleTracker.RecordModuleConstructed(WellKnownModuleNames.ModuleA);
        }

        public void Initialize()
        {
            this._logger.Log("ViewerModule demonstrates logging during Initialize().", Category.Info, Priority.Medium);
            this._moduleTracker.RecordModuleInitialized(WellKnownModuleNames.ViewerModule);
        }

       

      
    }
}

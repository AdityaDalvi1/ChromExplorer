using System;
using System.ComponentModel.Composition;
using ChromExplorer.Modules.Syringe.Settings;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using ModuleTracking;
using Serial.Utils;

namespace ChromExplorer.Modules.Syringe
{
    [ModuleExport(typeof(SyringeModule))]
    public class SyringeModule : IModule
    {
        private readonly ILoggerFacade _logger;
        private readonly IModuleTracker _moduleTracker;

        [Import]
        public IRegionManager RegionManager;

        [ImportingConstructor]
        public SyringeModule(ILoggerFacade logger, IModuleTracker moduleTracker)
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

            var settings = SyringeModuleSettings.Default;
            

            if (settings.Port == null)
            {
                settings.Port = new PortConfiguration { Name = "COM9", BaudRate = BaudRateDescriptor.BaudRates[6] };
                settings.Save();

            }

        }
    }
}

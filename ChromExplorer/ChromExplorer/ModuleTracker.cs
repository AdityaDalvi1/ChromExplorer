using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Logging;
using ModuleTracking;

namespace ChromExplorer
{
    [Export(typeof(IModuleTracker))]
    public class ModuleTracker : IModuleTracker
    {
        [Import]
        private ILoggerFacade _logger;

        public void RecordModuleDownloading(string moduleName, long bytesReceived, long totalBytesToReceive)
        {
            _logger.Log(string.Format("{0} module downloading.", moduleName), Category.Debug, Priority.Low);
        }

        public void RecordModuleLoaded(string moduleName)
        {
            _logger.Log(string.Format("{0} module loaded.", moduleName), Category.Debug, Priority.Low);
        }

        public void RecordModuleConstructed(string moduleName)
        {
            _logger.Log(string.Format("{0} module constructed.", moduleName), Category.Debug, Priority.Low);
        }

        public void RecordModuleInitialized(string moduleName)
        {
            _logger.Log(string.Format("{0} module initialized.", moduleName), Category.Debug, Priority.Low);
        }
    }
}

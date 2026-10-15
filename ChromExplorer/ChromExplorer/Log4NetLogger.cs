using System.Reflection;
using log4net;
using Microsoft.Practices.Prism.Logging;

namespace ChromExplorer
{
    public class Log4NetLogger : ILoggerFacade
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Log(string message, Category category, Priority priority)
        {
            switch (category)
            {
                case Category.Debug:
                    log.DebugFormat("[{0}] {1}", priority, message);
                    break;

                case Category.Info:
                    log.InfoFormat("[{0}] {1}", priority, message);
                    break;

                case Category.Warn:
                    log.WarnFormat("[{0}] {1}", priority, message);
                    break;

                case Category.Exception:
                    log.ErrorFormat("[{0}] {1}", priority, message);
                    break;
            }
        }
    }
}

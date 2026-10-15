using System.Reflection;
using log4net;
using Mindscape.LightSpeed.Logging;

namespace ChromExplorer.ModasReader
{
    public class LightSpeedLogger : ILogger
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void LogSql(object sql)
        {
            log.Debug(sql);
        }

        public void LogDebug(object text)
        {
            log.Debug(text);
        }
    }

}

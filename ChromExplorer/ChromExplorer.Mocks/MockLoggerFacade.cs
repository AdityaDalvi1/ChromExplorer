using System;
using Microsoft.Practices.Prism.Logging;

namespace ChromExplorer.Mocks
{
    public class MockLoggerFacade : ILoggerFacade
    {
        public void Log(string message, Category category, Priority priority)
        {
            Console.WriteLine((string) "{0}: {1} {2}", (object) message, (object) category, (object) priority);
        }
    }
}

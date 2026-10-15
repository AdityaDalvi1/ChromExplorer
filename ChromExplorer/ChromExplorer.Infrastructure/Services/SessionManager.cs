using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Infrastructure.Services
{
    public static class SessionManager
    {
        public static string Username { get; set; }

        /// <summary>
        /// Raised when something (e.g. the logout button, or an idle timeout) wants the
        /// current session ended. The app shell (App.xaml.cs) subscribes to this so modules
        /// can request a logout without taking a direct reference to the host application.
        /// </summary>
        public static event Action<bool> LogoutRequested;

        public static void RequestLogout(bool isAutoLogout)
        {
            LogoutRequested?.Invoke(isAutoLogout);
        }
    }
}

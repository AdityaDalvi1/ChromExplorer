using ChromExplorer.Modules.Auth.Services;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Modularity;
using ModuleTracking;

namespace ChromExplorer
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    [Export]
    public partial class Shell : Window, IPartImportsSatisfiedNotification
    {

        // The shell imports IModuleTracker once to record updates as modules are downloaded.        
        [Import(AllowRecomposition = false)]
        private IModuleTracker _moduleTracker;

        // The shell imports IModuleManager once to load modules on-demand.
        [Import(AllowRecomposition = false)]
        private IModuleManager _moduleManager;

        // The shell imports the logger once to output logs to the UI.
        //[Import(AllowRecomposition = false)]
        //private CallbackLogger _logger;

        public Shell()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Logs the specified message.  Called by the CallbackLogger.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="priority">The priority.</param>
        public void Log(string message, Category category, Priority priority)
        {
            //this.TraceTextBox.AppendText(
            //                                string.Format(
            //                                            CultureInfo.CurrentUICulture,
            //                                            "[{0}][{1}] {2}\r\n",
            //                                            category,
            //                                            priority,
            //                                            message));
        }

        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        public void OnImportsSatisfied()
        {
            // IPartImportsSatisfiedNotification is useful when you want to coordinate doing some work
            // with imported parts independent of when the UI is visible.

            // I use the IModuleTracker as the data-context for data-binding.
            // This quickstart only demonstrates modularity for Prism and does not use data-binding patterns such as MVVM.
            this.DataContext = this._moduleTracker;

            // I set this shell's Log method as the callback for receiving log messages.
            //this._logger.Callback = this.Log;
            //this._logger.ReplaySavedLogs();

            // I subscribe to events to help track module loading/loaded progress.
            // The ModuleManager uses the Async Events Pattern.
            this._moduleManager.LoadModuleCompleted += this.ModuleManagerLoadModuleCompleted;
            this._moduleManager.ModuleDownloadProgressChanged += this.ModuleManagerModuleDownloadProgressChanged;
            
        }

        /// <summary>
        /// Handles the LoadModuleProgressChanged event of the ModuleManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Practices.Composite.Modularity.ModuleDownloadProgressChangedEventArgs"/> instance containing the event data.</param>
        private void ModuleManagerModuleDownloadProgressChanged(object sender, ModuleDownloadProgressChangedEventArgs e)
        {
            this._moduleTracker.RecordModuleDownloading(e.ModuleInfo.ModuleName, e.BytesReceived, e.TotalBytesToReceive);
        }

        /// <summary>
        /// Handles the LoadModuleCompleted event of the ModuleManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Practices.Composite.Modularity.LoadModuleCompletedEventArgs"/> instance containing the event data.</param>
        private void ModuleManagerLoadModuleCompleted(object sender, LoadModuleCompletedEventArgs e)
        {
            this._moduleTracker.RecordModuleLoaded(e.ModuleInfo.ModuleName);
        }

        /// <summary>
        /// Refreshes the "logged in as" indicator in the top-right corner. Called by App.xaml.cs
        /// right after the Shell is shown post-login, since the Shell itself is only ever
        /// hidden/re-shown across logins (never recreated), so this needs to be refreshed
        /// explicitly for each new session rather than bound once at construction.
        /// </summary>
        public void UpdateLoggedInUserDisplay()
        {
            string username = Application.Current.Properties.Contains("Username")
                ? Application.Current.Properties["Username"]?.ToString()
                : null;

            if (string.IsNullOrEmpty(username))
            {
                loggedInUserBadge.Visibility = Visibility.Collapsed;
                return;
            }

            txtLoggedInUser.Text = username;
            loggedInUserBadge.Visibility = Visibility.Visible;

            string role = new AuthService().GetCurrentUserRoleName();
            if (string.IsNullOrEmpty(role))
            {
                loggedInUserRoleBadge.Visibility = Visibility.Collapsed;
                return;
            }

            txtLoggedInUserRole.Text = role.ToUpperInvariant();
            loggedInUserRoleBadge.Background = RoleBadgeColor(role);
            loggedInUserRoleBadge.Visibility = Visibility.Visible;
        }

        // A little color-coding so the role badge reads at a glance — Reviewer matches the same
        // purple used for the "Review" action button elsewhere in the app, Admin/System roles get
        // the app's primary blue, anything else falls back to a neutral gray.
        private static System.Windows.Media.Brush RoleBadgeColor(string role)
        {
            if (role.IndexOf("Reviewer", StringComparison.OrdinalIgnoreCase) >= 0)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6A, 0x1B, 0x9A));

            if (role.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x19, 0x76, 0xD2));

            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x75, 0x75, 0x75));
        }

        private void Shell_Closing(object sender, CancelEventArgs e)
        {
            var app = Application.Current as App;
            if (app != null && app.SkipExitConfirmation)
            {
                // Already shutting down for a reason the user confirmed elsewhere
                // (e.g. declined to log back in) — don't ask "close?" again.
                return;
            }

            var confirm = MessageBox.Show(
                "Are you sure you want to close ChromExplorer?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            var authService = new AuthService();

            if (confirm == MessageBoxResult.Yes)
            {
                try { authService.Log("Application Closed", "User confirmed application close (clicked Yes)"); }
                catch { }
            }
            else
            {
                e.Cancel = true;
                try { authService.Log("Application Close Cancelled", "User cancelled application close (clicked No)"); }
                catch { }
            }
        }
    }

    
}

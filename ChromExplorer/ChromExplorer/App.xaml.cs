// ============================================================
// MERGED: App.xaml.cs
// Folder A: Added crash logging (try/catch around bootstrapper)
// Folder B: Added LoginWindow integration and Auth service integration
// Merge strategy: Keep Folder B's login flow + add Folder A's crash logging
// ============================================================

using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Services;
using System.ComponentModel.Composition.Hosting;
using ChromExplorer.Modules.Auth.Services;
using ChromExplorer.Properties;
using ChromExplorer.Views;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ChromExplorer.Modules.Auth.Views;

namespace ChromExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly int IdleTimeoutMinutes = 5;
        private DispatcherTimer _idleTimer;
        private DateTime _lastActivityUtc;
        private ChromExplorerBootstraper _bootstrapper;

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }




        // System-wide idle time (mouse/keyboard, any window) via the OS — not dependent on
        // WPF's internal input plumbing, which is scoped to this app's dispatcher and can
        // miss/duplicate events in edge cases.
        private static TimeSpan GetSystemIdleTime()
        {
            var info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);

            if (!GetLastInputInfo(ref info))
                return TimeSpan.Zero;

            uint idleTicks = unchecked((uint)Environment.TickCount - info.dwTime);
            return TimeSpan.FromMilliseconds(idleTicks);
        }

        /// <summary>
        /// When true, Shell's Closing handler skips its own Yes/No confirmation — used when
        /// the app is already shutting down for a reason the user already confirmed
        /// (e.g. declining to log back in), so we don't ask "close?" a second time.
        /// </summary>
        public bool SkipExitConfirmation { get; private set; }

        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.Properties["Args"] = e.Args;

            // Modules (e.g. the logout button) can't take a direct reference to this
            // exe project without creating a circular reference, so they request logout
            // through this shared event instead.
            SessionManager.LogoutRequested += Logout;

            // Catches crashes on any non-UI thread (DispatcherUnhandledException below only
            // covers the UI thread) — registered as early as possible so nothing slips through.
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

            // The bootstrapper creates the Shell. App.xaml does not use StartupUri.
            // MERGED [Folder A]: Wrapped in try/catch for crash logging on startup errors.
            try
            {
                _bootstrapper = new ChromExplorerBootstraper();
                _bootstrapper.Run();

                new AuthService().EnsurePassTransactionTable();
                new AuthService().EnsureFirstLoginColumns();
                new AuthService().EnsureReviewerFeatureSeed();
                new AuthService().RecordApplicationStart();

                // MERGED [Folder B]: Hide the Shell (MainWindow) until login succeeds.
                Application.Current.MainWindow.Hide();

                ShowLoginFlow();
            }
            catch (Exception ex)
            {
                // MERGED [Folder A]: Write crash log and show user-friendly error dialog.
                System.IO.File.WriteAllText("crash.log", ex.ToString());
                try { new AuthService().RecordApplicationCrash(ex.ToString()); } catch { }
                MessageBox.Show(ex.ToString(), "ChromExplorer Startup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string msg = e.ExceptionObject?.ToString() ?? "Unknown non-UI-thread exception";
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), msg);
            }
            catch { }

            try { new AuthService().RecordApplicationCrash(msg); } catch { }
        }

        /// <summary>
        /// Shows the login window. On success, shows the Shell and starts the idle-timeout
        /// monitor. On cancel, shuts down the whole application.
        /// </summary>
        private void ShowLoginFlow()
        {
            StopIdleMonitor();

            LoginWindow login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result == true)
            {
                Application.Current.MainWindow.Show();
                (Application.Current.MainWindow as Shell)?.UpdateLoggedInUserDisplay();
                StartIdleMonitor();
            }
            else
            {
                // Shell_Closing skips its own logging when SkipExitConfirmation is set, so this
                // is the only place this exit path gets recorded as a normal, expected shutdown.
                // Covers both "Exit Application" clicked on the login screen and declining to
                // log back in after a logout.
                try { new AuthService().Log("Application Closed", $"Application closed from login screen (Exit Application clicked) | {DateTime.Now:dd-MM-yyyy HH:mm:ss}"); } catch { }

                SkipExitConfirmation = true;
                Shutdown();
            }
        }

        /// <summary>
        /// Ends only the current user session — clears session state, hides the Shell,
        /// and returns to the login window. Does not terminate the application process.
        /// </summary>
        public void Logout(bool isAutoLogout)
        {
            try
            {
                var authService = new AuthService();
                authService.Log(
                    isAutoLogout ? "Auto Logout" : "Logout",
                    isAutoLogout
                        ? $"User automatically logged out after {IdleTimeoutMinutes} minutes of inactivity"
                        : "User logged out from system");
            }
            catch { }

            StopIdleMonitor();
            ClearOverContentRegion();
            ResetMainContentToHome();
            ClearPendingReportQueue();
            Application.Current.Properties.Clear();
            Application.Current.MainWindow.Hide();
            ShowLoginFlow();
        }

        // ReportSessionService (the "+ Add to Report" queue) is a MEF-shared singleton that also
        // persists to a per-machine disk file (see its own doc comment) specifically so it
        // survives an app restart/crash — but that also means it survives a Logout, which would
        // otherwise leave the next person who logs into this same running app.exe staring at
        // whatever files the PREVIOUS user had queued but never generated into a report. Wipe it
        // here so every session starts with an empty queue.
        private void ClearPendingReportQueue()
        {
            try
            {
                var session = _bootstrapper?.ExportedContainer
                    .GetExportedValue<ChromExplorer.Modules.Viewer.Services.ReportSessionService>();

                int queuedCount = session?.Count ?? 0;
                session?.Clear();

                new AuthService().Log("Logout - Report Queue Cleared",
                    $"Pending 'Add to Report' queue cleared on logout | Files that were queued: {queuedCount}");
            }
            catch (Exception ex)
            {
                try
                {
                    new AuthService().Log("Logout - Report Queue Clear Failed",
                        $"Could not clear the pending 'Add to Report' queue on logout | Error: {ex.Message}");
                }
                catch { }
            }
        }

        // The Shell window is only hidden/shown across a logout, never recreated, so whatever
        // page was navigated to in MainContentArea (MS Control, ModasGrpc dashboard, etc.) stays
        // active and would otherwise still be showing for the next user who logs in. Bring the
        // Home screen back to the front instead.
        private void ResetMainContentToHome()
        {
            try
            {
                var regionManager = RegionManager.GetRegionManager(Application.Current.MainWindow);
                if (regionManager == null || !regionManager.Regions.ContainsRegionWithName(RegionNames.MainContentArea))
                    return;

                var region = regionManager.Regions[RegionNames.MainContentArea];
                var homeView = region.Views.OfType<HomeScreenView>().FirstOrDefault();
                if (homeView != null)
                {
                    region.Activate(homeView);
                }
            }
            catch { }
        }

        // The Shell window is only hidden/shown across a logout, never recreated, so whatever
        // overlay view (User Details, Assign Role, etc.) was open in OverContentRegion stays
        // active and would otherwise still be showing over the Home screen for the next user.
        private void ClearOverContentRegion()
        {
            try
            {
                var regionManager = RegionManager.GetRegionManager(Application.Current.MainWindow);
                if (regionManager == null || !regionManager.Regions.ContainsRegionWithName(RegionNames.OverContentRegion))
                    return;

                var region = regionManager.Regions[RegionNames.OverContentRegion];
                foreach (var view in region.ActiveViews.ToList())
                {
                    region.Deactivate(view);
                }
            }
            catch { }
        }

        private void StartIdleMonitor()
        {
            _lastActivityUtc = DateTime.UtcNow;
            InputManager.Current.PreProcessInput += OnGlobalActivity;

            if (_idleTimer == null)
            {
                _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _idleTimer.Tick += IdleTimer_Tick;
            }
            _idleTimer.Start();
        }

        private void StopIdleMonitor()
        {
            InputManager.Current.PreProcessInput -= OnGlobalActivity;
            _idleTimer?.Stop();
        }

        private void OnGlobalActivity(object sender, PreProcessInputEventArgs e)
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            // GetSystemIdleTime (Win32 GetLastInputInfo) can report a bogus, huge value in some
            // remote/automated/virtualized sessions where Windows never updates the last-input
            // timestamp normally. OnGlobalActivity (WPF's own input pipeline) always works
            // regardless of environment. Trust whichever signal shows the most recent activity —
            // only log out if BOTH agree the user has actually been idle long enough.
            var appLevelIdle = DateTime.UtcNow - _lastActivityUtc;
            var systemIdle = GetSystemIdleTime();
            var effectiveIdle = appLevelIdle < systemIdle ? appLevelIdle : systemIdle;

            if (effectiveIdle.TotalMinutes >= IdleTimeoutMinutes)
            {
                Logout(isAutoLogout: true);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var comException = e.Exception as COMException;
            if (comException != null && comException.ErrorCode == -2147221040)
            {
                e.Handled = true;
                return;
            }

            // Log all other unhandled dispatcher exceptions so we can diagnose crashes.
            string msg = e.Exception?.ToString() ?? "Unknown exception";
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"),
                msg);
            try { new AuthService().RecordApplicationCrash(msg); } catch { }
            MessageBox.Show(msg, "Unhandled Exception — see crash.log",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevent hard crash so we can read the message
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}

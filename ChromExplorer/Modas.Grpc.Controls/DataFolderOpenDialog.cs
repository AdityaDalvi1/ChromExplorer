namespace Modas.Grpc.Controls
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;

    public class DataFolderOpenDialog
    {
        const int WM_GETTEXT = 0x000D;
        const int WM_SETTEXT = 0x000C;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        // When requiredDrive is given (e.g. "D"), a folder on any other drive is refused with a
        // message and the picker is shown again — it only ever returns folders on that drive
        // (or null if the user cancels).
        public static string[] ShowFolder(string initialPath, string title, string requiredDrive)
        {
            while (true)
            {
                var result = ShowFolder(initialPath, title);
                if (result == null || string.IsNullOrEmpty(requiredDrive))
                    return result;

                string prefix = requiredDrive.TrimEnd(':', '\\') + ":";
                if (result.All(p => p != null && p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    return result;

                System.Windows.MessageBox.Show(
                    "Only folders on the " + prefix + " drive can be used.\n\nPlease choose a folder on " + prefix + "\\.",
                    "Folder Not Allowed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        public static string[] ShowFolder(string initialPath = "", string title = "")
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = true,
                Title = string.IsNullOrEmpty(title) ? "Open Data Files" : title,
                InitialDirectory = initialPath,
                DefaultDirectory = initialPath
            };

            // Start a task that will look for the dialog window and rename the button
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            Task.Run(() =>
            {
                // try for ~2 seconds
                var deadline = DateTime.UtcNow.AddSeconds(2);
                while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
                {
                    try
                    {
                        // Find top-level dialog window by title
                        IntPtr hDlg = FindWindow("#32770", dlg.Title);
                        if (hDlg != IntPtr.Zero)
                        {
                            var changed = 0;
                            EnumChildWindows(hDlg, (hwnd, lparam) =>
                            {
                                var sb = new StringBuilder(256);
                                SendMessage(hwnd, WM_GETTEXT, (IntPtr)sb.Capacity, sb);
                                var txt = sb.ToString();

                                // heuristics: look for the button text you expect; adjust for localization
                                if (txt.IndexOf("Select Folder", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    SendMessage(hwnd, WM_SETTEXT, IntPtr.Zero, "Open Data File");
                                    changed++;
                                    return false; // stop enumeration
                                }

                                if (txt.IndexOf("Folder", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    SendMessage(hwnd, WM_SETTEXT, IntPtr.Zero, "Data File");
                                    changed++;
                                    return false; // stop enumeration
                                }


                                return true; // continue
                            }, IntPtr.Zero);

                            if (changed == 2) break;
                        }
                    }
                    catch { /* swallow errors; continue retrying */ }

                    Thread.Sleep(80);
                }
            }, token);

            // Show the dialog (blocking). When ShowDialog returns cancel the worker.
            try
            {
                var res = dlg.ShowDialog();

                if (res == CommonFileDialogResult.Ok)
                {
                    return dlg.FileNames.ToArray();
                }
            }
            finally
            {
                cts.Cancel();
            }

            return null;
        }
    }
}
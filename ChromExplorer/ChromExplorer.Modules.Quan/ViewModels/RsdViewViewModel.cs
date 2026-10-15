using ChromExplorer.Modules.Quan.Model;
using ChromExplorer.Modules.Quan.Services;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;

namespace ChromExplorer.Modules.Quan.ViewModels
{
    /// <summary>Backs the RSD tab: a live view of RsdService's queued entries plus the
    /// per-concentration %RSD summary, refreshed whenever the service's queue changes (including
    /// from "Add to RSD" on the chromatogram view, which shares the same MEF-singleton
    /// RsdService). Also owns Save list / Open list for the RSD data.</summary>
    [Export(typeof(RsdViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RsdViewViewModel
    {
        // Fixed location — the user is never asked where to save.
        public const string SaveFolder = @"D:\RSD data";
        public const string FileExtension = ".rsd";

        private const string ConnectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        private readonly RsdService _rsdService;

        [ImportingConstructor]
        public RsdViewViewModel(RsdService rsdService)
        {
            _rsdService = rsdService;

            Entries = new ObservableCollection<RsdPeakInfo>(_rsdService.Entries);
            Summaries = new ObservableCollection<RsdGroupSummary>(_rsdService.ComputeGroupSummaries());

            _rsdService.EntriesChanged += (s, e) => Refresh();

            DeleteSelectedCommand = new DelegateCommand<RsdPeakInfo>(OnDeleteSelected);
            ClearAllCommand = new DelegateCommand(OnClearAll);
            SaveFileCommand = new DelegateCommand(OnSaveFile);
            OpenFileCommand = new DelegateCommand(OnOpenFile);
        }

        public ObservableCollection<RsdPeakInfo> Entries { get; }
        public ObservableCollection<RsdGroupSummary> Summaries { get; }

        public DelegateCommand<RsdPeakInfo> DeleteSelectedCommand { get; }
        public DelegateCommand ClearAllCommand { get; }
        public DelegateCommand SaveFileCommand { get; }
        public DelegateCommand OpenFileCommand { get; }

        private void OnDeleteSelected(RsdPeakInfo entry)
        {
            if (entry == null) return;

            if (!ReasonPasswordPrompt.TryAuthorize("RSD - Remove File", "Reason for Removing this File", out string reason))
                return;

            _rsdService.Remove(entry);
            InsertLog("RSD File Removed", $"Removed '{entry.FilePath}' (Sample ID: {entry.SampleID}) | Reason: {reason}");
        }

        private void OnClearAll()
        {
            if (!_rsdService.Entries.Any())
                return;

            var answer = MessageBox.Show(
                "This will remove all queued files and their %RSD results from this tab.\nUnsaved data will be lost. Do you want to continue?",
                "Clear All", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes)
                return;

            if (!ReasonPasswordPrompt.TryAuthorize("RSD - Clear All", "Reason for Clearing All Files", out string reason))
                return;

            int count = _rsdService.Entries.Count();
            _rsdService.Clear();
            InsertLog("RSD List Cleared", $"All RSD entries cleared | Entries: {count} | Reason: {reason}");
        }

        private void OnSaveFile()
        {
            if (!_rsdService.Entries.Any())
            {
                MessageBox.Show("There is no RSD data to save. Add files to RSD first.",
                    "Save RSD List", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!EnsureSaveFolder())
                return;

            string filePath = Path.Combine(SaveFolder, "RSD_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + FileExtension);

            if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(filePath, "RSD data file"))
                return;

            try
            {
                _rsdService.SaveData(filePath);
                InsertLog("RSD List Saved", "Saved to: " + filePath);
                MessageBox.Show("RSD list saved here:\n" + filePath,
                    "RSD List Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                InsertLog("RSD List Save Failed", ex.Message);
                MessageBox.Show("Error saving RSD list: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenFile()
        {
            if (_rsdService.Entries.Any())
            {
                var answer = MessageBox.Show(
                    "Opening a saved RSD list replaces the files currently queued on this tab. Continue?",
                    "Open RSD List", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                    return;
            }

            var dlg = new OpenFileDialog
            {
                Title = "Open RSD List",
                DefaultExt = FileExtension,
                Filter = "RSD data (*.rsd)|*.rsd",
                InitialDirectory = Directory.Exists(SaveFolder) ? SaveFolder : null
            };

            if (!ChromExplorer.Infrastructure.FileStorage.ShowOpenDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.RsdLists, "RSD list"))
                return;

            try
            {
                int count = _rsdService.LoadData(dlg.FileName);
                InsertLog("RSD List Opened", $"Opened: {dlg.FileName} | Entries: {count}");
            }
            catch (Exception ex)
            {
                InsertLog("RSD List Open Failed", ex.Message);
                MessageBox.Show("Could not open this file as an RSD list:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool EnsureSaveFolder()
        {
            try
            {
                string root = Path.GetPathRoot(SaveFolder);
                if (!string.IsNullOrEmpty(root) && !Directory.Exists(root))
                    throw new DirectoryNotFoundException($"Drive '{root}' is not available on this computer.");

                Directory.CreateDirectory(SaveFolder);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"RSD lists are saved to:\n{SaveFolder}\n\nThis folder could not be created or written to:\n{ex.Message}",
                    "RSD Folder Unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static void InsertLog(string action, string description)
        {
            try
            {
                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"INSERT INTO AuditLogM
                          (action, description, is_active, inserted_by, inserted_date)
                          VALUES (@action, @desc, 1, @uid, datetime('now','localtime'))", con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private void Refresh()
        {
            Entries.Clear();
            foreach (var entry in _rsdService.Entries)
                Entries.Add(entry);

            Summaries.Clear();
            foreach (var summary in _rsdService.ComputeGroupSummaries())
                Summaries.Add(summary);
        }
    }
}

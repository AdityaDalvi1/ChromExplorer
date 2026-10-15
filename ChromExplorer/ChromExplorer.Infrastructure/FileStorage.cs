using System;
using System.Configuration;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ChromExplorer.Infrastructure
{
    public enum StorageCategory
    {
        DataProcessingMethods,
        InstrumentMethods,
        SampleSequences,
        Exports,
        Quantitation,
        RsdLists,
        PendingReports
    }

    // One fixed home for every file the user saves or opens from inside the app (Data Processing
    // Methods, Sample Sequences, instrument methods, exports, Quan/RSD lists). Reports have their
    // own equivalent in ReportStorage. A Windows file dialog can't be locked to a folder, so
    // ShowSaveDialog/ShowOpenDialog start in the right folder and re-open the dialog (with a
    // message and an audit entry) whenever the user picks a file outside it.
    // The root comes from appSettings["DataRootFolder"] and defaults to D:\ChromExplorer.
    public static class FileStorage
    {
        public const string DefaultRootFolder = @"D:\ChromExplorer";

        // Instrument methods already live here on existing installs — kept as is.
        public const string InstrumentMethodsFolder = @"D:\ChromMethods";

        public static string RootFolder
        {
            get
            {
                var configured = ConfigurationManager.AppSettings["DataRootFolder"];
                return string.IsNullOrWhiteSpace(configured) ? DefaultRootFolder : configured.Trim();
            }
        }

        public static string GetFolder(StorageCategory category)
        {
            switch (category)
            {
                case StorageCategory.InstrumentMethods: return InstrumentMethodsFolder;
                case StorageCategory.SampleSequences: return Path.Combine(RootFolder, "SampleSequences");
                case StorageCategory.Exports: return Path.Combine(RootFolder, "Exports");
                case StorageCategory.Quantitation: return Path.Combine(RootFolder, "Quantitation");
                case StorageCategory.PendingReports: return Path.Combine(RootFolder, "PendingReports");
                case StorageCategory.RsdLists: return @"D:\RSD data";
                default: return Path.Combine(RootFolder, "DataProcessingMethods");
            }
        }

        // Creates the folder if needed and returns it, or null (after telling the user) when it
        // can't be created/written — e.g. the machine has no D: drive — so the caller stops
        // instead of saving somewhere else.
        public static string EnsureFolder(StorageCategory category)
        {
            string folder = GetFolder(category);
            try
            {
                string root = Path.GetPathRoot(folder);
                if (!string.IsNullOrEmpty(root) && !Directory.Exists(root))
                    throw new DirectoryNotFoundException($"Drive '{root}' is not available on this computer.");

                Directory.CreateDirectory(folder);
                return folder;
            }
            catch (Exception ex)
            {
                SavedFileRegistry.WriteAudit("Storage Folder Unavailable",
                    $"Could not use folder '{folder}' | Error: {ex.Message}");
                MessageBox.Show(
                    $"Files of this type are stored in:\n{folder}\n\nThis folder could not be created or used:\n{ex.Message}\n\n" +
                    "Please ask your administrator to make this location available, then try again.",
                    "Storage Folder Unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // True when `path` is the category folder or anywhere beneath it.
        public static bool IsInside(string path, StorageCategory category)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                string folder = Path.GetFullPath(GetFolder(category)).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                string full = Path.GetFullPath(path);
                return full.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // True when `path` is inside ANY of the app's storage folders (the categories above plus
        // the report folder) — the last-line check every save goes through in
        // FileSaveGuard.CanSaveTo, so a save can't land elsewhere even if a dialog wasn't gated.
        public static bool IsInsideAnyAllowedFolder(string path)
        {
            foreach (StorageCategory category in Enum.GetValues(typeof(StorageCategory)))
            {
                if (IsInside(path, category))
                    return true;
            }

            try
            {
                string reports = Path.GetFullPath(ReportStorage.RootFolder).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                return Path.GetFullPath(path).StartsWith(reports, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // Shows the Save dialog starting in the category folder; keeps re-opening it until the
        // chosen file is inside that folder or the user cancels. Returns true with dlg.FileName set.
        public static bool ShowSaveDialog(SaveFileDialog dlg, StorageCategory category, string itemDescription)
        {
            string folder = EnsureFolder(category);
            if (folder == null)
                return false;

            // Our own "already exists" gate (FileSaveGuard) replaces the native replace prompt.
            dlg.OverwritePrompt = false;
            if (!IsInside(Path.Combine(dlg.InitialDirectory ?? "", "x"), category))
                dlg.InitialDirectory = folder;

            while (dlg.ShowDialog() == true)
            {
                if (IsInside(dlg.FileName, category))
                    return true;

                Reject(dlg.FileName, folder, itemDescription, "save");
                dlg.InitialDirectory = folder;
            }
            return false;
        }

        // Same for Open: starts in the category folder and refuses files from anywhere else.
        public static bool ShowOpenDialog(OpenFileDialog dlg, StorageCategory category, string itemDescription)
        {
            string folder = EnsureFolder(category);
            if (folder == null)
                return false;

            dlg.InitialDirectory = folder;

            while (dlg.ShowDialog() == true)
            {
                if (IsInside(dlg.FileName, category))
                    return true;

                Reject(dlg.FileName, folder, itemDescription, "open");
                dlg.InitialDirectory = folder;
            }
            return false;
        }

        private static void Reject(string path, string folder, string itemDescription, string verb)
        {
            SavedFileRegistry.WriteAudit("Storage Location Blocked",
                $"Attempted to {verb} a {itemDescription} outside the allowed folder ({folder}): {path}",
                Path.GetFileName(path) ?? "");

            MessageBox.Show(
                $"A {itemDescription} can only be {(verb == "save" ? "saved to" : "opened from")}:\n{folder}\n\nChoose a file inside this folder.",
                "Location Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

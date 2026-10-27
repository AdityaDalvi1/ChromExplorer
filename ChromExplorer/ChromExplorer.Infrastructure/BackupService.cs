using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows;

namespace ChromExplorer.Infrastructure
{
    // "Backup" button: copies everything the app stores in its fixed D: locations (sample
    // sequences, data processing methods, exports, quantitation, instrument methods, RSD lists,
    // pending reports, all reports) plus the working source.db3 database into ONE new folder named
    // Backup_yyyyMMdd_HHmmss (date and time of the backup) under BackupService.RootFolder.
    // The backup root lives next to - never inside - the folders being copied, so a backup never
    // copies earlier backups. A straight file-copy snapshot: nothing is moved or deleted.
    public static class BackupService
    {
        public const string DefaultRootFolder = @"D:\ChromExplorerBackups";

        private const string DatabaseFileName = "source.db3";

        public static string RootFolder
        {
            get
            {
                var configured = ConfigurationManager.AppSettings["BackupFolder"];
                return string.IsNullOrWhiteSpace(configured) ? DefaultRootFolder : configured.Trim();
            }
        }

        // Runs the whole backup, tells the user how it went, and returns the backup folder path
        // (or null if it failed).
        public static string CreateBackup()
        {
            string backupFolder = Path.Combine(RootFolder, "Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            try
            {
                string root = Path.GetPathRoot(backupFolder);
                if (!string.IsNullOrEmpty(root) && !Directory.Exists(root))
                    throw new DirectoryNotFoundException($"Drive '{root}' is not available on this computer.");

                Directory.CreateDirectory(backupFolder);

                int filesCopied = CopyDatabase(backupFolder);

                // (folder to back up, name it gets inside the backup folder)
                var sources = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>(FileStorage.RootFolder, "ChromExplorer"),
                    new KeyValuePair<string, string>(FileStorage.GetFolder(StorageCategory.InstrumentMethods), "ChromMethods"),
                    new KeyValuePair<string, string>(FileStorage.GetFolder(StorageCategory.RsdLists), "RSD data"),
                    new KeyValuePair<string, string>(ReportStorage.RootFolder, "ChromReport"),
                };

                foreach (var source in sources)
                    filesCopied += CopyFolderIfExists(source.Key, Path.Combine(backupFolder, source.Value), backupFolder);

                SavedFileRegistry.WriteAudit("Backup Created",
                    $"Backup created at '{backupFolder}' | {filesCopied} file(s) copied");

                MessageBox.Show(
                    $"Backup complete.\n\nSaved to:\n{backupFolder}\n\n{filesCopied} file(s) copied.",
                    "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                return backupFolder;
            }
            catch (Exception ex)
            {
                SavedFileRegistry.WriteAudit("Backup Failed",
                    $"Backup to '{backupFolder}' could not be completed | Error: {ex.Message}");

                MessageBox.Show(
                    $"Backup could not be completed:\n{ex.Message}\n\nAttempted location:\n{backupFolder}",
                    "Backup Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }
        }

        // Copied as a plain file (SQLite allows this while the app has it open), so a backup can
        // be taken at any time.
        private static int CopyDatabase(string backupFolder)
        {
            string sourcePath = ResolveDatabasePath();
            if (sourcePath == null)
                return 0;

            File.Copy(sourcePath, Path.Combine(backupFolder, DatabaseFileName), overwrite: true);
            return 1;
        }

        // The app opens source.db3 through a relative connection string, so it normally sits in
        // the working directory; the exe's own folder is the fallback.
        private static string ResolveDatabasePath()
        {
            string inWorkingDir = Path.GetFullPath(DatabaseFileName);
            if (File.Exists(inWorkingDir))
                return inWorkingDir;

            string inBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFileName);
            return File.Exists(inBaseDir) ? inBaseDir : null;
        }

        private static int CopyFolderIfExists(string sourceFolder, string destFolder, string backupFolder)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
                return 0;

            string backupFull = Path.GetFullPath(backupFolder).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            int count = 0;

            foreach (var filePath in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                // Never copy the backup into itself if a source folder happens to contain it.
                if (Path.GetFullPath(filePath).StartsWith(backupFull, StringComparison.OrdinalIgnoreCase))
                    continue;

                string relativePath = filePath.Substring(sourceFolder.Length).TrimStart('\\', '/');
                string destPath = Path.Combine(destFolder, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(filePath, destPath, overwrite: true);
                count++;
            }

            return count;
        }
    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Modas.Grpc.Controls;
using Modas.Grpc.Models;

namespace ChromExplorer.Modules.ModasGrpc.Views
{
    public class GeneralSettingsViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        private double _acquisitionLength;
        private string _modasMethod;
        private string _dataFolder;

        public GeneralSettingsViewModel()
        {
            _acquisitionLength = 10;
            _modasMethod = "some method";
            _dataFolder = "some data folder";

            BrowseInstrumentMethodCommand = new DelegateCommand(OnBrowseMethod);
            BrowseDataFolderCommand = new DelegateCommand(OnBrowseDataFolder);
        }

        public void UpdateFromMethod(AnalysisMethod method)
        {
            AcquisitionLength = method.AcquisitionLengthSeconds;
            ModasMethod = method.ModasMethodFile;
            DataFolder = method.DataFolder;
        }

        private void OnBrowseDataFolder()
        {
            // Data (acquisition) folders live on D: drive same as everything else the app
            // tracks — opening the browser at C:\ made the user navigate across drives every
            // single time.
            var folder = DataFolderOpenDialog.ShowFolder(@"D:\", "Open Data Folder", "D");

            if (folder != null)
            {
                DataFolder = folder.FirstOrDefault();
                if (Method != null)
                    CommitToMethod(Method);

            }

        }

        private void OnBrowseMethod()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 2. Set options (optional)
            openFileDialog.Filter = "Modas method files (*.mtm2)|*.mtm2|All files (*.*)|*.*";

            bool chosen = ChromExplorer.Infrastructure.FileStorage.ShowOpenDialog(openFileDialog,
                ChromExplorer.Infrastructure.StorageCategory.InstrumentMethods, "instrument method file");

            if (chosen)
            {
                ModasMethod = openFileDialog.FileName;

                string fileName = Path.GetFileName(openFileDialog.FileName);
                string selectedBy = Application.Current?.Properties?.Contains("Username") == true
                    ? Application.Current.Properties["Username"].ToString()
                    : "Unknown";
                InsertLog("Browse Instrument Method",
                    $"Instrument method selected by '{selectedBy}' | File: {fileName} | Path: {openFileDialog.FileName}",
                    "", fileName);

                var methodState = ChromExplorer.Infrastructure.SavedFileRegistry.GetReviewState(
                    ChromExplorer.Infrastructure.SavedFileRegistry.TypeMethodFile, openFileDialog.FileName);

                ChromExplorer.Infrastructure.SavedFileRegistry.Record(
                    ChromExplorer.Infrastructure.SavedFileRegistry.TypeMethodFile, openFileDialog.FileName);

                if (methodState.IsRejected)
                {
                    ChromExplorer.Infrastructure.SavedFileRegistry.WriteAudit("Rejected Method File Selected",
                        $"A Rejected Instrument Method was selected | Rejected by: {methodState.ReviewedBy} | Remark: {methodState.Remark}", fileName);
                    // Instrument methods (.mtm2) are authored in Modas, not edited inside ChromExplorer,
                    // so the revision happens there — saved under a new name — and is then selected here.
                    ChromExplorer.Infrastructure.SavedFileRegistry.ShowRejectedNotice(fileName, methodState.ReviewedBy, methodState.Remark,
                        "Edit this method in Modas and save the revised method under a NEW file name (the original is never overwritten), then select the new file here and send it for review.");
                }

                if (Method != null)
                    CommitToMethod(Method);
            }
        }

        private void InsertLog(string action, string description, string parameter, string fileName = "")
        {
            try
            {
                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    var sql = @"INSERT INTO AuditLogM
                              (action, description, parameter, file_name, is_active, inserted_by, inserted_date)
                              VALUES
                              (@action, @desc, @param, @fileName, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@param", parameter ?? string.Empty);
                        cmd.Parameters.AddWithValue("@fileName", fileName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public void CommitToMethod(AnalysisMethod method)
        {
            method.AcquisitionLengthSeconds = AcquisitionLength;
            method.ModasMethodFile = ModasMethod;
            method.DataFolder = DataFolder;
        }

        public double AcquisitionLength
        {
            get => _acquisitionLength;
            set => SetField(ref _acquisitionLength, value);
        }

        public string ModasMethod
        {
            get => _modasMethod;
            set => SetField(ref _modasMethod, value);
        }

        public string DataFolder
        {
            get => _dataFolder;
            set => SetField(ref _dataFolder, value);
        }

        public DelegateCommand BrowseInstrumentMethodCommand { get; private set; }
        public DelegateCommand BrowseDataFolderCommand { get; private set; }
        public AnalysisMethod Method { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
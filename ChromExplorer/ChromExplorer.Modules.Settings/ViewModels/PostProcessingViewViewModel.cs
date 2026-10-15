using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Properties;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Windows;

namespace ChromExplorer.Modules.Settings.ViewModels
{
    // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
    [Export(typeof(PostProcessingViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PostProcessingViewViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        [Import] private IPostProcessingService _service;
        private ObservableCollection<IPostProcessingOperation> _operations;
        private IPostProcessingOperation _selectedMsOperation;
        private IPostProcessingOperation _selectedEicOperation;
        private ObservableCollection<IPostProcessingOperation> _msOperations;
        private ObservableCollection<IPostProcessingOperation> _eicOperations;

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        public PostProcessingViewViewModel()
        {
            ApplyCommand = new DelegateCommand<object>(OnApplyCommandExecute);
            ApplyMsCommand = new DelegateCommand<object>(OnApplyMsCommandExecute);
            ApplyEicCommand = new DelegateCommand<object>(OnApplyEicCommandExecute);
        }

        public DelegateCommand<object> ApplyCommand { get; private set; }
        public DelegateCommand<object> ApplyMsCommand { get; private set; }
        public DelegateCommand<object> ApplyEicCommand { get; private set; }

        public ObservableCollection<IPostProcessingOperation> MsOperations
        {
            get { return _msOperations; }
            private set
            {
                if (Equals(value, _msOperations)) return;
                _msOperations = value;
                OnPropertyChanged("MsOperations");
            }
        }

        public ObservableCollection<IPostProcessingOperation> EicOperations
        {
            get { return _eicOperations; }
            private set
            {
                if (Equals(value, _eicOperations)) return;
                _eicOperations = value;
                OnPropertyChanged("EicOperations");
            }
        }

        public IPostProcessingOperation SelectedMsOperation
        {
            get { return _selectedMsOperation; }
            set
            {
                if (Equals(value, _selectedMsOperation)) return;
                _selectedMsOperation = value;
                OnPropertyChanged("SelectedMsOperation");
            }
        }

        public IPostProcessingOperation SelectedEicOperation
        {
            get { return _selectedEicOperation; }
            set
            {
                if (Equals(value, _selectedEicOperation)) return;
                _selectedEicOperation = value;
                OnPropertyChanged("SelectedEicOperation");
            }
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            if (_service != null)
            {
                MsOperations = new ObservableCollection<IPostProcessingOperation>(_service.Operations.Where(o => o.Type == PostProcessingType.MS));
                EicOperations = new ObservableCollection<IPostProcessingOperation>(_service.Operations.Where(o => o.Type == PostProcessingType.EIC));

                SelectedMsOperation = MsOperations.FirstOrDefault();
                SelectedEicOperation = EicOperations.FirstOrDefault();
            }
        }

        private void OnApplyCommandExecute(object obj)
        {
            if (_service != null)
            {
                _service.SaveSettings();
                _service.RaisePostProcessingApplied();
            }
        }

        //private void OnApplyMsCommandExecute(object obj)
        //{
        //    if (_service != null)
        //    {
        //        _service.SaveMsSettings();
        //        _service.RaisePostProcessingApplied("MS");
        //    }
        //}

        //private void OnApplyEicCommandExecute(object obj)
        //{
        //    if (_service != null)
        //    {
        //        _service.SaveEicSettings();
        //        _service.RaisePostProcessingApplied("EIC");
        //    }
        //}

        private void OnApplyMsCommandExecute(object obj)
        {
            if (_service != null)
            {
                _service.SaveMsSettings();

                string parameter = "";

                if (SelectedMsOperation != null)
                {
                    var type = SelectedMsOperation.GetType();
                    string filterName = type.Name;

                    // GET CONFIG VIEW
                    var configViewProp = type.GetProperty("ConfigView");

                    if (configViewProp != null)
                    {
                        var configView = configViewProp.GetValue(SelectedMsOperation, null);

                        if (configView != null)
                        {
                            var dataContextProp = configView.GetType().GetProperty("DataContext");

                            if (dataContextProp != null)
                            {
                                var dataContext = dataContextProp.GetValue(configView, null);

                                if (dataContext != null)
                                {
                                    var settingsProps = dataContext.GetType().GetProperties();

                                    foreach (var p in settingsProps)
                                    {
                                        try
                                        {
                                            if (p.Name == "Title" ||
                                                p.Name == "IsActive")
                                                continue;

                                            var value = p.GetValue(dataContext, null);

                                            if (value == null)
                                                continue;

                                            parameter += p.Name + " : " + value + " , ";
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            }
                        }
                    }

                    InsertAuditLog(
                        "Filter applied on MS",
                        filterName + " applied on MS",
                        parameter);
                }

                _service.RaisePostProcessingApplied("MS");
            }
        }

        private void OnApplyEicCommandExecute(object obj)
        {
            if (_service != null)
            {
                _service.SaveEicSettings();

                string parameter = "";

                if (SelectedEicOperation != null)
                {
                    var type = SelectedEicOperation.GetType();
                    string filterName = type.Name.Replace("Eic", "");

                    var configViewProp = type.GetProperty("ConfigView");

                    if (configViewProp != null)
                    {
                        var configView = configViewProp.GetValue(SelectedEicOperation, null);

                        if (configView != null)
                        {
                            var dataContextProp = configView.GetType().GetProperty("DataContext");

                            if (dataContextProp != null)
                            {
                                var dataContext = dataContextProp.GetValue(configView, null);

                                if (dataContext != null)
                                {
                                    var settingsProps = dataContext.GetType().GetProperties();

                                    foreach (var p in settingsProps)
                                    {
                                        try
                                        {
                                            if (p.Name == "Title" ||
                                                p.Name == "IsActive")
                                                continue;

                                            var value = p.GetValue(dataContext, null);

                                            if (value == null)
                                                continue;

                                            parameter += p.Name + " : " + value + " , ";
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            }
                        }
                    }

                    InsertAuditLog(
                        "Filter applied on EIC",
                        filterName + " applied on EIC",
                        parameter);
                }

                _service.RaisePostProcessingApplied("EIC");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InsertAuditLog(string action, string description, string parameter)
        {
            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();

                var sql = @"INSERT INTO AuditLogM
                          (action, description, parameter, is_active, inserted_by, inserted_date)
                          VALUES
                          (@action, @desc, @param, 1, @uid, datetime('now','localtime'))";

                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@desc", description);
                    cmd.Parameters.AddWithValue("@param", parameter ?? string.Empty);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
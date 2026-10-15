using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure.Events;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Windows;

namespace ChromExplorer.Modules.Settings.ViewModels
{
    [Export(typeof(PeakIntegrationViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PeakIntegrationViewViewModel : INotifyPropertyChanged, IPartImportsSatisfiedNotification
    {
        [Import]
        private IEventAggregator _eventAggregator;

        private double _startTime;
        private double _endTime;

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        public PeakIntegrationViewViewModel()
        {
            ApplyCommand = new DelegateCommand<object>(OnApplyCommandExecute);
            RemoveCommand = new DelegateCommand<object>(OnRemoveCommandExecute);
        }

        public double StartTime
        {
            get { return _startTime; }
            set
            {
                if (value.Equals(_startTime)) return;
                _startTime = value;
                OnPropertyChanged("StartTime");
            }
        }

        public double EndTime
        {
            get { return _endTime; }
            set
            {
                if (value.Equals(_endTime)) return;
                _endTime = value;
                OnPropertyChanged("EndTime");
            }
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            _eventAggregator?.GetEvent<PeakIntegrationMethodLoadedEvent>().Subscribe(args =>
            {
                if (args == null) return;
                StartTime = args.StartTime;
                EndTime = args.EndTime;
            });
        }

        public DelegateCommand<object> ApplyCommand { get; private set; }
        public DelegateCommand<object> RemoveCommand { get; private set; }

        private void OnApplyCommandExecute(object obj)
        {
            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<PeakIntegrationEvent>().Publish(new PeakIntegrationArgs
                {
                    StartTime = StartTime,
                    EndTime = EndTime,
                    IsRemove = false
                });

                InsertLog("Peak Integration", "Peak Integration Applied", $"Start Time: {StartTime}, End Time: {EndTime}");
            }
        }

        private void OnRemoveCommandExecute(object obj)
        {
            if (_eventAggregator != null)
            {

                _eventAggregator.GetEvent<PeakIntegrationEvent>().Publish(new PeakIntegrationArgs
                {
                    IsRemove = true
                });

                InsertLog("Peak Integration", "Peak Integration Removed", "");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InsertLog(string action, string description, string parameter)
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

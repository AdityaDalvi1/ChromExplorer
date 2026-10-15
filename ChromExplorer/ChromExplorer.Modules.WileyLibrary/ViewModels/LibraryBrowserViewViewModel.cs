using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.WileyLibrary.Annotations;
using ChromExplorer.WileySearch;
using Microsoft.Practices.Prism.Commands;

namespace ChromExplorer.Modules.WileyLibrary.ViewModels
{
    [Export(typeof (LibraryBrowserViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class LibraryBrowserViewViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);



        private int _currentRecordNumber;
        private string _statusText;
        private readonly WileyDataProvider _provider;
        private Record _currentRecord;
        private int _imageSize = 256;
        private BitmapSource _structureImage;
        private string _spectrumText;


        public LibraryBrowserViewViewModel()
        {
            _provider = null;

            try
            {
                _provider = new WileyDataProvider();
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }

            MoveLeftCommand = new DelegateCommand<object>(OnMoveLeft, OnMoveLeftCanExecute);
            MoveRightCommand = new DelegateCommand<object>(OnMoveRight, OnMoveRightCanExecute);

            CurrentRecordNumber = 1;
        }

        public int CurrentRecordNumber
        {
            get { return _currentRecordNumber; }
            set
            {
                if (value == _currentRecordNumber) return;
                _currentRecordNumber = value;
                UpdateCommands();
                UpdateRecord();
                OnPropertyChanged("CurrentRecordNumber");
            }
        }

        public Record CurrentRecord
        {
            get { return _currentRecord; }
            set
            {
                if (Equals(value, _currentRecord)) return;
                _currentRecord = value;
                OnPropertyChanged("CurrentRecord");
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (value == _statusText) return;
                _statusText = value;
                OnPropertyChanged("StatusText");
            }
        }

        public int ImageSize
        {
            get { return _imageSize; }
            set
            {
                if (value == _imageSize) return;
                _imageSize = value;
                OnPropertyChanged("ImageSize");
            }
        }

        public string SpectrumText
        {
            get { return _spectrumText; }
            set
            {
                if (value == _spectrumText) return;
                _spectrumText = value;
                OnPropertyChanged("SpectrumText");
            }
        }


        public DelegateCommand<object> MoveLeftCommand { get; private set; }
        public DelegateCommand<object> MoveRightCommand { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapSource StructureImage
        {
            get { return _structureImage; }
            private set
            {
                if (Equals(value, _structureImage)) return;
                _structureImage = value;
                OnPropertyChanged("StructureImage");
            }
        }

        public void OnImportsSatisfied()
        {
        }

        private bool OnMoveRightCanExecute(object arg)
        {
            if (_provider == null)
                return false;

            return _currentRecordNumber < _provider.RecordCount - 1;
        }

        private bool OnMoveLeftCanExecute(object arg)
        {
            return _currentRecordNumber > 1;
        }

        private void OnMoveRight(object obj)
        {
            CurrentRecordNumber++;
        }

        private void OnMoveLeft(object obj)
        {
            CurrentRecordNumber--;
        }

        private void UpdateRecord()
        {
            StructureImage = null;

            if (_provider == null)
            {
                CurrentRecord = null;
            }
            else
                CurrentRecord = _provider.GetRecord(CurrentRecordNumber, _imageSize, _imageSize);

            if (CurrentRecord != null)
            {
                StructureImage = ImageUtils.LoadBitmap(CurrentRecord.Structure);
                SpectrumText = CurrentRecord.SpectrumString;
            }
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (_provider != null)
                StatusText = string.Format("{0} of {1}", _currentRecordNumber, _provider.RecordCount);
            else
            {
                StatusText = String.Empty;
            }
        }

        private void UpdateCommands()
        {
            MoveLeftCommand.RaiseCanExecuteChanged();
            MoveLeftCommand.RaiseCanExecuteChanged();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}
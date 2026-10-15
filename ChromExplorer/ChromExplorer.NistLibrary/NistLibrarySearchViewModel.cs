using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using ChromExplorer.DataModel.Properties;
using Microsoft.Practices.Prism.Commands;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ChromExplorer.NistLibrary
{
    [Export(typeof(NistLibrarySearchViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class NistLibrarySearchViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        private string _nistExecutablePath;
        private string _firstLocatorFile;
        private string _secondLocatorFile;
        private string _workingDir;

        public NistLibrarySearchViewModel()
        {
            ApplyCommand = new DelegateCommand<object>(OnApply);
            BrowseNistExecutableCommand = new DelegateCommand<object>(OnBrowseNistExec);
            BrowseFirstLocatorFileCommand = new DelegateCommand<object>(OnBrowseFirstLocatorFile);
            BrowseSecondLocatorFileCommand = new DelegateCommand<object>(OnBrowseSecondLocatorFile);
            BrowseWorkingDirCommand = new DelegateCommand<object>(OnBrowseWorkingDir);
        }

        private void OnBrowseSecondLocatorFile(object obj)
        {
            var dlg = new SaveFileDialog() { DefaultExt = ".fil", Filter = "Second Locator File|*.fil|All Files|*.*", FileName = "FILESPEC.FIL" };
            var result = dlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                SecondLocatorFile = dlg.FileName;

            }
        }

        private void OnBrowseFirstLocatorFile(object obj)
        {
            var dlg = new SaveFileDialog() { DefaultExt = ".msd", Filter = "First Locator File|*.msd|All Files|*.*", FileName = "AUTOIMP.MSD" };
            var result = dlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                FirstLocatorFile = dlg.FileName;

            }
        }

        private void OnBrowseWorkingDir(object obj)
        {
            var dlg = new FolderBrowserDialog();
            var result = dlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                NistExecutablePath = dlg.SelectedPath;

            }
        }

        private void OnApply(object obj)
        {
            NistLibrarySettings.Default.NistExecutable = NistExecutablePath;
            NistLibrarySettings.Default.FirstLocatorFile = FirstLocatorFile;
            NistLibrarySettings.Default.SecondLocatorFile = SecondLocatorFile;
            NistLibrarySettings.Default.WorkingDir = WorkingDir;

            NistLibrarySettings.Default.Save();
        }

        private void OnBrowseNistExec(object obj)
        {
            var dlg = new OpenFileDialog {DefaultExt = ".exe", Filter = "Executable file|*.exe|All Files|*.*"};

            var result = dlg.ShowDialog();

            if (result == true)
            {
                NistExecutablePath = dlg.FileName;

            }
        }
        
        public string NistExecutablePath
        {
            get { return _nistExecutablePath; }
            set
            {
                if (value == _nistExecutablePath) return;
                _nistExecutablePath = value;
                OnPropertyChanged(nameof(NistExecutablePath));
            }
        }

        public string FirstLocatorFile  
        {
            get { return _firstLocatorFile; }
            set
            {
                if (value == _firstLocatorFile) return;
                _firstLocatorFile = value;
                OnPropertyChanged(nameof(FirstLocatorFile));
            }
        }

        public string SecondLocatorFile
        {
            get { return _secondLocatorFile; }
            set
            {
                if (value == _secondLocatorFile) return;
                _secondLocatorFile = value;
                OnPropertyChanged(nameof(SecondLocatorFile));
            }
        }

        public string WorkingDir
        {
            get { return _workingDir; }
            set
            {
                if (value == _workingDir) return;
                _workingDir = value;
                OnPropertyChanged(nameof(WorkingDir));
            }
        }

        public DelegateCommand<object> ApplyCommand { get; private set; }
        public DelegateCommand<object> BrowseNistExecutableCommand { get; private set; }
        public DelegateCommand<object> BrowseFirstLocatorFileCommand { get; private set; }
        public DelegateCommand<object> BrowseSecondLocatorFileCommand { get; private set; }
        public DelegateCommand<object> BrowseWorkingDirCommand { get; private set; }

        #region Implementation of IPartImportsSatisfiedNotification

        public void OnImportsSatisfied()
        {
            NistExecutablePath = NistLibrarySettings.Default.NistExecutable;
            FirstLocatorFile = NistLibrarySettings.Default.FirstLocatorFile;
            SecondLocatorFile = NistLibrarySettings.Default.SecondLocatorFile;
            WorkingDir = NistLibrarySettings.Default.WorkingDir;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
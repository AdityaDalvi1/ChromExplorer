using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Licensing.Annotations;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Practices.Prism.Regions;
using QlmLicenseLib;
using QlmLicenseLib.QlmAspService;
using QRCoder;

namespace ChromExplorer.Modules.Licensing.ViewModels
{
    public class QrCodeNotification : Notification
    {
        public BitmapImage Image { get; set; }
    }

    [Export(typeof(LicensingViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class LicensingViewViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Uri DefaultUri = new Uri("HomeScreenView", UriKind.Relative);

        private const string RegistrationUri =
            "http://activate.apmaldi.com/QlmCustomerSite/qlmregistrationform.aspx?is_args=chrom";

        private const string PortalUri = "http://activate.apmaldi.com/QlmCustomerSite";

        readonly InteractionRequest<QrCodeNotification> _confirmationFromViewModelInteractionRequest;
        readonly InteractionRequest<Notification> _webServiceResponseNotification;


        private readonly IRegionManager _regionManager;
        private readonly ILicenseService _licenseService;
        private string _activationKey;
        private string _licenseStatus;
        private bool _isValid;
        private string _computerId;
        private string _computerKey;


        [ImportingConstructor]
        public LicensingViewViewModel(IRegionManager regionManager, ILicenseService licenseService)
        {
            _confirmationFromViewModelInteractionRequest = new InteractionRequest<QrCodeNotification>();
            _webServiceResponseNotification = new InteractionRequest<Notification>();

            _regionManager = regionManager;
            _licenseService = licenseService;

            QlmCloseCommand = new DelegateCommand<object>(OnQlmClose);
            QlmActivateCommand = new DelegateCommand<object>(OnQlmActivateCommand, OnQlmActivateCommandCanExecute);
            QlmDeactivateCommand = new DelegateCommand<object>(OnQlmDeactivate, OnQlmDeactivateCanExecute);
            RegisterCommand = new DelegateCommand<object>(OnRegister);
            RegisterOfflineCommand = new DelegateCommand<object>(OnRegisterOffline);
            GetOfflineActivationCodeCommand = new DelegateCommand<object>(OnGetOfflineActivationCode);
            QlmActivateOfflineCommand = new DelegateCommand<object>(OnActivateOffline, OnActivateOfflineCanExecute);
            QlmDeactivateOfflineCommand = new DelegateCommand<object>(OnDeactivateOffline, OnDeactivateOfflineCanExecute);

            InvalidateLicense();
            UpdateUI();
        }

        private bool OnDeactivateOfflineCanExecute(object arg)
        {
            return _licenseService.IsValid;
        }

        private bool OnActivateOfflineCanExecute(object arg)
        {
            return !_licenseService.IsValid
                && !string.IsNullOrEmpty(ActivationKey)
                && !string.IsNullOrEmpty(ComputerId)
                && !string.IsNullOrEmpty(ComputerKey);
        }

        private void UpdateUI()
        {
            QlmActivateCommand.RaiseCanExecuteChanged();
            QlmDeactivateCommand.RaiseCanExecuteChanged();
            QlmActivateOfflineCommand.RaiseCanExecuteChanged();
            QlmDeactivateOfflineCommand.RaiseCanExecuteChanged();
        }

        private bool OnQlmDeactivateCanExecute(object arg)
        {
            return _licenseService.IsValid && !string.IsNullOrEmpty(ActivationKey);
        }

        private bool OnQlmActivateCommandCanExecute(object arg)
        {
            return !_licenseService.IsValid && !string.IsNullOrEmpty(ActivationKey);
        }

        private void OnDeactivateOffline(object obj)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(PortalUri, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(64);

            _confirmationFromViewModelInteractionRequest.Raise(
                new QrCodeNotification { Title = "Customer Portal", Image = Convert(qrCodeImage) },
                dummy => { });

            _licenseService.DeactivateOffline();
        }

        private void OnActivateOffline(object obj)
        {
            if (string.IsNullOrEmpty(ActivationKey) || string.IsNullOrEmpty(ComputerKey))
                return;

            _licenseService.ActivateOffline(ActivationKey, ComputerId, ComputerKey);

            if (!string.IsNullOrEmpty(_licenseService.WebServiceMessage))
            {
                RaiseWebServiceResponseNotification(_licenseService.WebServiceMessage);
            }

            InvalidateLicense();

            RaiseWebServiceResponseNotification(_licenseService.IsValid
                ? "Your license has been activated"
                : "Your license failed to activate");
        }

        private void OnGetOfflineActivationCode(object obj)
        {
            if (!string.IsNullOrEmpty(ActivationKey))
            {
                var url = _licenseService.CreateActivationUrl(ActivationKey);

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(64);

                _confirmationFromViewModelInteractionRequest.Raise(
                    new QrCodeNotification { Title = "Computer Key QR Code", Image = Convert(qrCodeImage) },
                    dummy => { });
            }
        }

        private void OnRegisterOffline(object obj)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(RegistrationUri, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(64);

            _confirmationFromViewModelInteractionRequest.Raise(
                new QrCodeNotification { Title = "Registration QR Code", Image = Convert(qrCodeImage) },
                dummy => { });
        }

        private BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void OnRegister(object obj)
        {
            System.Diagnostics.Process.Start(RegistrationUri);
        }

        private void InvalidateLicense()
        {
            IsValid = _licenseService.Validate();
            LicenseStatus = _licenseService.LicenseStatus;
            ActivationKey = _licenseService.ActivationKey;
            ComputerId = _licenseService.ComputerId;
            ComputerKey = _licenseService.ComputerKey;

            //var dk = _licenseService.DeactivationCode;
        }

        private void OnQlmDeactivate(object obj)
        {
            _licenseService.Deactivate();

            if (!string.IsNullOrEmpty(_licenseService.WebServiceMessage))
            {
                RaiseWebServiceResponseNotification(_licenseService.WebServiceMessage);
            }

            InvalidateLicense();
        }

        public void OnQlmClose(object arg)
        {
            _regionManager.RequestNavigate(RegionNames.MainContentArea, DefaultUri);
        }

        public void OnQlmActivateCommand(object arg)
        {
            if (!string.IsNullOrEmpty(ActivationKey))
            {
                _licenseService.ActivateOnline(ActivationKey);
                InvalidateLicense();

                if (!string.IsNullOrEmpty(_licenseService.WebServiceMessage))
                {
                    RaiseWebServiceResponseNotification(_licenseService.WebServiceMessage);
                }
            }
        }

        public DelegateCommand<object> QlmCloseCommand { get; private set; }
        public DelegateCommand<object> RegisterCommand { get; private set; }
        public DelegateCommand<object> QlmActivateCommand { get; private set; }
        public DelegateCommand<object> QlmActivateOfflineCommand { get; private set; }

        public DelegateCommand<object> QlmDeactivateCommand { get; private set; }
        public DelegateCommand<object> QlmDeactivateOfflineCommand { get; private set; }
        public DelegateCommand<object> RegisterOfflineCommand { get; private set; }
        public DelegateCommand<object> GetOfflineActivationCodeCommand { get; private set; }


        public IInteractionRequest ConfirmationFromViewModelInteractionRequest
        {
            get { return _confirmationFromViewModelInteractionRequest; }
        }

        public IInteractionRequest WebServiceResponseNotification
        {
            get { return _webServiceResponseNotification; }
        }
        

        public string ActivationKey
        {
            get => _activationKey;
            set
            {
                if (value == _activationKey) return;
                _activationKey = value;
                UpdateUI();
                OnPropertyChanged();
            }
        }

        public string LicenseStatus
        {
            get => _licenseStatus;
            set
            {
                if (value == _licenseStatus) return;
                _licenseStatus = value;
                OnPropertyChanged();
            }
        }

        public string ComputerId
        {
            get => _computerId;
            set
            {
                if (value == _computerId) return;
                _computerId = value;
                UpdateUI();
                OnPropertyChanged();
            }
        }

        public string ComputerKey
        {
            get => _computerKey;
            set
            {
                if (value == _computerKey) return;
                _computerKey = value;
                UpdateUI();
                OnPropertyChanged();
            }
        }

        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (value == _isValid) return;
                _isValid = value;
                OnPropertyChanged();
            }
        }

        private void RaiseWebServiceResponseNotification(string message)
        {
            _webServiceResponseNotification.Raise(
                new Notification { Content = message, Title = "Activation message" },
                dummy => { _regionManager.RequestNavigate(RegionNames.MainContentArea, DefaultUri); });
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
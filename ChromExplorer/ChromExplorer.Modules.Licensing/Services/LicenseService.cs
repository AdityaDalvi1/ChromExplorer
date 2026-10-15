using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Licensing.Annotations;
using QlmLicenseLib;

namespace ChromExplorer.Modules.Licensing.Services
{
    [Export(typeof(ILicenseService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LicenseService : ILicenseService, INotifyPropertyChanged
    {
        private const string SettingsFile = "ChromExplorer 2.0.lw.xml";
        private const string WebServiceUrl = "http://activate.apmaldi.com/QlmLicenseServer/qlmservice.asmx";
        private const string CodeUrlFormat = "http://activate.apmaldi.com/QlmCustomerSite/QlmAutoActivation.aspx?is_avkey={0}&is_pcid={1}&is_productid=7&is_majorversion=2&is_minorversion=0&is_deactcode={2}&is_pcname={3}";
        private readonly QLM.LicenseValidator _lv;
        private string _licenseStatus;
        private bool _isValid;
        private string _activationKey;
        private string _webServiceMessage;
        private string _computerId;
        private string _computerKey;

        [ImportingConstructor]
        public LicenseService()
        {
            _lv = new QLM.LicenseValidator(SettingsFile);
            ComputerId = Environment.MachineName;

            Validate();

        }

        #region Implementation of ILicenseService

        public string DeactivationCode
        {
            get
            {
                ELicenseBinding licenseBinding = ELicenseBinding.ComputerName;
                string code = _lv.QlmLicenseObject.CreateDeactivationCode(ActivationKey, ComputerId, licenseBinding);
                string key = _lv.QlmLicenseObject.CommunicationEncryptionKey;
                return code;
            }

        }


        public bool IsValid
        {
            get => _isValid;
            private set
            {
                if (value == _isValid) return;
                _isValid = value;
                OnPropertyChanged();
            }
        }

        public string LicenseStatus
        {
            get => _licenseStatus;
            private set
            {
                if (value == _licenseStatus) return;
                _licenseStatus = value;
                OnPropertyChanged();
            }
        }

        public string ActivationKey
        {
            get => _activationKey;
            private set
            {
                if (value == _activationKey) return;
                _activationKey = value;
                OnPropertyChanged();
            }
        }

        public string WebServiceMessage
        {
            get => _webServiceMessage;
            private set
            {
                if (value == _webServiceMessage) return;
                _webServiceMessage = value;
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
                OnPropertyChanged();
            }
        }

        public bool Validate()
        {
            bool needsActivation = false;
            string errorMsg = string.Empty;

            ELicenseBinding licenseBinding = ELicenseBinding.ComputerName;

            IsValid = _lv.ValidateLicenseAtStartup(licenseBinding, ref needsActivation, ref errorMsg);

            LicenseStatus = !IsValid ? errorMsg : "Your license is activated";

            if (IsValid)
            {
                ActivationKey = _lv.ActivationKey;
                ComputerKey = _lv.ComputerKey;
            }

            return IsValid;
        }

        public bool ActivateOnline(string key)
        {
            WebServiceMessage = string.Empty;
            string response = string.Empty;

            _lv.QlmLicenseObject.ActivateLicense(WebServiceUrl, key, Environment.MachineName
                , Environment.MachineName, "12.0.00", string.Empty, out response);

            

            ILicenseInfo licenseInfo = new LicenseInfo();
            string message = string.Empty;
            if (_lv.QlmLicenseObject.ParseResults(response, ref licenseInfo, ref message))
            {
                // Store the activation key and computer key on the local system
                _lv.QlmLicenseObject.StoreKeys(key, licenseInfo.ComputerKey);
                WebServiceMessage = message;
                return true;
            }

            WebServiceMessage = message;
            return false;
        }
        //private const string CodeUrlFormat = "is_avkey={0}&is_pcid={1}&is_productid=7&is_majorversion=2&is_minorversion=0&is_deactcode={2}&is_pcname={3}";
        public bool ActivateOffline(string key, string computerId, string computerBoundKey)
        {
            WebServiceMessage = string.Empty;
            string message = string.Empty;

            if (_lv.QlmLicenseObject.ValidateMachineLicenseEx(key, computerBoundKey, computerId, ref message))
            {
                _lv.QlmLicenseObject.StoreKeys(key, computerBoundKey);
                WebServiceMessage = message;
                return true;
            }
            else
            {
                WebServiceMessage = message;
            }
            if (ValidateActivationKey(key) && ValidateComputerKey(computerBoundKey))
            {
                _lv.QlmLicenseObject.StoreKeys(key, computerBoundKey);
                return true;
            }

            return false;
        }

        public bool ValidateActivationKey(string key)
        {
            string machineId = string.Empty;
            bool needActivation = false;
            string message = string.Empty;
            return _lv.ValidateLicense(key, string.Empty, ref machineId, ELicenseBinding.ComputerName,
                ref needActivation, ref message);
        }

        public bool ValidateComputerKey(string key)
        {
            string machineId = string.Empty;
            bool needActivation = false;
            string message = string.Empty;
            return _lv.ValidateLicense(string.Empty, key, ref machineId, ELicenseBinding.ComputerName,
                ref needActivation, ref message);
        }


        public string CreateActivationUrl(string key)
        {
            ELicenseBinding licenseBinding = ELicenseBinding.ComputerName;
            var code = _lv.QlmLicenseObject.CreateDeactivationCode(key, Environment.MachineName, licenseBinding);

            return string.Format(CodeUrlFormat, key, Environment.MachineName, code, Environment.MachineName);
        }

        public void DeactivateOffline()
        {
            _lv.DeleteKeys();
        }

        public void Deactivate()
        {
            string response = string.Empty;

            _lv.QlmLicenseObject.ReleaseLicense(WebServiceUrl, _lv.ActivationKey, Environment.MachineName, out response);

            ILicenseInfo licenseInfo = new LicenseInfo();

            string returnMessage = string.Empty;

            if (_lv.QlmLicenseObject.ParseResults(response, ref licenseInfo, ref returnMessage))
            {
                WebServiceMessage = returnMessage;
                _lv.DeleteKeys();
                _lv.UnpublishAnalyticsFromServer();
            }
            else
            {
                WebServiceMessage = returnMessage;
            }

            
        }



        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
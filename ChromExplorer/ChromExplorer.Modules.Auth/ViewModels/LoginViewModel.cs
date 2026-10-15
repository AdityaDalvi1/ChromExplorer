using ChromExplorer.Modules.Auth.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChromExplorer.Modules.Auth.Models.User;

namespace ChromExplorer.Modules.Auth.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;

        private LoginModel _model;

        public LoginViewModel()
        {
            _authService = new AuthService();
            _model = new LoginModel();

        }

        public string ErrorMessage
        {
            get { return _model.ErrorMessage; }
            set
            {
                if (_model.ErrorMessage != value)
                {
                    _model.ErrorMessage = value;
                    OnPropertyChanged("ErrorMessage");
                }
            }
        }


        public bool PerformLogin(string passwordFromView, string username)
        {

            _model.Password = passwordFromView;
            _model.Username = username;

            //return true;


            if (string.IsNullOrWhiteSpace(_model.Username) || string.IsNullOrWhiteSpace(_model.Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return false;
            }


            try
            {
                string encryptedPassword = CryptoHelper.Encrypt(_model.Password);
                _model.Password = encryptedPassword;

                bool isValid = _authService.AuthenticateUser(_model, out string authError);

                if (isValid)
                {
                    ErrorMessage = "";
                    return true;
                }
                else
                {
                    ErrorMessage = authError;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

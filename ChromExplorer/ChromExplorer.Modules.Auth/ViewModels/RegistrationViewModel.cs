using ChromExplorer.Modules.Auth.Models;
using ChromExplorer.Modules.Auth.Services;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using static ChromExplorer.Modules.Auth.Models.User;

namespace ChromExplorer.Modules.Auth.ViewModels
{
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private RegistrationModel _model;


        private int _selectedRoleId;

        public RegistrationViewModel()
        {
            _authService = new AuthService();
            _model = new RegistrationModel();

        }

        public string Username
        {
            get { return _model.Username; }
            set
            {
                if (_model.Username != value)
                {
                    _model.Username = value;
                    OnPropertyChanged("Username");
                }
            }
        }

        public string FullName
        {
            get { return _model.FullName; }
            set
            {
                if (_model.FullName != value)
                {
                    _model.FullName = value;
                    OnPropertyChanged("FullName");
                }
            }
        }

        public string Password
        {
            get { return _model.Password; }
            set
            {
                if (_model.Password != value)
                {
                    _model.Password = value;
                    OnPropertyChanged("Password");
                }
            }
        }

        public string Email
        {
            get { return _model.Email; }
            set
            {
                if (_model.Email != value)
                {
                    _model.Email = value;
                    OnPropertyChanged("Email");
                }
            }
        }

        public string ErrorMessage
        {
            get { return _model?.ErrorMessage ?? string.Empty; }
            set
            {
                _model.ErrorMessage = value ?? string.Empty;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }


        //public int SelectedRoleId
        //{
        //    get { return _selectedRoleId; }
        //    set
        //    {
        //        if (_selectedRoleId != value)
        //        {
        //            _selectedRoleId = value;
        //            _model.RoleId = value;
        //            OnPropertyChanged("SelectedRoleId");
        //        }
        //    }
        //}
        private int? _selectedRole;
        public int? SelectedRoleId
        {
            get { return _selectedRole; }
            set
            {
                if (_selectedRoleId != value)
                {
                    _selectedRole = value;
                    _model.RoleId = value;   // if model also int?
                    OnPropertyChanged(nameof(SelectedRoleId));
                }
            }
        }


        public bool PerformRegistration(string username, string fullName, string Password, string email)
        {
            ErrorMessage = string.Empty;
            _model.Username = username;
            _model.FullName = fullName;
            _model.Password = Password;
            _model.Email = email;
            _model.RoleId = _selectedRole;

            if (string.IsNullOrWhiteSpace(_model.Username))
            {
                ErrorMessage = "Please enter a username.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_model.FullName))
            {
                ErrorMessage = "Please enter a full name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_model.Password))
            {
                ErrorMessage = "Please enter a Password.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_model.Email))
            {
                ErrorMessage = "Please enter an email address.";
                return false;
            }

            if (_model.RoleId <= 0)
            {
                ErrorMessage = "Please select a role.";
                return false;
            }

            if (!IsValidEmail(_model.Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return false;
            }


            if (!IsValidPassword(_model.Password))
            {
                ErrorMessage = "Password must be at least 10 characters, start with a capital letter, and include a lowercase letter, a number, and a special character.";
                return false;
            }

            if (string.Equals(_model.Username, _model.Password, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Password cannot be the same as the username.";
                return false;
            }

            try
            {
                int adminId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                string result = _authService.CreateNewUser(
                    _model.Username,
                    _model.FullName,
                    _model.Email,
                    _model.Password,
                    (int)_model.RoleId,
                    adminId
                );

                if (result == "Success")
                {
                    ErrorMessage = "";
                    return true;
                }
                else
                {
                    ErrorMessage = result;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
                return false;
            }
        }

        public bool PerformRegistration1()
        {


            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Please enter a username.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Please enter a full name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter a Password.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please enter an email address.";
                return false;
            }

            if (_model.RoleId <= 0)
            {
                ErrorMessage = "Please select a role.";
                return false;
            }

            if (!IsValidEmail(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return false;
            }

            if (!IsValidPassword(Password))
            {
                ErrorMessage = "Password must be at least 6 characters long and include one uppercase letter, one lowercase letter, one number, and one special character.";
                return false;
            }


            try
            {
                int adminId = 1;

                string result = _authService.CreateNewUser(
                    _model.Username,
                    _model.FullName,
                    _model.Email,
                    _model.Password,
                    (int)_model.RoleId,
                    adminId
                );

                if (result == "Success")
                {
                    ErrorMessage = "";
                    return true;
                }
                else
                {
                    ErrorMessage = result;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
                return false;
            }
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }


        private bool IsValidPassword(string password)
        {
            try
            {
                // Must start with capital letter, min 10 chars, contain lowercase, digit, special char
                var regex = new Regex(@"^[A-Z](?=.*[a-z])(?=.*\d)(?=.*[\W_]).{9,}$");
                return regex.IsMatch(password);
            }
            catch
            {
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
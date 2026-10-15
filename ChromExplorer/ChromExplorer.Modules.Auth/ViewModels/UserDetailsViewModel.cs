using ChromExplorer.Modules.Auth.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChromExplorer.Modules.Auth.Models.User;

namespace ChromExplorer.Modules.Auth.ViewModels
{
    public class UserDetailsViewModel
    {

        private readonly AuthService _authService;
        private RegistrationModel _model;



        public UserDetailsViewModel()
        {
            _authService = new AuthService();
            _model = new RegistrationModel();

        }


        public List<UserAccount> GetUserDetails()
        {
            return _authService.GetUserList();
        }

        public UserAccount GetUserById(int userId)
        {
            return _authService.GetUserById(userId);
        }

        public bool DeleteUser(int userId, string fullName, string reason)
        {
            return _authService.DeleteUser(userId, fullName, reason);
        }

        public bool ActivateUser(int userId, string fullName, string reason)
        {
            return _authService.ActivateUser(userId, fullName, reason);
        }

        public string UpdateUser(int userId, string username, string fullName, string email, int roleId, string reason)
        {
            return _authService.UpdateUser(userId, username, fullName, email, roleId, reason);
        }

        public void UnlockUser(string username, string reason)
        {
            _authService.UnlockAccount(username, reason);
        }
    }
}
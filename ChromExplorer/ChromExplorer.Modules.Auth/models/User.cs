using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

namespace ChromExplorer.Modules.Auth.Models

{

    public class User

    {
        public class LoginModel
        {
            public string Username { get; set; }

            public string Password { get; set; }

            public string EncryptedPassword { get; set; }

            public string ErrorMessage { get; set; }

        }

        public class RegistrationModel
        {
            public string Username { get; set; }

            public string FullName { get; set; }

            //     public string Phone { get; set; }

            public string Password { get; set; }

            public string Email { get; set; }

            public int? RoleId { get; set; }

            public string ErrorMessage { get; set; }

        }
        public class UserAccount
        {
            public int SrNo { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string RoleName { get; set; }
            public int RoleId { get; set; }
            public bool IsActive { get; set; }
            public bool IsLocked { get; set; }
            public DateTime? LockedUntil { get; set; }
        }
        public class ReportDO
        {
            public int SrNo { get; set; }
            public int AuditLogId { get; set; }
            public string Username { get; set; }
            public string Action { get; set; }
            public string Description { get; set; }
            public string FileName { get; set; }
            public string Role { get; set; }
            public string InsertedDate { get; set; }
            public string AuditorRemark { get; set; }

            public string Parameters { get; set; }
        }

        public class MenuModel
        {
            public string MenuName { get; set; }
            public string ViewName { get; set; }
        }
    }

}


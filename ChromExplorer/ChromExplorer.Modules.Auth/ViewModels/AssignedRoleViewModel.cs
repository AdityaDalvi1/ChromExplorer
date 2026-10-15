using ChromExplorer.Modules.Auth.models;
using ChromExplorer.Modules.Auth.Models;
using ChromExplorer.Modules.Auth.services;
using ChromExplorer.Modules.Auth.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChromExplorer.Modules.Auth.Models.User;

namespace ChromExplorer.Modules.Auth.ViewModels
{
    public class AssignedRoleViewModel
    {
        private readonly AssignRoleRightsData _assignRoleRightsData;
        private AssignedRoleViewModel _model;
        public ObservableCollection<DropDownData> Users { get; set; }
        public ObservableCollection<DropDownData> Roles { get; set; }

        public string SelectedUserId { get; set; }
        public string SelectedRoleId { get; set; }
        public AssignedRoleViewModel()
        {
            _assignRoleRightsData = new AssignRoleRightsData();
            //_model = new AssignedRoleViewModel();

        }
        public List<AssignRoleData> GetAssignedRoleDetails()
        {
            List<AssignRoleData> lisdata = new List<AssignRoleData>();

            lisdata = _assignRoleRightsData.GetAssignedRoleList();

            return lisdata;
        }

        public List<DropDownData> BindUsername()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                listdata = _assignRoleRightsData.GetUsername();
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        public List<DropDownData> BindRoles()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                listdata = _assignRoleRightsData.GetRoles();
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        public List<AssignRoleData> SaveAssignRole(AssignRoleData assignRolesData, string reason = "")
        {
            List<AssignRoleData> listdata = new List<AssignRoleData>();
            try
            {
                listdata = _assignRoleRightsData.SaveAssignRole(assignRolesData, reason);
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        //public bool DeleteAssignRole(AssignRoleData row)
        //{
        //    List<AssignRoleData> listdata = new List<AssignRoleData>();
        //    try
        //    {
        //        listdata = _assignRoleRightsData.DeleteAssignRole(row);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return listdata;
        //}

        public bool DeleteAssignRole(AssignRoleData row, string reason = "")
        {
            try
            {
                var result = _assignRoleRightsData.DeleteAssignRole(row, reason);

                if (result != null)
                {
                    return true; // deleted
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public List<AssignRoleData> GetAssignedRoleDetailsById(string id)
        {
            List<AssignRoleData> listdata = new List<AssignRoleData>();
            try
            {
                listdata = _assignRoleRightsData.GetAssignedRoleDetailsById(id);
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        public bool ActivateAssignRole(AssignRoleData row, string reason = "")
        {
            try
            {
                return _assignRoleRightsData.ActivateAssignRole(row, reason);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

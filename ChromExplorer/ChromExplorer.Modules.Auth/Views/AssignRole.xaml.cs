using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Auth.models;
using ChromExplorer.Modules.Auth.Models;
using ChromExplorer.Modules.Auth.ViewModels;
using Microsoft.Practices.Prism.Regions;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChromExplorer.Modules.Auth.Views
{
    /// <summary>
    /// Interaction logic for AssignRole.xaml
    /// </summary>
    /// 
    [Export("AssignRole", typeof(AssignRole))]

    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AssignRole : UserControl, INavigationAware
    {
        private readonly AssignedRoleViewModel _assignroleDetails;
        private bool isEditMode = false;
        [Import]
        public IRegionManager RegionManager { get; set; }
        public AssignRole()
        {
            InitializeComponent();
            //LoadData();
            _assignroleDetails = new AssignedRoleViewModel();

            this.DataContext = _assignroleDetails;


            //BindUserName();   
            //BindRoles();
        }
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // no need anything here for now
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            string id = navigationContext.Parameters["id"]?.ToString();
            string mode = navigationContext.Parameters["mode"]?.ToString();
            string reason = navigationContext.Parameters["reason"]?.ToString();

            if (mode == "edit" && !string.IsNullOrEmpty(id))
            {
                isEditMode = true; // 🔥 important
            }

            try
            {
                _editReason = !string.IsNullOrEmpty(reason) ? Uri.UnescapeDataString(reason) : "";
            }
            catch (Exception)
            {
                _editReason = reason ?? "";
            }

            BindUserName();
            BindRoles();

            if (isEditMode)
            {
                LoadUserRoleData(id);
            }
        }

        private string _editId = null; // 🔥 store id globally
        private string _editReason = ""; // 🔥 reason for edit, passed from ViewAssignRole
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string pageName = typeof(ViewAssignRights).FullName;
            RegionManager.RequestNavigate(RegionNames.OverContentRegion, new Uri(pageName, UriKind.Relative));
        }
        private void LoadUserRoleData(string id)
        {
            try
            {
                List<AssignRoleData> listdata = _assignroleDetails.GetAssignedRoleDetailsById(id);


                if (listdata != null)
                {
                    // 🔥 store id for update
                    _editId = listdata[0].userrole_id;

                    cmbUser.SelectedValue = listdata[0].UserName;
                    cmbRole.SelectedValue = listdata[0].RoleName;




                    // 🔥 change button names
                    btnSubmit.Content = "Update";
                    btnCancel.Content = "Reset";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssignRoleData assignRolesData = new AssignRoleData();

                // 🔥 if edit mode → pass id
                if (!string.IsNullOrEmpty(_editId))
                {
                    assignRolesData.userrole_id = _editId;
                }

                if (cmbUser.SelectedItem != null)
                {
                    var selectedUser = (DropDownData)cmbUser.SelectedItem;
                    assignRolesData.UserName = selectedUser.Id;
                }
                else
                {
                    MessageBox.Show("Please select user");
                    return;
                }

                if (cmbRole.SelectedItem != null)
                {
                    var selectedRole = (DropDownData)cmbRole.SelectedItem;
                    assignRolesData.RoleName = selectedRole.Id;
                }
                else
                {
                    MessageBox.Show("Please select role");
                    return;
                }

                List<AssignRoleData> listdata = _assignroleDetails.SaveAssignRole(assignRolesData, _editReason);

                if (listdata != null && listdata.Count > 0)
                {
                    var result = listdata[0];

                    if (result.Status == "Inserted")
                    {
                        MessageBox.Show("Role assigned successfully");
                    }
                    else if (result.Status == "Updated")
                    {
                        MessageBox.Show("Role updated successfully");
                    }
                    isEditMode = false;
        
                    string pageName = typeof(ViewAssignRole).FullName;
                    RegionManager.RequestNavigate(RegionNames.OverContentRegion, new Uri(pageName, UriKind.Relative));
                }

                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //private void SubmitButton_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        AssignRoleData assignRolesData = new AssignRoleData();

        //        if (cmbUser.SelectedItem != null)
        //        {
        //            var selectedUser = (DropDownData)cmbUser.SelectedItem;
        //            assignRolesData.UserName = selectedUser.Id;    
        //        }
        //        else
        //        {
        //            MessageBox.Show("Please select user");
        //            return;
        //        }

        //        if (cmbRole.SelectedItem != null)
        //        {
        //            var selectedRole = (DropDownData)cmbRole.SelectedItem;
        //            assignRolesData.RoleName = selectedRole.Id;
        //        }
        //        else
        //        {
        //            MessageBox.Show("Please select role");
        //            return;
        //        }

        //        if (assignRolesData.UserName == "0" || assignRolesData.RoleName == "0")
        //        {
        //            MessageBox.Show("Please select valid user and role");
        //            return;
        //        }

        //       List<AssignRoleData> listdata = _assignroleDetails.SaveAssignRole(assignRolesData);

        //        if (listdata != null && listdata.Count > 0)
        //        {
        //            var result = listdata[0];

        //            if (result.Status == "Inserted")
        //            {
        //                MessageBox.Show("Role assigned successfully");
        //                string pageName = typeof(ViewAssignRole).FullName;

        //                var uri = new Uri(pageName, UriKind.Relative);

        //                RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
        //            }
        //            else if (result.Status == "Updated")
        //            {
        //                MessageBox.Show("Role already exists, updated");
        //                string pageName = typeof(ViewAssignRole).FullName;

        //                var uri = new Uri(pageName, UriKind.Relative);

        //                RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
        //            }
        //            else
        //            {
        //                MessageBox.Show("Operation failed");
        //            }
        //        }
        //        Clear();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        private void Clear()
        {
            try
            {
                cmbRole.SelectedIndex = 0;
                cmbUser.SelectedIndex = 0;
            }
            catch (Exception ex)
            {

            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pageName = typeof(ViewAssignRole).FullName;

                var uri = new Uri(pageName, UriKind.Relative);

                RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
            }
            catch (Exception ex)
            {

            }
        }
        //private void BindUserName()
        //{
        //    try
        //    {
        //        var listdata = _assignroleDetails.BindUsername();

        //        if (listdata != null && listdata.Count > 0)
        //        {
        //            _assignroleDetails.Users = new ObservableCollection<User>(
        //                listdata.Select(x => new User
        //                {
        //                    UserId = x.Id,
        //                    Username = x.Text
        //                }).ToList()
        //            );
        //        }
        //        else
        //        {
        //            _assignroleDetails.Users = new ObservableCollection<User>();
        //            MessageBox.Show("No users found");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        private void BindRoles()
        {
            try
            {
                List<DropDownData> listdata = new List<DropDownData>();
                listdata = _assignroleDetails.BindRoles();

                if (listdata != null && listdata.Count > 0)
                {
                    listdata.Insert(0, new DropDownData
                    {
                        Id = "0",
                        Text = "--Select Role--"
                    });

                    cmbRole.ItemsSource = listdata;
                    cmbRole.DisplayMemberPath = "Text";
                    cmbRole.SelectedValuePath = "Id";

                    if (!isEditMode)
                    {
                        cmbRole.SelectedIndex = 0;
                    }
                }
                else
                {
                    cmbRole.ItemsSource = null;
                    MessageBox.Show("No roles found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void BindUserName()
        {
            try
            {
                List<DropDownData> listdata = new List<DropDownData>();
                listdata = _assignroleDetails.BindUsername();

                if (listdata != null && listdata.Count > 0)
                {
                    listdata.Insert(0, new DropDownData
                    {
                        Id = "0",
                        Text = "--Select Username--"
                    });

                    cmbUser.ItemsSource = listdata;
                    cmbUser.DisplayMemberPath = "Text";
                    cmbUser.SelectedValuePath = "Id";

                    if (!isEditMode)
                    {
                        cmbUser.SelectedIndex = 0;
                    }
                }
                else
                {
                    cmbUser.ItemsSource = null;
                    MessageBox.Show("No users found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Auth.models;
using ChromExplorer.Modules.Auth.services;
using ChromExplorer.Modules.Auth.ViewModels;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace ChromExplorer.Modules.Auth.Views
{
    [Export("UserRegistrationView", typeof(UserRegistrationView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class UserRegistrationView : UserControl, IPartImportsSatisfiedNotification, INavigationAware
    {
        private readonly RegistrationViewModel _viewModel;
        private readonly UserDetailsViewModel _userDetailsViewModel;
        private readonly AssignRoleRightsData _assignRoleRightsData;
        private bool _isEditMode = false;
        private int _editUserId = 0;
        private string _editReason = "";

        public UserRegistrationView()
        {
            InitializeComponent();
            _viewModel = new RegistrationViewModel();
            _userDetailsViewModel = new UserDetailsViewModel();
            _assignRoleRightsData = new AssignRoleRightsData();
            this.DataContext = _viewModel;
            BindRoles();
        }

        private void BindRoles()
        {
            try
            {
                cmbRole.Items.Clear();

                List<DropDownData> roles = _assignRoleRightsData.GetRoles();

                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        cmbRole.Items.Add(new ComboBoxItem
                        {
                            Content = role.Text,
                            Tag = Convert.ToInt32(role.Id)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [Import]
        public IRegionManager RegionManager { get; set; }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditMode)
            {
                // Edit mode — update user
                if (string.IsNullOrWhiteSpace(txtFullName.Text)) { ShowValidationError("Please enter a full name."); return; }
                if (string.IsNullOrWhiteSpace(txtEmail.Text)) { ShowValidationError("Please enter an email."); return; }
                if (_viewModel.SelectedRoleId == null) { ShowValidationError("Please select a role."); return; }

                string result = _userDetailsViewModel.UpdateUser(
                    _editUserId,
                    txtUsername.Text,
                    txtFullName.Text,
                    txtEmail.Text,
                    (int)_viewModel.SelectedRoleId,
                    _editReason);

                if (result == "Success")
                {
                    MessageBox.Show("User updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    CloseView();
                }
                else
                {
                    ShowValidationError(result);
                }
            }
            else
            {
                bool success = _viewModel.PerformRegistration(
                    txtUsername.Text,
                    txtFullName.Text,
                    txtPassword.Text,
                    txtEmail.Text);

                if (success)
                {
                    MessageBox.Show("User registered successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    CloseView();
                }
                else
                {
                    ShowValidationError(_viewModel.ErrorMessage);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)

        {

            ClearForm();

            CloseView();

        }

        private void ShowValidationError(string message)

        {

            var msg = message ?? string.Empty;

            lblMsg.Text = msg;

            lblMsg.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));

            if (string.IsNullOrWhiteSpace(msg))

            {

                ErrorHost.Visibility = Visibility.Collapsed;

            }

            else

            {

                ErrorHost.Visibility = Visibility.Visible;

                ErrorHost.BringIntoView();

            }

        }



        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            if (_isEditMode)
                CloseView();
        }

        public void ClearForm()

        {

            txtUsername.Text = string.Empty;

            txtFullName.Text = string.Empty;

            txtEmail.Text = string.Empty;

            txtPassword.Text = string.Empty;

            _viewModel.SelectedRoleId = null;

            _viewModel.ErrorMessage = string.Empty;

            ShowValidationError(string.Empty);

        }

        private void CloseView()

        {

            if (RegionManager == null)

            {

                MessageBox.Show("RegionManager is not available. Ensure MEF has injected it.", "Error");

                return;

            }

            else

            {


                RegionManager.RequestNavigate(RegionNames.OverContentRegion, "UserDetails");

            }

        }


        public void OnImportsSatisfied()
        {
        }

        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void txtFullName_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z ]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            string mode = navigationContext.Parameters["mode"]?.ToString();
            string userIdStr = navigationContext.Parameters["userId"]?.ToString();
            _editReason = Uri.UnescapeDataString(navigationContext.Parameters["reason"]?.ToString() ?? "");

            if (mode == "edit" && int.TryParse(userIdStr, out int uid))
            {
                _isEditMode = true;
                _editUserId = uid;
                txtTitle.Text = "Edit User";
                btnSubmit.Content = "UPDATE";
                btnCancel.Content = "CANCEL";

                var user = _userDetailsViewModel.GetUserById(uid);
                lblPassword.Visibility = Visibility.Collapsed;
                borderPassword.Visibility = Visibility.Collapsed;

                if (user != null)
                {
                    // Set via ViewModel so bindings stay in sync
                    _viewModel.Username = user.UserName;
                    _viewModel.FullName = user.FullName;
                    _viewModel.Email = user.Email;
                    _viewModel.Password = "";
                    _viewModel.SelectedRoleId = user.RoleId;
                }
            }
            else
            {
                _isEditMode = false;
                _editUserId = 0;
                txtTitle.Text = "User Registration";
                btnSubmit.Content = "SUBMIT";
                btnCancel.Content = "REST FORM";
                lblPassword.Visibility = Visibility.Visible;
                borderPassword.Visibility = Visibility.Visible;
                ClearForm();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => false;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
    }
}


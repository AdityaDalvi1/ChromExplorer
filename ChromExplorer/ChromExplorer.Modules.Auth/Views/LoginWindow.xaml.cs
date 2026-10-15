using ChromExplorer.Infrastructure.Services;
using ChromExplorer.Modules.Auth.Services;
using ChromExplorer.Modules.Auth.ViewModels;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ChromExplorer.Modules.Auth.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;
        private readonly AuthService _authService;
        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            _authService = new AuthService();
            this.DataContext = _viewModel;
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassVisible.Visibility == Visibility.Visible)
            {
                txtPass.Password = txtPassVisible.Text;
                txtPass.Visibility = Visibility.Visible;
                txtPassVisible.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtPassVisible.Text = txtPass.Password;
                txtPassVisible.Visibility = Visibility.Visible;
                txtPass.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string password = txtPassVisible.Visibility == Visibility.Visible ? txtPassVisible.Text : txtPass.Password;
            bool success = _viewModel.PerformLogin(password, txtUser.Text);

            if (!success)
            {
                return;
            }

            if (success)
            {
                int userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                // Check if first-login window expired (2 hours)
                if (_authService.CheckAndDeactivateExpired(userId))
                {
                    MessageBox.Show(
                        "Your account has been deactivated because you did not login and change your password within 2 hours of account creation.\n\nPlease contact your administrator.",
                        "Account Deactivated",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Check if this is first login — force password change
                if (_authService.IsFirstLogin(userId))
                {
                    var changeWin = new ChangeFirstPasswordWindow(userId, _authService);
                    bool? changed = changeWin.ShowDialog();
                    if (changed != true)
                        return; // user cancelled — don't let them in

                    // Password changed successfully — this session was opened with the old
                    // temp password, so don't let it carry straight into the app. Reset the
                    // login form and make them sign back in with the new password instead.
                    ResetLoginFormAfterPasswordChange();
                    return;
                }
                // Check if password expired (90 days since last change in pass_transaction)
                else if (_authService.IsPasswordExpired(userId))
                {
                    MessageBox.Show(
                        "Your password has expired (90 days). You must change your password to continue.",
                        "Password Expired",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    var changeWin = new ChangeFirstPasswordWindow(userId, _authService);
                    bool? changed = changeWin.ShowDialog();
                    if (changed != true)
                        return; // user cancelled — don't let them in

                    ResetLoginFormAfterPasswordChange();
                    return;
                }
                // Not expired yet, but within the last 5 days before hitting 90 — remind on every
                // login during that window so it doesn't get missed.
                else if (_authService.TryGetPasswordExpiryWarning(userId, 5, out int daysLeft))
                {
                    MessageBox.Show(
                        $"Your password will expire in {daysLeft} day(s). Please change it soon to avoid losing access.",
                        "Password Expiring Soon",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                SessionManager.Username = txtUser.Text;
                this.DialogResult = true;
                this.Close();
            }
        }

        // Undoes the session that AuthenticateUser opened with the now-stale (just-changed)
        // password, and clears the form so the user has to sign in again with the new one —
        // instead of falling straight through into the app on the old session.
        private void ResetLoginFormAfterPasswordChange()
        {
            Application.Current.Properties.Clear();

            txtUser.Clear();
            txtPass.Clear();
            txtPassVisible.Clear();
            txtPass.Visibility = Visibility.Visible;
            txtPassVisible.Visibility = Visibility.Collapsed;

            _viewModel.ErrorMessage = "Password changed successfully. Please log in again with your new password.";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    internal class ChangeFirstPasswordWindow : Window
    {
        private readonly int _userId;
        private readonly AuthService _authService;
        private PasswordBox _pbCurrent, _pbNew, _pbConfirm;
        private TextBox _tbCurrent, _tbNew, _tbConfirm;
        private TextBlock _lblError;

        public ChangeFirstPasswordWindow(int userId, AuthService authService)
        {
            _userId = userId;
            _authService = authService;
            BuildUI();
        }

        private void BuildUI()
        {
            Title = "First Login — Change Password Required";
            Width = 460;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.SingleBorderWindow;

            var outer = new Grid { Margin = new Thickness(30) };
            for (int i = 0; i < 10; i++)
                outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            var title = new TextBlock
            {
                Text = "You must change your password before continuing.",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x3E, 0x50)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(title, 0);
            outer.Children.Add(title);

            // Current Password
            AddLabel(outer, "Current Password *", 1);
            (_pbCurrent, _tbCurrent) = AddPasswordBox(outer, 2);

            // New Password
            AddLabel(outer, "New Password *", 3);
            (_pbNew, _tbNew) = AddPasswordBox(outer, 4);

            // Confirm New Password
            AddLabel(outer, "Confirm New Password *", 5);
            (_pbConfirm, _tbConfirm) = AddPasswordBox(outer, 6);

            // Password hint
            var hint = new TextBlock
            {
                Text = "Min 10 chars · Start with capital · Include lowercase, number, special character",
                FontSize = 11,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Grid.SetRow(hint, 7);
            outer.Children.Add(hint);

            // Error
            _lblError = new TextBlock
            {
                Foreground = Brushes.Red,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(_lblError, 8);
            outer.Children.Add(_lblError);

            // Buttons
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Grid.SetRow(btnPanel, 9);

            var btnSubmit = new Button
            {
                Content = "Change Password",
                Width = 140,
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSubmit.Click += OnSubmit;

            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 34 };
            btnCancel.Click += (s, ev) => { DialogResult = false; };

            btnPanel.Children.Add(btnSubmit);
            btnPanel.Children.Add(btnCancel);
            outer.Children.Add(btnPanel);

            Content = outer;
        }

        private void AddLabel(Grid grid, string text, int row)
        {
            var lbl = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x3E, 0x50)),
                Margin = new Thickness(0, 8, 0, 3)
            };
            Grid.SetRow(lbl, row);
            grid.Children.Add(lbl);
        }

        private (PasswordBox, TextBox) AddPasswordBox(Grid grid, int row)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pb = new PasswordBox { BorderThickness = new Thickness(0), Background = Brushes.Transparent, Height = 28, FontSize = 14 };
            Grid.SetColumn(pb, 0);

            var tb = new TextBox { BorderThickness = new Thickness(0), Background = Brushes.Transparent, Height = 28, FontSize = 14, Visibility = Visibility.Collapsed };
            Grid.SetColumn(tb, 0);

            var btnToggle = new ToggleButton
            {
                Content = "👁",
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Grid.SetColumn(btnToggle, 1);
            btnToggle.Click += (s, ev) =>
            {
                if (tb.Visibility == Visibility.Visible)
                {
                    pb.Password = tb.Text;
                    pb.Visibility = Visibility.Visible;
                    tb.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tb.Text = pb.Password;
                    tb.Visibility = Visibility.Visible;
                    pb.Visibility = Visibility.Collapsed;
                }
            };

            inner.Children.Add(pb);
            inner.Children.Add(tb);
            inner.Children.Add(btnToggle);

            border.Child = inner;
            Grid.SetRow(border, row);
            grid.Children.Add(border);
            return (pb, tb);
        }

        private static string GetPasswordText(PasswordBox pb, TextBox tb)
        {
            return tb.Visibility == Visibility.Visible ? tb.Text : pb.Password;
        }

        private void OnSubmit(object sender, RoutedEventArgs e)
        {
            string current = GetPasswordText(_pbCurrent, _tbCurrent);
            string newPass = GetPasswordText(_pbNew, _tbNew);
            string confirm = GetPasswordText(_pbConfirm, _tbConfirm);

            if (string.IsNullOrWhiteSpace(current)) { _lblError.Text = "Please enter your current password."; return; }
            if (string.IsNullOrWhiteSpace(newPass)) { _lblError.Text = "Please enter a new password."; return; }
            if (string.IsNullOrWhiteSpace(confirm)) { _lblError.Text = "Please confirm your new password."; return; }
            if (newPass != confirm) { _lblError.Text = "New password and confirm password do not match."; return; }
            if (newPass == current) { _lblError.Text = "New password must be different from your current password."; return; }

            var regex = new Regex(@"^[A-Z](?=.*[a-z])(?=.*\d)(?=.*[\W_]).{9,}$");
            if (!regex.IsMatch(newPass))
            {
                _lblError.Text = "Password must be at least 10 characters, start with a capital letter, and include a lowercase letter, a number, and a special character.";
                return;
            }

            string result = _authService.ChangeFirstTimePassword(_userId, current, newPass);
            if (result == "Success")
            {
                MessageBox.Show("Password changed successfully. Welcome to ChromExplorer!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
            {
                _lblError.Text = result;
            }
        }
    }
}
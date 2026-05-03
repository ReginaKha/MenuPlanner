using MenuPlanner.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MenuPlanner.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly AuthService _authService;

        public RegisterWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
            LoadRoles();
        }

        private void LoadRoles()
        {
            try
            {
                var roles = _authService.GetRoles().ToList();
                cmbRole.ItemsSource = roles;
                cmbRole.DisplayMemberPath = "Name";
                cmbRole.SelectedValuePath = "Id";

                if (roles.Count > 0)
                    cmbRole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                txtMessage.Text = "Ошибка: " + ex.Message;
                txtMessage.Visibility = Visibility.Visible;
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) ||
                string.IsNullOrWhiteSpace(txtLogin.Text) ||
                cmbRole.SelectedItem == null ||
                string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                txtMessage.Text = "Заполните все поля";
                txtMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var roleId = (int)cmbRole.SelectedValue;
                var result = _authService.Register(
                    txtLogin.Text.Trim(),
                    txtPassword.Password,
                    txtFullName.Text.Trim(),
                    roleId
                );

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Регистрация", MessageBoxButton.OK, MessageBoxImage.Information);
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
                else
                {
                    txtMessage.Text = result.Message;
                    txtMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                txtMessage.Text = "Ошибка: " + ex.Message;
                txtMessage.Visibility = Visibility.Visible;
            }
        }
        private void LoginLink_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Просто закрываем окно регистрации
            this.Close();
        }
    }

}
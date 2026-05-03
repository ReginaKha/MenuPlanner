using MenuPlanner.Services;
using System.Windows;
using System.Windows.Input;

namespace MenuPlanner.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                return;
            }

            var result = _authService.Login(login, password);

            if (result.Success)
            {
                // Открываем главное окно

                var mainWindow = new MainWindow();
                mainWindow.Initialize(_authService.CurrentUser);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowError(result.Message);
            }
        }

        private void ShowError(string message)
        {
            txtErrorMessage.Text = message;
            txtErrorMessage.Visibility = Visibility.Visible;

            // Очищаем поле пароля
            txtPassword.Clear();
            txtPassword.Focus();
        }

        // Кнопка перехода к регистрации (добавьте в XAML)
        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Hide(); // Hide вместо Close, чтобы вернуться после регистрации

            // Закрыть окно входа, когда регистрация завершена
            registerWindow.Closed += (s, args) => this.Show();
        }

        private void ForgotPassword_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(
                "Для сброса пароля обратитесь к администратору системы.\n\n" +
                "Администратор может сбросить ваш пароль в разделе \"Пользователи\".",
                "Сброс пароля",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
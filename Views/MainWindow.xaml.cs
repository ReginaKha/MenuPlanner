using MenuPlanner.Model;
using MenuPlanner.Services;
using MenuPlanner.Views.Pages;
using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MenuPlanner.Views
{
    public partial class MainWindow : Window
    {
        private Users _currentUser;  // ⚠️ Класс сущности — "user" (не Users!)

        // ✅ 1. ОБЯЗАТЕЛЬНЫЙ конструктор без параметров для XAML
        public MainWindow()
        {
            InitializeComponent();
        }

        // ✅ 2. Метод для передачи пользователя ПОСЛЕ создания окна
        public void Initialize(Users currentUser)
        {
            _currentUser = currentUser;
            InitializeUserInterface();

            // Навигация по умолчанию
            MainFrame.Navigate(new DashboardPage());
            txtPageTitle.Text = "Главная";
        }

        private void InitializeUserInterface()
        {
            if (_currentUser == null) return;

            txtUserName.Text = _currentUser.FullName;

            // Навигационное свойство — "Roles" (множественное число, как в модели)
            txtUserRole.Text = _currentUser.Roles?.Name ?? "Пользователь";

            var roleName = _currentUser.Roles?.Name;

            // Сброс видимости
            btnMenu.Visibility = Visibility.Visible;
            btnInventory.Visibility = Visibility.Visible;
            btnPurchase.Visibility = Visibility.Visible;
            btnAnalytics.Visibility = Visibility.Visible;
            btnUsers.Visibility = Visibility.Visible;

            // Ограничения по ролям (используем русские названия из БД)
            if (roleName == "Калькулятор")
            {
                btnMenu.Visibility = Visibility.Collapsed;
                btnInventory.Visibility = Visibility.Collapsed;
                btnPurchase.Visibility = Visibility.Collapsed;
                btnAnalytics.Visibility = Visibility.Collapsed;
                btnUsers.Visibility = Visibility.Collapsed;
            }
            else if (roleName == "Кладовщик")
            {
                btnMenu.Visibility = Visibility.Collapsed;
                btnAnalytics.Visibility = Visibility.Collapsed;
                btnUsers.Visibility = Visibility.Collapsed;
            }
            else if (roleName == "Бухгалтер")
            {
                btnMenu.Visibility = Visibility.Collapsed;
                btnInventory.Visibility = Visibility.Collapsed;
                btnUsers.Visibility = Visibility.Collapsed;
            }
            else if (roleName == "Шеф-повар")
            {
                btnUsers.Visibility = Visibility.Collapsed;
            }
            // Администратор видит всё
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Выход из системы
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, есть ли словарь
                var dict = Application.Current.Resources["DarkThemeDict"];
                if (dict == null)
                {
                    MessageBox.Show("❌ DarkThemeDict не найден в ресурсах!\n" +
                                  "Проверьте App.xaml", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Переключаем
                ThemeService.ToggleTheme();

                // Обновляем фон окна
                var bg = Application.Current.FindResource("BackgroundBrush") as System.Windows.Media.Brush;
                if (bg != null)
                {
                    this.Background = bg;
                }

                // Перезагружаем текущую страницу
                if (MainFrame.Content is Page currentPage)
                {
                    MainFrame.Navigate(currentPage);
                }

                // Меняем иконку
                if (sender is Button btn)
                {
                    btn.Content = ThemeService.IsDark ? "☀️" : "🌓";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageTag)
            {
                NavigateToPage(pageTag);
            }
        }

        private void NavigateToPage(string pageTag)
        {
            Page page = pageTag switch
            {
                "Dashboard" => new DashboardPage(),
                "Ingredients" => new IngredientsPage(),
                "Recipes" => new RecipesPage(),
                "Menu" => new MenuPage(),
                "Inventory" => new InventoryPage(),
                "PurchaseRequests" => new PurchaseRequestsPage(),
                "Analytics" => new AnalyticsPage(),
                "Users" => new UsersPage(),
                "Settings" => new SettingsPage(),
                _ => new DashboardPage()
            };

            MainFrame.Navigate(page);
            txtPageTitle.Text = GetPageTitle(pageTag);
        }

        private string GetPageTitle(string pageTag)
        {
            return pageTag switch
            {
                "Dashboard" => "Главная",
                "Ingredients" => "Ингредиенты",
                "Recipes" => "Рецепты",
                "Menu" => "Планирование меню",
                "Inventory" => "Склад",
                "PurchaseRequests" => "Заявки на закупку",
                "Analytics" => "Аналитика",
                "Users" => "Пользователи",
                "Settings" => "Настройки",
                _ => "MenuPlanner"
            };
        }
    }
}
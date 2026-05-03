using System.Linq;
using System.Windows;

namespace MenuPlanner.Services
{
    public static class ThemeService
    {
        private static bool _isDark = false;
        private static ResourceDictionary _darkDict;

        public static bool IsDark => _isDark;

        public static void ToggleTheme()
        {
            _isDark = !_isDark;

            // Ищем словарь
            if (_darkDict == null)
            {
                _darkDict = Application.Current.Resources["DarkThemeDict"] as ResourceDictionary;

                if (_darkDict == null)
                {
                    MessageBox.Show("❌ Не найден словарь DarkThemeDict!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var merged = Application.Current.Resources.MergedDictionaries;

            if (_isDark)
            {
                if (!merged.Contains(_darkDict))
                {
                    merged.Add(_darkDict);
                    MessageBox.Show("✅ Тёмная тема включена", "Тема",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                if (merged.Contains(_darkDict))
                {
                    merged.Remove(_darkDict);
                    MessageBox.Show("✅ Светлая тема включена", "Тема",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MenuPlanner.Model;

namespace MenuPlanner.Views.Pages
{
    public partial class IngredientsPage : Page
    {
        private readonly MenuPlannerEntities _context;
        private List<Ingredients> _allIngredients = new List<Ingredients>();

        public IngredientsPage()
        {
            InitializeComponent();
            _context = new MenuPlannerEntities();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем данные
                var list = _context.Ingredients
                    .Include("Categories")
                    .OrderBy(i => i.Name)
                    .ToList();

                _allIngredients = list ?? new List<Ingredients>();

                // Безопасная подгрузка категорий
                foreach (var item in _allIngredients)
                {
                    if (item?.CategoryId.HasValue == true && item.Categories == null)
                    {
                        item.Categories = _context.Categories
                            .FirstOrDefault(c => c.Id == item.CategoryId.Value);
                    }
                }

                LoadCategories();
                dgIngredients.ItemsSource = _allIngredients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add(new ComboBoxItem { Content = "Все категории", Tag = (int?)null });

            var cats = _context.Categories.OrderBy(c => c.Name).ToList();
            foreach (var cat in cats)
            {
                cmbCategory.Items.Add(new ComboBoxItem { Content = cat.Name, Tag = cat.Id });
            }
            cmbCategory.SelectedIndex = 0;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddIngredientWindow(_context);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                MessageBox.Show("Добавлено!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DgIngredients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgIngredients.SelectedItem is Ingredients item)
            {
                var dialog = new AddIngredientWindow(_context, item);
                if (dialog.ShowDialog() == true)
                {
                    _context.SaveChanges();
                    LoadData();
                    MessageBox.Show("Обновлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void dgIngredients_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && dgIngredients.SelectedItem is Ingredients item)
            {
                if (MessageBox.Show($"Удалить \"{item.Name}\"?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _context.Ingredients.Remove(item);
                    _context.SaveChanges();
                    LoadData();
                    MessageBox.Show("Удалено!", "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            try
            {
                if (dgIngredients == null) return;

                string search = (txtSearch?.Text ?? "").ToLower().Trim();
                int? catId = (cmbCategory?.SelectedItem as ComboBoxItem)?.Tag as int?;

                var filtered = _allIngredients.Where(i =>
                {
                    if (i == null) return false;

                    // Поиск по названию
                    bool matchName = string.IsNullOrEmpty(search) ||
                                    (!string.IsNullOrEmpty(i.Name) && i.Name.ToLower().Contains(search));

                    // Фильтр по категории
                    bool matchCat = !catId.HasValue ||
                                   (i.CategoryId.HasValue && i.CategoryId == catId.Value);

                    return matchName && matchCat;
                }).ToList();

                // ✅ НИКОГДА не ставим null!
                dgIngredients.ItemsSource = filtered;
            }
            catch
            {
                // При любой ошибке показываем все данные
                dgIngredients.ItemsSource = _allIngredients;
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cmbCategory.SelectedIndex = 0;
            dgIngredients.ItemsSource = _allIngredients;
        }
    }
}
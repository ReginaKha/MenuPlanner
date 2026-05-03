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
    /// <summary>
    /// Логика взаимодействия для RecipesPage.xaml
    /// </summary>
    public partial class RecipesPage : Page
    {
        private readonly MenuPlannerEntities _context;
        private List<Recipes> _allRecipes = new List<Recipes>();

        public RecipesPage()
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
                var list = _context.Recipes
                    .Include("RecipeIngredients")
                    .Include("RecipeIngredients.Ingredients")
                    .OrderBy(r => r.Name)
                    .ToList();

                _allRecipes = list ?? new List<Recipes>();

                LoadCategories();
                dgRecipes.ItemsSource = _allRecipes;
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
            cmbCategory.Items.Add(new ComboBoxItem { Content = "Все категории", Tag = (string)null });

            // Получаем уникальные категории из рецептов
            var categories = _allRecipes
                .Where(r => !string.IsNullOrEmpty(r.Category))
                .Select(r => r.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var cat in categories)
            {
                cmbCategory.Items.Add(new ComboBoxItem { Content = cat, Tag = cat });
            }
            cmbCategory.SelectedIndex = 0;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddRecipeWindow(_context);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                MessageBox.Show("Рецепт добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DgRecipes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgRecipes.SelectedItem is Recipes recipe)
            {
                var dialog = new AddRecipeWindow(_context, recipe);
                if (dialog.ShowDialog() == true)
                {
                    _context.SaveChanges();
                    LoadData();
                    MessageBox.Show("Рецепт обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DgRecipes_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && dgRecipes.SelectedItem is Recipes recipe)
            {
                if (MessageBox.Show($"Удалить рецепт \"{recipe.Name}\"?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // Удаляем сначала ингредиенты рецепта
                    var recipeIngredients = _context.RecipeIngredients
                        .Where(ri => ri.RecipeId == recipe.Id)
                        .ToList();
                    foreach (var ingredient in recipeIngredients)
                    {
                        _context.RecipeIngredients.Remove(ingredient);
                    }

                    // Затем удаляем рецепт
                    _context.Recipes.Remove(recipe);
                    _context.SaveChanges();
                    LoadData();
                    MessageBox.Show("Рецепт удален!", "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
        private void cmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            try
            {
                if (dgRecipes == null) return;
                if (_allRecipes == null)
                {
                    dgRecipes.ItemsSource = new List<Recipes>();
                    return;
                }

                // Получаем поисковый запрос
                string search = "";
                if (txtSearch != null && !string.IsNullOrEmpty(txtSearch.Text))
                {
                    search = txtSearch.Text.ToLower().Trim();
                }

                // Получаем выбранную категорию
                string selectedCategory = null;
                if (cmbCategory != null && cmbCategory.SelectedItem is ComboBoxItem catItem && catItem.Tag is string catTag)
                {
                    selectedCategory = catTag;
                }

                // Получаем выбранный статус
                string selectedStatus = null;
                if (cmbStatus != null && cmbStatus.SelectedItem is ComboBoxItem statusItem)
                {
                    var statusContent = statusItem.Content.ToString();
                    if (statusContent != "Все статусы")
                    {
                        selectedStatus = statusContent;
                    }
                }

                // Фильтруем список
                var filtered = new List<Recipes>();
                foreach (var recipe in _allRecipes)
                {
                    if (recipe == null) continue;

                    // Проверка по названию
                    bool matchName = true;
                    if (!string.IsNullOrEmpty(search))
                    {
                        if (string.IsNullOrEmpty(recipe.Name))
                        {
                            matchName = false;
                        }
                        else
                        {
                            matchName = recipe.Name.ToLower().Contains(search);
                        }
                    }

                    // Проверка по категории
                    bool matchCat = true;
                    if (selectedCategory != null)
                    {
                        matchCat = recipe.Category == selectedCategory;
                    }

                    // Проверка по статусу
                    bool matchStatus = true;
                    if (selectedStatus != null)
                    {
                        matchStatus = recipe.Status == selectedStatus;
                    }

                    if (matchName && matchCat && matchStatus)
                    {
                        filtered.Add(recipe);
                    }
                }

                dgRecipes.ItemsSource = filtered;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка фильтрации: {ex.Message}");

                if (dgRecipes != null && _allRecipes != null)
                {
                    dgRecipes.ItemsSource = _allRecipes;
                }
                else
                {
                    dgRecipes.ItemsSource = new List<Recipes>();
                }
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cmbCategory.SelectedIndex = 0;
            cmbStatus.SelectedIndex = 0;
            dgRecipes.ItemsSource = _allRecipes;
        }
    }
}

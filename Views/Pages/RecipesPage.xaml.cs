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
            cmbCategory.Items.Add(new ComboBoxItem { Content = "Все категории", Tag = (int?)null });

            // Загружаем категории из справочника
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            foreach (var cat in categories)
            {
                cmbCategory.Items.Add(new ComboBoxItem { Content = cat.Name, Tag = cat.Id });
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
                int? selectedCategoryId = null;
                if (cmbCategory != null && cmbCategory.SelectedItem is ComboBoxItem catItem && catItem.Tag is int catTag)
                {
                    selectedCategoryId = catTag;
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
                    if (selectedCategoryId.HasValue)
                    {
                        if (!recipe.CategoryId.HasValue)
                        {
                            matchCat = false;
                        }
                        else
                        {
                            matchCat = recipe.CategoryId.Value == selectedCategoryId.Value;
                        }
                    }

                    // Проверка по статусу (закомментировано, т.к. поле Status удалено из модели)
                    bool matchStatus = true;
                    /*
                    if (selectedStatus != null)
                    {
                        matchStatus = recipe.Status == selectedStatus;
                    }
                    */

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

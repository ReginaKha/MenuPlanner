using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MenuPlanner.Model;

namespace MenuPlanner.Views
{
    public partial class AddRecipeWindow : Window
    {
        private readonly MenuPlannerEntities _context;
        private Recipes _editingRecipe;
        private List<RecipeIngredientItem> _recipeIngredients = new List<RecipeIngredientItem>();

        public AddRecipeWindow(MenuPlannerEntities context, Recipes recipe = null)
        {
            InitializeComponent();
            _context = context;
            _editingRecipe = recipe;

            // Загружаем категории
            var categories = _context.Categories.Where(c => c.IsActive == true).OrderBy(c => c.Name).ToList();
            cmbCategory.ItemsSource = categories;
            cmbCategory.DisplayMemberPath = "Name";
            cmbCategory.SelectedValuePath = "Id";

            // Загружаем статусы
            cmbStatus.SelectedIndex = 0;

            if (_editingRecipe != null)
            {
                Title = "Редактирование рецепта";
                txtName.Text = _editingRecipe.Name;
                txtYieldPortions.Text = _editingRecipe.YieldPortions?.ToString();

                // Устанавливаем категорию
                if (!string.IsNullOrEmpty(_editingRecipe.Category))
                {
                    var cat = categories.FirstOrDefault(c => c.Name == _editingRecipe.Category);
                    if (cat != null)
                        cmbCategory.SelectedItem = cat;
                }

                // Устанавливаем статус
                if (!string.IsNullOrEmpty(_editingRecipe.Status))
                {
                    foreach (ComboBoxItem item in cmbStatus.Items)
                    {
                        if (item.Content.ToString() == _editingRecipe.Status)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }

                // Загружаем ингредиенты рецепта
                LoadRecipeIngredients();
            }
        }

        private void LoadRecipeIngredients()
        {
            _recipeIngredients.Clear();

            if (_editingRecipe != null && _editingRecipe.Id > 0)
            {
                var recipeIngredients = _context.RecipeIngredients
                    .Where(ri => ri.RecipeId == _editingRecipe.Id)
                    .Include("Ingredients")
                    .ToList();

                foreach (var ri in recipeIngredients)
                {
                    _recipeIngredients.Add(new RecipeIngredientItem
                    {
                        IngredientId = ri.IngredientId ?? 0,
                        IngredientName = ri.Ingredients?.Name ?? "Неизвестно",
                        Quantity = ri.Quantity,
                        Unit = ri.Unit,
                        RecipeIngredientId = ri.Id
                    });
                }
            }

            dgRecipeIngredients.ItemsSource = null;
            dgRecipeIngredients.ItemsSource = _recipeIngredients;
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void BtnAddIngredient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectIngredientWindow(_context);
            if (dialog.ShowDialog() == true)
            {
                var selectedIngredient = dialog.SelectedIngredient;
                var quantity = dialog.SelectedQuantity;
                var unit = dialog.SelectedUnit;

                // Проверяем, не добавлен ли уже этот ингредиент
                var existing = _recipeIngredients.FirstOrDefault(ri => ri.IngredientId == selectedIngredient.Id);
                if (existing != null)
                {
                    MessageBox.Show("Этот ингредиент уже добавлен!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _recipeIngredients.Add(new RecipeIngredientItem
                {
                    IngredientId = selectedIngredient.Id,
                    IngredientName = selectedIngredient.Name,
                    Quantity = quantity,
                    Unit = unit ?? selectedIngredient.Unit
                });

                dgRecipeIngredients.ItemsSource = null;
                dgRecipeIngredients.ItemsSource = _recipeIngredients;
            }
        }

        private void BtnRemoveIngredient_Click(object sender, RoutedEventArgs e)
        {
            if (dgRecipeIngredients.SelectedItem is RecipeIngredientItem item)
            {
                _recipeIngredients.Remove(item);
                dgRecipeIngredients.ItemsSource = null;
                dgRecipeIngredients.ItemsSource = _recipeIngredients;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Заполните название рецепта!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_recipeIngredients.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один ингредиент!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_editingRecipe == null)
                {
                    _editingRecipe = new Recipes();
                    _context.Recipes.Add(_editingRecipe);
                }

                _editingRecipe.Name = txtName.Text.Trim();
                
                var selectedCategory = cmbCategory.SelectedItem as Categories;
                _editingRecipe.Category = selectedCategory?.Name;
                
                _editingRecipe.YieldPortions = int.TryParse(txtYieldPortions.Text, out var portions) ? portions : (int?)null;
                
                var selectedStatus = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
                _editingRecipe.Status = selectedStatus;
                
                _editingRecipe.UpdatedAt = DateTime.Now;

                _context.SaveChanges();

                // Сохраняем ингредиенты
                SaveRecipeIngredients();

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveRecipeIngredients()
        {
            // Удаляем старые связи
            var existingIngredients = _context.RecipeIngredients
                .Where(ri => ri.RecipeId == _editingRecipe.Id)
                .ToList();

            _context.RecipeIngredients.RemoveRange(existingIngredients);

            // Добавляем новые
            foreach (var item in _recipeIngredients)
            {
                _context.RecipeIngredients.Add(new RecipeIngredients
                {
                    RecipeId = _editingRecipe.Id,
                    IngredientId = item.IngredientId,
                    Quantity = item.Quantity,
                    Unit = item.Unit
                });
            }

            _context.SaveChanges();
        }
    }

    public class RecipeIngredientItem
    {
        public int RecipeIngredientId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }
}

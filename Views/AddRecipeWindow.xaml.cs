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
                txtRecipeNumber.Text = _editingRecipe.RecipeNumber ?? string.Empty;
                txtSource.Text = _editingRecipe.Source ?? string.Empty;
                txtYieldWeight.Text = _editingRecipe.YieldWeight.HasValue ? _editingRecipe.YieldWeight.Value.ToString("F2") : "0.00";
                txtBaseServings.Text = _editingRecipe.BaseServings.ToString();
                txtTechnology.Text = _editingRecipe.Technology ?? string.Empty;
                txtMarkupPercent.Text = _editingRecipe.MarkupPercent.HasValue ? _editingRecipe.MarkupPercent.Value.ToString("F2") : "0.00";

                // Устанавливаем категорию по ID
                if (_editingRecipe.CategoryId.HasValue)
                {
                    cmbCategory.SelectedValue = _editingRecipe.CategoryId.Value;
                }
                else if (!string.IsNullOrEmpty(_editingRecipe.Category))
                {
                    // Fallback: поиск категории по имени если CategoryId не заполнен
                    var categoryByName = categories.FirstOrDefault(c => c.Name == _editingRecipe.Category);
                    if (categoryByName != null)
                    {
                        cmbCategory.SelectedItem = categoryByName;
                    }
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
                
                // Обновляем расчет стоимости
                CalculateCost();
            }
            else
            {
                // Значения по умолчанию для нового рецепта
                txtBaseServings.Text = "1";
                txtMarkupPercent.Text = "30.00";
                txtYieldWeight.Text = "0.00";
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
                    var ingredient = ri.Ingredients;
                    decimal pricePerUnit = ingredient?.DefaultPrice ?? 0;
                    decimal grossWeight = ri.GrossWeight > 0 ? ri.GrossWeight : ri.Quantity;
                    // Цена рассчитывается за грамм (если цена за кг, то делим на 1000)
                    decimal totalCost = grossWeight * pricePerUnit / 1000;

                    _recipeIngredients.Add(new RecipeIngredientItem
                    {
                        IngredientId = ri.IngredientId ?? 0,
                        IngredientName = ingredient?.Name ?? "Неизвестно",
                        GrossWeight = grossWeight,
                        NetWeight = ri.NetWeight,
                        Unit = ri.Unit ?? ingredient?.Unit ?? "шт",
                        PricePerUnit = pricePerUnit,
                        TotalCost = totalCost,
                        RecipeIngredientId = ri.Id
                    });
                }
            }

            dgRecipeIngredients.ItemsSource = null;
            dgRecipeIngredients.ItemsSource = _recipeIngredients;
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем ввод только цифр для полей целых чисел
            if (sender == txtBaseServings)
            {
                e.Handled = !int.TryParse(e.Text, out _);
            }
        }

        private void TxtDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Для десятичных чисел разрешаем цифры, точку и запятую
            e.Handled = !decimal.TryParse(e.Text, out _) && e.Text != "." && e.Text != ",";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void BtnAddIngredient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectIngredientWindow(_context);
            if (dialog.ShowDialog() == true)
            {
                var selectedIngredient = dialog.SelectedIngredient;
                var grossWeight = dialog.SelectedGrossWeight;
                var netWeight = dialog.SelectedNetWeight;
                var unit = dialog.SelectedUnit;

                // Проверяем, не добавлен ли уже этот ингредиент
                var existing = _recipeIngredients.FirstOrDefault(ri => ri.IngredientId == selectedIngredient.Id);
                if (existing != null)
                {
                    MessageBox.Show("Этот ингредиент уже добавлен!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal pricePerUnit = selectedIngredient.DefaultPrice ?? 0;
                // Цена рассчитывается за грамм (если цена за кг, то делим на 1000)
                // Для простоты считаем что цена в БД указана за ту же единицу, что и вес в граммах
                decimal totalCost = grossWeight * pricePerUnit / 1000;

                _recipeIngredients.Add(new RecipeIngredientItem
                {
                    IngredientId = selectedIngredient.Id,
                    IngredientName = selectedIngredient.Name,
                    GrossWeight = grossWeight,
                    NetWeight = netWeight,
                    Unit = unit ?? selectedIngredient.Unit ?? "шт",
                    PricePerUnit = pricePerUnit,
                    TotalCost = totalCost
                });

                dgRecipeIngredients.ItemsSource = null;
                dgRecipeIngredients.ItemsSource = _recipeIngredients;
                
                // Обновляем расчет стоимости
                CalculateCost();
            }
        }

        private void BtnRemoveIngredient_Click(object sender, RoutedEventArgs e)
        {
            if (dgRecipeIngredients.SelectedItem is RecipeIngredientItem item)
            {
                _recipeIngredients.Remove(item);
                dgRecipeIngredients.ItemsSource = null;
                dgRecipeIngredients.ItemsSource = _recipeIngredients;
                
                // Обновляем расчет стоимости
                CalculateCost();
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
                _editingRecipe.RecipeNumber = txtRecipeNumber.Text?.Trim();
                _editingRecipe.Source = txtSource.Text?.Trim();
                _editingRecipe.YieldWeight = decimal.TryParse(txtYieldWeight.Text, out var yieldWeight) ? yieldWeight : (decimal?)null;
                _editingRecipe.BaseServings = int.TryParse(txtBaseServings.Text, out var baseServings) ? baseServings : 1;
                _editingRecipe.Technology = txtTechnology.Text?.Trim();
                _editingRecipe.MarkupPercent = decimal.TryParse(txtMarkupPercent.Text, out var markup) ? markup : (decimal?)null;
                
                var selectedCategory = cmbCategory.SelectedItem as Categories;
                _editingRecipe.CategoryId = selectedCategory?.Id;
                _editingRecipe.Category = selectedCategory?.Name;
                
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

            foreach (var ingredient in existingIngredients)
            {
                _context.RecipeIngredients.Remove(ingredient);
            }
            _context.SaveChanges(); // Сохраняем удаления сразу

            // Добавляем новые
            int sortOrder = 0;
            foreach (var item in _recipeIngredients)
            {
                var newIngredient = new RecipeIngredients
                {
                    RecipeId = _editingRecipe.Id,
                    IngredientId = item.IngredientId,
                    GrossWeight = item.GrossWeight,
                    NetWeight = item.NetWeight,
                    Quantity = item.GrossWeight, // Для совместимости дублируем вес в Quantity
                    Unit = item.Unit,
                    SortOrder = sortOrder++
                };
                _context.RecipeIngredients.Add(newIngredient);
            }

            _context.SaveChanges();
        }

        private void CalculateCost()
        {
            decimal totalCost = 0;

            if (_recipeIngredients != null)
            {
                foreach (var item in _recipeIngredients)
                {
                    totalCost += item.TotalCost;
                }
            }

            txtTotalCost.Text = $"{totalCost:F2} ₽";

            // Расчет цены продажи с учетом наценки
            decimal markupPercent = decimal.TryParse(txtMarkupPercent.Text, out var mp) ? mp : 0;
            decimal sellingPrice = totalCost * (1 + markupPercent / 100);
            txtSellingPrice.Text = $"{sellingPrice:F2} ₽";

            // Цена за порцию
            int portions = int.TryParse(txtBaseServings.Text, out var p) ? p : 0;
            if (portions > 0)
            {
                decimal costPerPortion = sellingPrice / portions;
                txtCostPerPortion.Text = $"{costPerPortion:F2} ₽";
            }
            else
            {
                txtCostPerPortion.Text = "—";
            }
        }

        private void TxtMarkupPercent_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateCost();
        }
    }

    public class RecipeIngredientItem
    {
        public int RecipeIngredientId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal NetWeight { get; set; }
        public string Unit { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalCost { get; set; }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MenuPlanner.Model;

namespace MenuPlanner.Views
{
    public partial class AddIngredientWindow : Window
    {
        private readonly MenuPlannerEntities _context;
        private Ingredients _editingItem;

        public AddIngredientWindow(MenuPlannerEntities context, Ingredients item = null)
        {
            InitializeComponent();
            _context = context;
            _editingItem = item;

            // Загружаем категории
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            cmbCategory.ItemsSource = categories;
            cmbCategory.DisplayMemberPath = "Name";  // ← ВАЖНО: что показывать
            cmbCategory.SelectedValuePath = "Id";    // ← ВАЖНО: какое поле хранить

            if (_editingItem != null)
            {
                Title = "Редактирование ингредиента";
                txtName.Text = _editingItem.Name;
                txtUnit.Text = _editingItem.Unit;
                txtPrice.Text = _editingItem.DefaultPrice?.ToString();
                txtShelfLife.Text = _editingItem.ShelfLifeDays?.ToString();

                // ✅ ВАЖНО: Устанавливаем выбранную категорию
                if (_editingItem.CategoryId.HasValue)
                {
                    cmbCategory.SelectedValue = _editingItem.CategoryId.Value;
                }

                chkActive.IsChecked = _editingItem.IsActive;
            }
        }

        // Только цифры для цены и срока
        private void TxtPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtUnit.Text))
            {
                MessageBox.Show("Заполните название и единицу измерения!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_editingItem == null)
                {
                    _editingItem = new Ingredients();
                    _context.Ingredients.Add(_editingItem);
                }

                _editingItem.Name = txtName.Text.Trim();
                _editingItem.Unit = txtUnit.Text.Trim();
                _editingItem.DefaultPrice = decimal.TryParse(txtPrice.Text, out var price) ? price : 0;
                _editingItem.ShelfLifeDays = int.TryParse(txtShelfLife.Text, out var shelf) ? shelf : 0;

                // ✅ ВАЖНО: Правильное сохранение категории
                var selectedCategory = cmbCategory.SelectedItem as Categories;
                if (selectedCategory != null)
                {
                    _editingItem.CategoryId = selectedCategory.Id;
                    _editingItem.Categories = selectedCategory;
                }
                else
                {
                    _editingItem.CategoryId = null;
                    _editingItem.Categories = null;
                }

                _editingItem.IsActive = chkActive.IsChecked ?? true;

                _context.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MenuPlanner.Model;

namespace MenuPlanner.Views
{
    public partial class SelectIngredientWindow : Window
    {
        private readonly MenuPlannerEntities _context;
        private List<Ingredients> _allIngredients = new List<Ingredients>();
        public Ingredients SelectedIngredient { get; private set; }
        public decimal SelectedQuantity { get; private set; }
        public string SelectedUnit { get; private set; }

        public SelectIngredientWindow(MenuPlannerEntities context)
        {
            InitializeComponent();
            _context = context;
            LoadIngredients();
        }

        private void LoadIngredients()
        {
            _allIngredients = _context.Ingredients
                .Where(i => i.IsActive == true)
                .OrderBy(i => i.Name)
                .ToList();

            dgIngredients.ItemsSource = _allIngredients;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void BtnFind_Click(object sender, RoutedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            string search = txtSearch?.Text?.ToLower().Trim() ?? "";

            if (string.IsNullOrEmpty(search))
            {
                dgIngredients.ItemsSource = _allIngredients;
            }
            else
            {
                var filtered = _allIngredients
                    .Where(i => i.Name.ToLower().Contains(search))
                    .ToList();
                dgIngredients.ItemsSource = filtered;
            }
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !decimal.TryParse(e.Text, out _);
        }

        private void DgIngredients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgIngredients.SelectedItem is Ingredients ingredient)
            {
                SelectIngredient(ingredient);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (dgIngredients.SelectedItem is Ingredients ingredient)
            {
                SelectIngredient(ingredient);
            }
            else
            {
                MessageBox.Show("Выберите ингредиент!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SelectIngredient(Ingredients ingredient)
        {
            // Получаем количество
            if (!decimal.TryParse(txtQuantity.Text, out var quantity) || quantity <= 0)
            {
                quantity = 1; // Значение по умолчанию
            }

            SelectedIngredient = ingredient;
            SelectedQuantity = quantity;
            SelectedUnit = txtUnit.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

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
        public decimal SelectedGrossWeight { get; private set; }
        public decimal SelectedNetWeight { get; private set; }
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

        private void TxtDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Для десятичных чисел разрешаем цифры, точку и запятую
            e.Handled = !decimal.TryParse(e.Text, out _) && e.Text != "." && e.Text != ",";
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
            // Получаем вес брутто
            if (!decimal.TryParse(txtGrossWeight.Text, out var grossWeight) || grossWeight <= 0)
            {
                grossWeight = 100; // Значение по умолчанию 100г
            }

            // Получаем вес нетто
            if (!decimal.TryParse(txtNetWeight.Text, out var netWeight) || netWeight <= 0)
            {
                netWeight = grossWeight; // По умолчанию нетто = брутто
            }

            // Валидация: нетто не может быть больше брутто
            if (netWeight > grossWeight)
            {
                MessageBox.Show("Вес нетто не может быть больше веса брутто!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Если ед. измерения не указана, используем значение из ингредиента
            string unit = string.IsNullOrWhiteSpace(txtUnit.Text) 
                ? ingredient.Unit 
                : txtUnit.Text;

            SelectedIngredient = ingredient;
            SelectedGrossWeight = grossWeight;
            SelectedNetWeight = netWeight;
            SelectedUnit = unit;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

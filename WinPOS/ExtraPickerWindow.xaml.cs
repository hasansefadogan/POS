using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WinPOS
{
    public partial class ExtraPickerWindow : Window
    {
        public class ExtraVm
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public bool IsSelected { get; set; }

            public string PriceText => Price <= 0
                ? "Ücretsiz"
                : $"{Price:0.00} CHF";
        }

        public List<ExtraVm> Items { get; } = new();
        public List<ExtraVm> Selected => Items.Where(x => x.IsSelected).ToList();

        public ExtraPickerWindow(string productName,
            IEnumerable<(int id, string name, decimal price)> extras)
        {
            InitializeComponent();

            TxtTitle.Text = $"Ekstralar – {productName}";

            foreach (var e in extras)
            {
                Items.Add(new ExtraVm
                {
                    Id = e.id,
                    Name = e.name,
                    Price = e.price
                });
            }

            ItemsExtras.ItemsSource = Items;

            CompositionTarget.Rendering += (_, __) => UpdateTotal();
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            var total = Items.Where(x => x.IsSelected).Sum(x => x.Price);
            TxtExtraTotal.Text = $"Ekstralar Toplamı: {total:0.00} CHF";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

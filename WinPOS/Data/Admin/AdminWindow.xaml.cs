using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using WinPOS.Data;
using WinPOS.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinPOS.Admin
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            PosDb.Init();
            InitializeComponent();
            DataContext = this;
            LoadAdminCategories();
            LoadAdminProducts();

            LoadAll();
        }

        private void LoadAll()
        {
            LoadCategories();
            LoadProducts();
            LoadExtras();
        }

        private void LoadCategories()
        {
            CmbCategory.Items.Clear();
            ListExtraCategories.Items.Clear();
            ListCategories.Items.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Categories ORDER BY Name;";
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                var id = r.GetInt32(0);
                var name = r.GetString(1);

                CmbCategory.Items.Add(new ComboBoxItemEx(id, name));
                ListExtraCategories.Items.Add(new ComboBoxItemEx(id, name)); // çoklu seçim için
                ListCategories.Items.Add($"{id} - {name}");
            }

            if (CmbCategory.Items.Count > 0 && CmbCategory.SelectedIndex < 0)
                CmbCategory.SelectedIndex = 0;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void LoadAdminCategories()
        {
            AdminCategories.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Categories ORDER BY Name;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                AdminCategories.Add(new Category
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1)
                });
            }

            SelectedAdminCategory ??= AdminCategories.FirstOrDefault();
        }

        private void LoadAdminProducts()
        {
            AdminProducts.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Price, Vat, CategoryId FROM Products ORDER BY Name;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                AdminProducts.Add(new Product
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Price = Convert.ToDecimal(r.GetDouble(2)),
                    Vat = Convert.ToDecimal(r.GetDouble(3)),
                    CategoryId = r.GetInt32(4)
                });
            }
        }
        private void BtnProductNew_Click(object sender, RoutedEventArgs e)
        {
            SelectedAdminProduct = null;
            EditProductName = "";
            EditProductPrice = "";
            EditProductVat = "7.7";
            AdminStatusText = "Yeni ürün girişi hazır.";
        }

        private void BtnProductAdd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAdminCategory == null)
            {
                MessageBox.Show("Kategori seçmelisin.");
                return;
            }

            if (!decimal.TryParse(EditProductPrice.Replace(",", "."), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                MessageBox.Show("Fiyat geçersiz.");
                return;
            }

            if (!decimal.TryParse(EditProductVat.Replace(",", "."), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var vat))
            {
                MessageBox.Show("KDV geçersiz. Örn: 2.6 veya 7.7");
                return;
            }

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Products (Name, Price, Vat, CategoryId)
VALUES (@n, @p, @v, @cid);";
            cmd.Parameters.AddWithValue("@n", EditProductName.Trim());
            cmd.Parameters.AddWithValue("@p", (double)price);
            cmd.Parameters.AddWithValue("@v", (double)vat);
            cmd.Parameters.AddWithValue("@cid", SelectedAdminCategory.Id);
            cmd.ExecuteNonQuery();

            LoadAdminProducts();
            AdminStatusText = "Ürün eklendi.";
        }

        private void BtnProductUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAdminProduct == null)
            {
                MessageBox.Show("Güncellemek için listeden bir ürün seç.");
                return;
            }
            if (SelectedAdminCategory == null)
            {
                MessageBox.Show("Kategori seçmelisin.");
                return;
            }

            if (!decimal.TryParse(EditProductPrice.Replace(",", "."), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                MessageBox.Show("Fiyat geçersiz.");
                return;
            }

            if (!decimal.TryParse(EditProductVat.Replace(",", "."), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var vat))
            {
                MessageBox.Show("KDV geçersiz. Örn: 2.6 veya 7.7");
                return;
            }

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
UPDATE Products
SET Name=@n, Price=@p, Vat=@v, CategoryId=@cid
WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@n", EditProductName.Trim());
            cmd.Parameters.AddWithValue("@p", (double)price);
            cmd.Parameters.AddWithValue("@v", (double)vat);
            cmd.Parameters.AddWithValue("@cid", SelectedAdminCategory.Id);
            cmd.Parameters.AddWithValue("@id", SelectedAdminProduct.Id);
            cmd.ExecuteNonQuery();

            LoadAdminProducts();
            AdminStatusText = "Ürün güncellendi.";
        }

        private void BtnProductDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAdminProduct == null)
            {
                MessageBox.Show("Silmek için listeden bir ürün seç.");
                return;
            }

            var ok = MessageBox.Show(
                $"Silinsin mi?\n\n{SelectedAdminProduct.Name}",
                "Onay",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (ok != MessageBoxResult.Yes)
                return;

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Products WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", SelectedAdminProduct.Id);
            cmd.ExecuteNonQuery();

            LoadAdminProducts();
            BtnProductNew_Click(sender, e);
            AdminStatusText = "Ürün silindi.";
        }


        private void LoadProducts()
        {
            ListProducts.Items.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT p.Id, p.Name, p.Price, c.Name
FROM Products p
JOIN Categories c ON c.Id = p.CategoryId
ORDER BY c.Name, p.Name;";
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                var id = r.GetInt32(0);
                var name = r.GetString(1);
                var price = r.GetDecimal(2);
                var cat = r.GetString(3);
                ListProducts.Items.Add($"{cat} | {name} | {price:0.00} CHF (#{id})");
            }
        }

        private void LoadExtras()
        {
            ListExtras.Items.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT e.Id, e.Name, e.Price
FROM Extras e
ORDER BY e.Name;";
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                var id = r.GetInt32(0);
                var name = r.GetString(1);
                var price = r.GetDecimal(2);
                var priceText = price <= 0 ? "Ücretsiz" : $"{price:0.00} CHF";
                ListExtras.Items.Add($"{name} | {priceText} (#{id})");
            }
        }
        public ObservableCollection<Category> AdminCategories { get; } = new();
        public ObservableCollection<Product> AdminProducts { get; } = new();

        private Product? _selectedAdminProduct;
        public Product? SelectedAdminProduct
        {
            get => _selectedAdminProduct;
            set
            {
                _selectedAdminProduct = value;
                if (value != null)
                {
                    EditProductName = value.Name;
                    EditProductPrice = value.Price.ToString("0.00");
                    EditProductVat = value.Vat.ToString("0.0");
                    SelectedAdminCategory = AdminCategories.FirstOrDefault(c => c.Id == value.CategoryId);
                }
                OnPropertyChanged();
            }
        }

        private Category? _selectedAdminCategory;
        public Category? SelectedAdminCategory
        {
            get => _selectedAdminCategory;
            set { _selectedAdminCategory = value; OnPropertyChanged(); }
        }

        private string _editProductName = "";
        public string EditProductName { get => _editProductName; set { _editProductName = value; OnPropertyChanged(); } }

        private string _editProductPrice = "";
        public string EditProductPrice { get => _editProductPrice; set { _editProductPrice = value; OnPropertyChanged(); } }

        private string _editProductVat = "7.7";
        public string EditProductVat { get => _editProductVat; set { _editProductVat = value; OnPropertyChanged(); } }

        private string _adminStatusText = "";
        public string AdminStatusText { get => _adminStatusText; set { _adminStatusText = value; OnPropertyChanged(); } }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var name = (TxtCategory.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Kategori adı boş olamaz.");
                return;
            }

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Categories(Name) VALUES(@n);";
            cmd.Parameters.AddWithValue("@n", name);
            cmd.ExecuteNonQuery();

            TxtCategory.Text = "";
            LoadAll();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var name = (TxtName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Ürün adı boş olamaz.");
                return;
            }

            if (CmbCategory.SelectedItem is not ComboBoxItemEx cat)
            {
                MessageBox.Show("Kategori seçin.");
                return;
            }

            if (!TryParseDecimal(TxtPrice.Text, out var price) || price < 0)
            {
                MessageBox.Show("Fiyat hatalı. Örn: 12.50");
                return;
            }

            var vat = GetSelectedVat();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO Products(Name, Price, Vat, CategoryId) VALUES(@n, @p, @v, @cid);";
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@p", price);
            cmd.Parameters.AddWithValue("@v", vat);
            cmd.Parameters.AddWithValue("@cid", cat.Id);
            cmd.ExecuteNonQuery();

            TxtName.Text = "";
            TxtPrice.Text = "";
            LoadProducts();
        }

        private void AddExtra_Click(object sender, RoutedEventArgs e)
        {
            var name = (TxtExtraName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Ekstra adı boş olamaz.");
                return;
            }

            if (!TryParseDecimal(TxtExtraPrice.Text, out var price) || price < 0)
            {
                MessageBox.Show("Ekstra fiyat hatalı. Örn: 1.00 (Ücretsiz için 0 yaz)");
                return;
            }

            var selectedCats = ListExtraCategories.SelectedItems.Cast<ComboBoxItemEx>().ToList();
            if (selectedCats.Count == 0)
            {
                MessageBox.Show("En az 1 kategori seçmelisin (Ctrl ile çoklu seç).");
                return;
            }

            using var con = PosDb.Open();

            long extraId;
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Extras(Name, Price) VALUES(@n, @p);";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.ExecuteNonQuery();
            }

            using (var cmd2 = con.CreateCommand())
            {
                cmd2.CommandText = "SELECT last_insert_rowid();";
                extraId = (long)cmd2.ExecuteScalar()!;
            }

            foreach (var cat in selectedCats)
            {
                using var cmd3 = con.CreateCommand();
                cmd3.CommandText = "INSERT OR IGNORE INTO CategoryExtras(CategoryId, ExtraId) VALUES(@cid, @eid);";
                cmd3.Parameters.AddWithValue("@cid", cat.Id);
                cmd3.Parameters.AddWithValue("@eid", extraId);
                cmd3.ExecuteNonQuery();
            }

            TxtExtraName.Text = "";
            TxtExtraPrice.Text = "";
            LoadExtras();
        }

        private decimal GetSelectedVat()
        {
            var item = (CmbVat.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "7.7";
            return TryParseDecimal(item, out var v) ? v : 7.7m;
        }

        private static bool TryParseDecimal(string? s, out decimal value)
        {
            s = (s ?? "").Trim().Replace("'", "").Replace(" ", "");
            if (s.Contains(",") && !s.Contains(".")) s = s.Replace(",", ".");
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private class ComboBoxItemEx
        {
            public int Id { get; }
            public string Name { get; }
            public ComboBoxItemEx(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using WinPOS.Admin;
using WinPOS.Data;
using WinPOS.Helpers;
using WinPOS.Languages;
using WinPOS.Models;

namespace WinPOS.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ================= CLOCK =================
        private string _clockText = "--:--";
        public string ClockText
        {
            get => _clockText;
            set { _clockText = value; OnPropertyChanged(); }
        }

        private bool _dark;

        // ================= COLLECTIONS =================
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<DbProduct> Products { get; } = new();
        public ObservableCollection<CartItem> Cart { get; } = new();

        // ⬇️ ALT EKSTRA PANELİ
        public ObservableCollection<ExtraOption> CategoryExtras { get; } = new();

        // ================= CATEGORY =================
        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value) return;
                _selectedCategory = value;
                OnPropertyChanged();

                if (value != null)
                {
                    LoadProductsFromDb(value.Id);
                    LoadCategoryExtrasFromDb(value.Id);
                }
            }
        }

        // ================= CART =================
        private CartItem? _selectedCartItem;
        public CartItem? SelectedCartItem
        {
            get => _selectedCartItem;
            set { _selectedCartItem = value; OnPropertyChanged(); RaiseCartButtons(); }
        }

        public string TotalText => $"TOPLAM: {Cart.Sum(x => x.LineTotal):0.00} CHF";

        // ================= SALE TYPE (aktif satış) =================
        private string _saleType = "RESTAURANT";
        public string SaleType
        {
            get => _saleType;
            private set
            {
                _saleType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VatModeText));
            }
        }

        // MainWindow.xaml.cs veya ayarlardan çağrılabilir
        public void SetSaleType(string saleType)
        {
            if (string.IsNullOrWhiteSpace(saleType)) return;

            saleType = saleType.Trim().ToUpperInvariant();
            if (saleType != "RESTAURANT" && saleType != "TAKEAWAY") return;
            if (_saleType == saleType) return;

            SaleType = saleType;

            OnPropertyChanged(nameof(ActiveVatRate));
            OnPropertyChanged(nameof(VatModeText));
        }

        public decimal ActiveVatRate => SaleType == "TAKEAWAY" ? 2.6m : 7.7m;

        public string VatModeText =>
            SaleType == "TAKEAWAY"
                ? "Aktif KDV: %2.6 (TAKEAWAY)"
                : "Aktif KDV: %7.7 (RESTAURANT)";

        // ================= POS SETTINGS (Ayarlar penceresi için) =================
        public ObservableCollection<string> DefaultSaleTypes { get; } =
            new ObservableCollection<string> { "RESTAURANT", "TAKEAWAY" };

        private string _defaultSaleType = "RESTAURANT";
        public string DefaultSaleType
        {
            get => _defaultSaleType;
            set
            {
                _defaultSaleType = value;
                OnPropertyChanged();
                SettingsStore.Set("DefaultSaleType", value);
                SetSaleType(value); // anında uygula
            }
        }

        public class PrinterItem
        {
            public string Name { get; set; } = "";
            public override string ToString() => Name;
        }

        public ObservableCollection<PrinterItem> Printers { get; } = new ObservableCollection<PrinterItem>();

        private PrinterItem? _selectedReceiptPrinter;
        public PrinterItem? SelectedReceiptPrinter
        {
            get => _selectedReceiptPrinter;
            set
            {
                _selectedReceiptPrinter = value;
                OnPropertyChanged();
                if (value != null)
                    SettingsStore.Set("ReceiptPrinter", value.Name);
            }
        }

        private string _receiptHeader = "PIZZA UND KEBAB HAUS";
        public string ReceiptHeader
        {
            get => _receiptHeader;
            set
            {
                _receiptHeader = value;
                OnPropertyChanged();
                SettingsStore.Set("ReceiptHeader", value);
            }
        }

        // ================= LANGUAGE =================
        public class LanguageItem
        {
            public string Code { get; set; } = "";
            public string Display { get; set; } = "";
        }

        public ObservableCollection<LanguageItem> Languages { get; } = new();

        private LanguageItem? _selectedLanguage;
        public LanguageItem? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                if (value != null)
                    LanguageManager.SetLanguage(value.Code);
            }
        }

        // ================= COMMANDS =================
        public RelayCommand ToggleThemeCommand { get; }
        public RelayCommand OpenAdminCommand { get; }
        public RelayCommand AddProductCommand { get; }
        public RelayCommand IncreaseQtyCommand { get; }
        public RelayCommand DecreaseQtyCommand { get; }
        public RelayCommand RemoveItemCommand { get; }
        public RelayCommand PayCashCommand { get; }
        public RelayCommand PayCardCommand { get; }
        public RelayCommand ZReportCommand { get; }

        public RelayCommand SelectCategoryCommand { get; }
        public RelayCommand CategoryClickCommand { get; }
        public RelayCommand CategorySelectedCommand { get; }

        public RelayCommand SetLightThemeCommand { get; }
        public RelayCommand SetDarkThemeCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }

        private readonly DispatcherTimer _timer;

        // ================= CTOR =================
        public MainViewModel()
        {
            PosDb.Init();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => ClockText = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            _timer.Start();

            ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
            OpenAdminCommand = new RelayCommand(_ => OpenAdmin());
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());

            AddProductCommand = new RelayCommand(p => AddProduct((DbProduct)p!));

            IncreaseQtyCommand = new RelayCommand(_ => Plus(), _ => SelectedCartItem != null);
            DecreaseQtyCommand = new RelayCommand(_ => Minus(), _ => SelectedCartItem != null);
            RemoveItemCommand = new RelayCommand(_ => Remove(), _ => SelectedCartItem != null);

            PayCashCommand = new RelayCommand(_ => PayCash(), _ => Cart.Any());
            PayCardCommand = new RelayCommand(_ => PayCard(), _ => Cart.Any());

            ZReportCommand = new RelayCommand(_ => OpenZReport());

            SelectCategoryCommand = new RelayCommand(p => SelectCategory(p));
            CategoryClickCommand = new RelayCommand(p => SelectCategory(p));
            CategorySelectedCommand = new RelayCommand(p => SelectCategory(p));

            SetLightThemeCommand = new RelayCommand(_ => { _dark = false; ThemeManager.SetTheme("Light"); });
            SetDarkThemeCommand = new RelayCommand(_ => { _dark = true; ThemeManager.SetTheme("Dark"); });

            // ===== DİL LİSTESİ =====
            Languages.Clear();
            Languages.Add(new LanguageItem { Code = "de-CH", Display = "Deutsch (CH)" });
            Languages.Add(new LanguageItem { Code = "fr-CH", Display = "Français (CH)" });
            Languages.Add(new LanguageItem { Code = "en", Display = "English" });
            Languages.Add(new LanguageItem { Code = "tr", Display = "Türkçe" });
            SelectedLanguage = Languages.First(l => l.Code == "de-CH");

            // ===== POS AYARLARINI YÜKLE =====
            DefaultSaleType = SettingsStore.Get("DefaultSaleType", "RESTAURANT");
            ReceiptHeader = SettingsStore.Get("ReceiptHeader", "PIZZA UND KEBAB HAUS");

            Printers.Clear();
            foreach (string p in PrinterSettings.InstalledPrinters)
                Printers.Add(new PrinterItem { Name = p });

            var savedPrinter = SettingsStore.Get("ReceiptPrinter", "");
            SelectedReceiptPrinter = Printers.FirstOrDefault(x => x.Name == savedPrinter) ?? Printers.FirstOrDefault();

            // Varsayılan satış tipini uygula
            SetSaleType(DefaultSaleType);

            LoadCategoriesFromDb();
        }

        // ================= LOAD DATA =================
        private void LoadCategoriesFromDb()
        {
            Categories.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Categories ORDER BY Name;";
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                Categories.Add(new Category
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1)
                });
            }

            SelectedCategory = Categories.FirstOrDefault();
        }

        private void LoadProductsFromDb(int categoryId)
        {
            Products.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Price, Vat, CategoryId FROM Products WHERE CategoryId=@cid;";
            cmd.Parameters.AddWithValue("@cid", categoryId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Products.Add(new DbProduct
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Price = Convert.ToDecimal(r.GetDouble(2)),
                    Vat = Convert.ToDecimal(r.GetDouble(3)),
                    CategoryId = r.GetInt32(4)
                });
            }
        }

        private void LoadCategoryExtrasFromDb(int categoryId)
        {
            CategoryExtras.Clear();

            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT e.Id, e.Name, e.Price
FROM CategoryExtras ce
JOIN Extras e ON e.Id = ce.ExtraId
WHERE ce.CategoryId=@cid
ORDER BY e.Name;";
            cmd.Parameters.AddWithValue("@cid", categoryId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                CategoryExtras.Add(new ExtraOption
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Price = Convert.ToDecimal(r.GetDouble(2))
                });
            }
        }

        // ================= ADD PRODUCT =================
        private void AddProduct(DbProduct p)
        {
            var selectedExtras = CategoryExtras
                .Where(x => x.IsSelected)
                .Select(x => new CartExtra { Name = x.Name, Price = x.Price })
                .ToList();

            AddToCart(p.Name, p.Price, p.Vat, selectedExtras);

            foreach (var ex in CategoryExtras)
                ex.IsSelected = false;
        }

        private void AddToCart(string name, decimal price, decimal vatRate, List<CartExtra> extras)
        {
            var item = new CartItem
            {
                Name = name,
                Price = price,
                VatRate = vatRate,
                Qty = 1,
                Extras = extras
            };

            Cart.Add(item);
            SelectedCartItem = item;

            OnPropertyChanged(nameof(TotalText));
            PayCashCommand.RaiseCanExecuteChanged();
            PayCardCommand.RaiseCanExecuteChanged();
        }

        private void SelectCategory(object? p)
        {
            if (p is Category c)
            {
                SelectedCategory = c;
                return;
            }

            if (p is int id)
            {
                var found = Categories.FirstOrDefault(x => x.Id == id);
                if (found != null) SelectedCategory = found;
                return;
            }

            if (p is string s && int.TryParse(s, out var id2))
            {
                var found = Categories.FirstOrDefault(x => x.Id == id2);
                if (found != null) SelectedCategory = found;
                return;
            }
        }

        private void OpenSettings()
        {
            var w = new SettingsWindow
            {
                Owner = Application.Current.MainWindow
            };
            w.ShowDialog();
        }

        // ================= CART ACTIONS =================
        private void Plus()
        {
            SelectedCartItem!.Qty++;
            OnPropertyChanged(nameof(TotalText));
        }

        private void Minus()
        {
            SelectedCartItem!.Qty--;
            if (SelectedCartItem.Qty <= 0)
                Cart.Remove(SelectedCartItem);

            OnPropertyChanged(nameof(TotalText));
            PayCashCommand.RaiseCanExecuteChanged();
            PayCardCommand.RaiseCanExecuteChanged();
        }

        private void Remove()
        {
            Cart.Remove(SelectedCartItem!);
            SelectedCartItem = null;

            OnPropertyChanged(nameof(TotalText));
            PayCashCommand.RaiseCanExecuteChanged();
            PayCardCommand.RaiseCanExecuteChanged();
        }

        // ================= PAYMENT =================
        private void PayCash() => SaveSale("CASH");
        private void PayCard() => SaveSale("CARD");

        private void SaveSale(string paymentMethod)
        {
            if (!Cart.Any()) return;

            try
            {
                using var con = PosDb.Open();
                using var tx = con.BeginTransaction();

                decimal grandTotal = Cart.Sum(x => x.LineTotal);

                decimal vat26 = 0m;
                decimal vat77 = 0m;

                foreach (var item in Cart)
                {
                    var lineGross = item.LineTotal;
                    var rate = item.VatRate;
                    if (rate <= 0) continue;

                    var vatAmount = lineGross - (lineGross / (1m + (rate / 100m)));

                    if (Math.Abs(rate - 2.6m) < 0.001m) vat26 += vatAmount;
                    else if (Math.Abs(rate - 7.7m) < 0.001m) vat77 += vatAmount;
                }

                decimal subTotal = grandTotal - vat26 - vat77;

                long saleId;
                using (var cmd = con.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
INSERT INTO Sales (CreatedAt, PaymentMethod, SubTotal, Vat26, Vat77, GrandTotal, SaleType)
VALUES (@createdAt, @pm, @sub, @v26, @v77, @gt, @saleType);
SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@pm", paymentMethod);
                    cmd.Parameters.AddWithValue("@sub", (double)subTotal);
                    cmd.Parameters.AddWithValue("@v26", (double)vat26);
                    cmd.Parameters.AddWithValue("@v77", (double)vat77);
                    cmd.Parameters.AddWithValue("@gt", (double)grandTotal);
                    cmd.Parameters.AddWithValue("@saleType", SaleType);

                    saleId = (long)cmd.ExecuteScalar()!;
                }

                foreach (var item in Cart)
                {
                    var extrasText = item.Extras.Count == 0
                        ? ""
                        : string.Join(", ", item.Extras.Select(x => x.Price <= 0 ? x.Name : $"{x.Name}({x.Price:0.00})"));

                    var extrasTotal = item.Extras.Sum(x => x.Price) * item.Qty;

                    using var cmdItem = con.CreateCommand();
                    cmdItem.Transaction = tx;
                    cmdItem.CommandText = @"
INSERT INTO SaleItems (SaleId, Name, Qty, UnitPrice, ExtrasText, ExtrasTotal, LineTotal, VatRate)
VALUES (@sid, @name, @qty, @unit, @extrasText, @extrasTotal, @lineTotal, @vatRate);";
                    cmdItem.Parameters.AddWithValue("@sid", saleId);
                    cmdItem.Parameters.AddWithValue("@name", item.Name);
                    cmdItem.Parameters.AddWithValue("@qty", item.Qty);
                    cmdItem.Parameters.AddWithValue("@unit", (double)item.Price);
                    cmdItem.Parameters.AddWithValue("@extrasText", extrasText);
                    cmdItem.Parameters.AddWithValue("@extrasTotal", (double)extrasTotal);
                    cmdItem.Parameters.AddWithValue("@lineTotal", (double)item.LineTotal);
                    cmdItem.Parameters.AddWithValue("@vatRate", (double)item.VatRate);

                    cmdItem.ExecuteNonQuery();
                }

                tx.Commit();

                Cart.Clear();
                SelectedCartItem = null;

                OnPropertyChanged(nameof(TotalText));
                PayCashCommand.RaiseCanExecuteChanged();
                PayCardCommand.RaiseCanExecuteChanged();

                MessageBox.Show(paymentMethod == "CASH" ? "Nakit satış kaydedildi" : "Kart satış kaydedildi");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Satış kaydedilemedi:\n" + ex.Message);
            }
        }

        private void OpenZReport() => MessageBox.Show("Z Raporu (demo)");

        // ================= ADMIN / THEME =================
        private void ToggleTheme()
        {
            _dark = !_dark;
            ThemeManager.SetTheme(_dark ? "Dark" : "Light");
        }

        private void OpenAdmin()
        {
            var prevCategoryId = SelectedCategory?.Id;

            new AdminWindow().ShowDialog();

            LoadCategoriesFromDb();

            if (prevCategoryId != null)
            {
                var same = Categories.FirstOrDefault(c => c.Id == prevCategoryId.Value);
                if (same != null)
                    SelectedCategory = same;
            }

            if (SelectedCategory != null)
            {
                LoadProductsFromDb(SelectedCategory.Id);
                LoadCategoryExtrasFromDb(SelectedCategory.Id);
            }
        }

        private void RaiseCartButtons()
        {
            IncreaseQtyCommand.RaiseCanExecuteChanged();
            DecreaseQtyCommand.RaiseCanExecuteChanged();
            RemoveItemCommand.RaiseCanExecuteChanged();
        }

        // ================= MODELS =================
        public class CartExtra
        {
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
        }

        public class CartItem
        {
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public decimal VatRate { get; set; }
            public int Qty { get; set; }
            public List<CartExtra> Extras { get; set; } = new();

            public decimal LineTotal => (Price + Extras.Sum(x => x.Price)) * Qty;

            public override string ToString()
            {
                var extras = Extras.Count == 0
                    ? ""
                    : "\n" + string.Join("\n", Extras.Select(x => "+ " + x.Name));

                return $"{Name} x{Qty}\n{LineTotal:0.00} CHF{extras}";
            }
        }

        public class DbProduct
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public decimal Vat { get; set; }
            public int CategoryId { get; set; }
        }

        public class ExtraOption : INotifyPropertyChanged
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

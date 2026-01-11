using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WinPOS.Data;
using WinPOS.Helpers;
using System.Windows.Threading;


namespace WinPOS.ViewModels
{
    public class ZReportViewModel : INotifyPropertyChanged
    {
        public class SaleRow
        {
            public string Time { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public decimal GrandTotal { get; set; }
            public decimal Vat26 { get; set; }
            public decimal Vat77 { get; set; }

            public string GrandTotalText => $"{GrandTotal:0.00}";
            public string Vat26Text => $"{Vat26:0.00}";
            public string Vat77Text => $"{Vat77:0.00}";
        }

        public ObservableCollection<SaleRow> Sales { get; } = new();
        private bool _isXMode = false;

        private DateTime _day = DateTime.Today;
        public DateTime Day
        {
            get => _day;
            set { _day = value.Date; OnPropertyChanged(); Refresh(); }
        }

        private decimal _cash, _card, _grand, _v26, _v77;

        public string ReportTitle => $"{Day:dd.MM.yyyy}  •  Oluşturma: {DateTime.Now:HH:mm:ss}";
        public string CashText => $"{_cash:0.00} CHF";
        public string CardText => $"{_card:0.00} CHF";
        public string GrandText => $"{_grand:0.00} CHF";
        public string VatText => $"%2.6: {_v26:0.00} CHF   |   %7.7: {_v77:0.00} CHF";
        public string ModeTitle => _isXMode ? "X Raporu (Gün içi)" : "Z Raporu";

        public RelayCommand RefreshCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand PrintCommand { get; }
        public RelayCommand ReceiptPrintCommand { get; }
        public RelayCommand XReportCommand { get; }

        private readonly Action _close;

        public ZReportViewModel(Action close)
        {
            _close = close;

            RefreshCommand = new RelayCommand(_ => Refresh());
            CloseCommand = new RelayCommand(_ =>
            {
                _autoTimer?.Stop();

                _close();
            });

            PrintCommand = new RelayCommand(_ => Print());
            ReceiptPrintCommand = new RelayCommand(_ => PrintReceipt80mm());
            XReportCommand = new RelayCommand(_ => ShowXReport());
            _autoTimer = new DispatcherTimer();
            _autoTimer.Interval = TimeSpan.FromSeconds(10); // 10 saniyede bir yenile
            _autoTimer.Tick += (_, __) =>
            {
                if (_autoRefreshOn)
                    Refresh();
            };
            _autoTimer.Start();


            Refresh();
        }

        public void Refresh()
        {
            Sales.Clear();

            var dayKey = Day.ToString("yyyy-MM-dd");

            using var con = PosDb.Open();

            // Satır satır satışlar
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
SELECT CreatedAt, PaymentMethod, GrandTotal, Vat26, Vat77
FROM Sales
WHERE substr(CreatedAt,1,10)=@day
ORDER BY CreatedAt;";
                cmd.Parameters.AddWithValue("@day", dayKey);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var createdAt = r.GetString(0); // "yyyy-MM-dd HH:mm:ss"
                    Sales.Add(new SaleRow
                    {
                        Time = createdAt.Length >= 16 ? createdAt.Substring(11, 5) : createdAt,
                        PaymentMethod = r.GetString(1),
                        GrandTotal = Convert.ToDecimal(r.GetDouble(2)),
                        Vat26 = Convert.ToDecimal(r.GetDouble(3)),
                        Vat77 = Convert.ToDecimal(r.GetDouble(4)),
                    });
                }
            }

            // Özet
            using (var cmd2 = con.CreateCommand())
            {
                cmd2.CommandText = @"
SELECT
  IFNULL(SUM(CASE WHEN PaymentMethod='CASH' THEN GrandTotal END),0),
  IFNULL(SUM(CASE WHEN PaymentMethod='CARD' THEN GrandTotal END),0),
  IFNULL(SUM(GrandTotal),0),
  IFNULL(SUM(Vat26),0),
  IFNULL(SUM(Vat77),0)
FROM Sales
WHERE substr(CreatedAt,1,10)=@day;";
                cmd2.Parameters.AddWithValue("@day", dayKey);

                using var r2 = cmd2.ExecuteReader();
                r2.Read();
                _cash = Convert.ToDecimal(r2.GetDouble(0));
                _card = Convert.ToDecimal(r2.GetDouble(1));
                _grand = Convert.ToDecimal(r2.GetDouble(2));
                _v26 = Convert.ToDecimal(r2.GetDouble(3));
                _v77 = Convert.ToDecimal(r2.GetDouble(4));
            }

            OnPropertyChanged(nameof(ReportTitle));
            OnPropertyChanged(nameof(CashText));
            OnPropertyChanged(nameof(CardText));
            OnPropertyChanged(nameof(GrandText));
            OnPropertyChanged(nameof(VatText));
            OnPropertyChanged(nameof(ModeTitle));
            _lastRefresh = DateTime.Now;
            OnPropertyChanged(nameof(LastRefreshText));

        }

        private void ShowXReport()
        {
            _isXMode = true;
            _autoRefreshOn = true;   // X modunda otomatik yenile
            Refresh();
        }

        private void ShowZReport()
        {
            _isXMode = false;
            _autoRefreshOn = false;  // Z modunda otomatik yenile kapalı
            Refresh();
        }

        private readonly DispatcherTimer _autoTimer;
        private bool _autoRefreshOn = false;
        private DateTime _lastRefresh = DateTime.Now;
        public string LastRefreshText => $"Son güncelleme: {_lastRefresh:HH:mm:ss}";




        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Print()
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;

            var doc = new FlowDocument
            {
                PageWidth = dlg.PrintableAreaWidth,
                PageHeight = dlg.PrintableAreaHeight,
                PagePadding = new Thickness(40),
                ColumnWidth = dlg.PrintableAreaWidth
            };

            doc.Blocks.Add(new Paragraph(new Run("Z RAPORU"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            });

            doc.Blocks.Add(new Paragraph(new Run(ReportTitle))
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            });

            doc.Blocks.Add(new Paragraph(new Run(" ")));

            doc.Blocks.Add(new Paragraph(new Run($"NAKİT : {CashText}")));
            doc.Blocks.Add(new Paragraph(new Run($"KART  : {CardText}")));
            doc.Blocks.Add(new Paragraph(new Run($"TOPLAM: {GrandText}")));
            doc.Blocks.Add(new Paragraph(new Run(VatText)));
            doc.Blocks.Add(new Paragraph(new Run(" ")));

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });

            var header = new TableRow();
            header.Cells.Add(new TableCell(new Paragraph(new Run("Saat"))) { FontWeight = FontWeights.Bold });
            header.Cells.Add(new TableCell(new Paragraph(new Run("Ödeme"))) { FontWeight = FontWeights.Bold });
            header.Cells.Add(new TableCell(new Paragraph(new Run("Tutar"))) { FontWeight = FontWeights.Bold });

            var headerGroup = new TableRowGroup();
            headerGroup.Rows.Add(header);
            table.RowGroups.Add(headerGroup);

            var bodyGroup = new TableRowGroup();
            foreach (var s in Sales)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(s.Time))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(s.PaymentMethod))));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{s.GrandTotal:0.00} CHF"))));
                bodyGroup.Rows.Add(row);
            }

            table.RowGroups.Add(bodyGroup);
            doc.Blocks.Add(table);

            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Z Raporu");
        }

        private void PrintReceipt80mm()
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;

            double targetWidth = 302;
            var pageWidth = Math.Min(dlg.PrintableAreaWidth, targetWidth);

            var doc = new FlowDocument
            {
                PageWidth = pageWidth,
                ColumnWidth = pageWidth,
                PagePadding = new Thickness(10),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 10
            };

            void CenterLine(string text, bool bold = false)
            {
                var p = new Paragraph(new Run(text))
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0)
                };
                if (bold) p.FontWeight = FontWeights.Bold;
                doc.Blocks.Add(p);
            }

            void LeftLine(string text)
            {
                doc.Blocks.Add(new Paragraph(new Run(text)) { Margin = new Thickness(0) });
            }

            void Hr() => LeftLine(new string('-', 32));

            CenterLine("PIZZA UND KEBAB HAUS", bold: true);
            CenterLine(_isXMode ? "X RAPORU" : "Z RAPORU", bold: true);
            CenterLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            Hr();

            LeftLine($"NAKIT : {CashText}");
            LeftLine($"KART  : {CardText}");
            LeftLine($"TOPLAM: {GrandText}");
            LeftLine(VatText.Replace("   |   ", "  "));
            Hr();

            LeftLine("Saat  Odeme  Tutar");
            Hr();

            foreach (var s in Sales)
            {
                var t = (s.Time ?? "").PadRight(5);
                var pm = (s.PaymentMethod ?? "").PadRight(5);
                var amt = $"{s.GrandTotal:0.00}".PadLeft(8);
                LeftLine($"{t}  {pm}  {amt}");
            }

            Hr();
            CenterLine("Tesekkurler");

            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, _isXMode ? "X Raporu (80mm)" : "Z Raporu (80mm)");
        }
    }
}

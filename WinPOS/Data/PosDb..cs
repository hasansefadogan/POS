using Microsoft.Data.Sqlite;
using System;
using System.IO;


namespace WinPOS.Data
{
    public static class PosDb
    {
        private static readonly string DbPath =
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "winpos.db");


        public static void Init()
        {
            using var con = new SqliteConnection($"Data Source={DbPath}");
            con.Open();

            using var cmd = con.CreateCommand();

            // =========================
            // TABLOLAR
            // =========================

            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Sales (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    PaymentMethod TEXT NOT NULL,
    SubTotal REAL NOT NULL,
    Vat26 REAL NOT NULL,
    Vat77 REAL NOT NULL,
    GrandTotal REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS SaleItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Qty INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    ExtrasText TEXT NOT NULL,
    ExtrasTotal REAL NOT NULL,
    LineTotal REAL NOT NULL,
    VatRate REAL NOT NULL,
    FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ZReports (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ZDate TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    CashTotal REAL NOT NULL,
    CardTotal REAL NOT NULL,
    GrandTotal REAL NOT NULL,
    Vat26 REAL NOT NULL,
    Vat77 REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS Categories(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Products(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Price REAL NOT NULL,
    Vat REAL NOT NULL DEFAULT 7.7,
    CategoryId INTEGER NOT NULL,
    FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE IF NOT EXISTS Extras(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Price REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS CategoryExtras(
    CategoryId INTEGER NOT NULL,
    ExtraId INTEGER NOT NULL,
    PRIMARY KEY(CategoryId, ExtraId),
    FOREIGN KEY(CategoryId) REFERENCES Categories(Id),
    FOREIGN KEY(ExtraId) REFERENCES Extras(Id)
);
";
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);";
            cmd.ExecuteNonQuery();

            // Sales tablosunda SaleType kolonu yoksa ekle (eski DB'ler için)
            using (var cmdInfo = con.CreateCommand())
            {
                cmdInfo.CommandText = "PRAGMA table_info(Sales);";
                using var r = cmdInfo.ExecuteReader();

                bool hasSaleType = false;
                while (r.Read())
                {
                    var colName = r.GetString(1);
                    if (string.Equals(colName, "SaleType", StringComparison.OrdinalIgnoreCase))
                    {
                        hasSaleType = true;
                        break;
                    }
                }

                if (!hasSaleType)
                {
                    using var alter = con.CreateCommand();
                    alter.CommandText = "ALTER TABLE Sales ADD COLUMN SaleType TEXT NOT NULL DEFAULT 'RESTAURANT';";
                    alter.ExecuteNonQuery();
                }
            }// ✅ Eski DB'ler için: Sales tablosunda SaleType kolonu yoksa ekle
            using (var cmdInfo = con.CreateCommand())
            {
                cmdInfo.CommandText = "PRAGMA table_info(Sales);";
                using var r = cmdInfo.ExecuteReader();

                bool hasSaleType = false;
                while (r.Read())
                {
                    var colName = r.GetString(1); // column name
                    if (string.Equals(colName, "SaleType", StringComparison.OrdinalIgnoreCase))
                    {
                        hasSaleType = true;
                        break;
                    }
                }

                if (!hasSaleType)
                {
                    using var alter = con.CreateCommand();
                    alter.CommandText = "ALTER TABLE Sales ADD COLUMN SaleType TEXT NOT NULL DEFAULT 'RESTAURANT';";
                    alter.ExecuteNonQuery();
                }
            }



            // =========================
            // INDEXLER
            // =========================
            cmd.CommandText = @"CREATE INDEX IF NOT EXISTS IX_Products_CategoryId ON Products(CategoryId);";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE INDEX IF NOT EXISTS IX_Sales_CreatedAt ON Sales(CreatedAt);";
            cmd.ExecuteNonQuery();

            // =========================
            // DEMO VERİ (SADECE BOŞSA)
            // =========================
            cmd.CommandText = "SELECT COUNT(*) FROM Categories;";
            var catCount = Convert.ToInt32(cmd.ExecuteScalar());

            if (catCount == 0)
            {
                cmd.CommandText = @"
INSERT INTO Categories (Id, Name) VALUES
(1,'Pizza'),
(2,'Kebab'),
(3,'Getränke');

INSERT INTO Products (Name, Price, Vat, CategoryId) VALUES
('Margherita', 12.50, 7.7, 1),
('Salami', 14.00, 7.7, 1),
('Döner Box', 14.00, 7.7, 2),
('Cola', 3.50, 7.7, 3);
";
                cmd.ExecuteNonQuery();
            }
        }

        public static SqliteConnection Open()
        {
            var con = new SqliteConnection($"Data Source={DbPath}");
            con.Open();
            return con;
        }
    }
}

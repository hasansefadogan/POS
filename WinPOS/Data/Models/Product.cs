namespace WinPOS.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public decimal Vat { get; set; } // 2.6 veya 7.7
        public int CategoryId { get; set; }
        public override string ToString() => $"{Name} ({Price:0.00} CHF, KDV %{Vat})";
    }
}

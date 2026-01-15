namespace WinPOS.Models;

public class CartExtra
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class CartItem
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; } = 1;
    public List<CartExtra> Extras { get; set; } = new();

    public decimal ExtrasTotal => Extras.Sum(x => x.Price);
    public decimal LineTotal => (Price + ExtrasTotal) * Qty;

    public override string ToString()
    {
        var extraText = Extras.Count == 0 ? "" :
            " + " + string.Join(", ", Extras.Select(x =>
                x.Price <= 0 ? x.Name : $"{x.Name}({x.Price:0.00})"));

        return $"{Name}{extraText}  x{Qty}  =  {LineTotal:0.00} CHF";
    }
}

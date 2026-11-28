namespace PawCare.ViewModels;

// one row shown on the cart page
public class CartItemVM
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; }
}

// S001:1,P001:3 exp
public class CartLine
{
    public string Id { get; set; } = "";
    public int Qty { get; set; }
}
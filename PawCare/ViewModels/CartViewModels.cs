namespace PawCare.ViewModels;

public class CartItemVM
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; }
}
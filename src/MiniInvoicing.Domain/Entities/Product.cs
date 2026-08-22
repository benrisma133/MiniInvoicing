namespace MiniInvoicing.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    public Product(string name, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.");

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.");

        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to deduct must be greater than zero.");

        if (quantity > StockQuantity)
            throw new InvalidOperationException($"Insufficient stock for '{Name}'. Available: {StockQuantity}.");

        StockQuantity -= quantity;
    }

    public void UpdateDetails(string name, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.");

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.");

        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
    }
}
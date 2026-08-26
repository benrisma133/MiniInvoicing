using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Domain.Entities;

public partial class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public Product() { }

    public Product(string name, decimal price, int stockQuantity)
    {
        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public static enProductOperationResult Validate(string name, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return enProductOperationResult.InvalidName;

        if (price <= 0)
            return enProductOperationResult.InvalidPrice;

        if (stockQuantity < 0)
            return enProductOperationResult.InvalidStockQuantity;

        return enProductOperationResult.Success;
    }

    public enProductOperationResult UpdateDetails(string name, decimal price, int stockQuantity)
    {
        var validation = Validate(name, price, stockQuantity);
        if (validation != enProductOperationResult.Success)
            return validation;

        Name = name;
        Price = price;
        StockQuantity = stockQuantity;

        return enProductOperationResult.Success;
    }

    public enProductOperationResult DeductStock(int quantity)
    {
        if (quantity <= 0)
            return enProductOperationResult.InvalidDeductQuantity;

        if (StockQuantity < quantity)
            return enProductOperationResult.InsufficientStock;

        StockQuantity -= quantity;

        return enProductOperationResult.Success;
    }
}
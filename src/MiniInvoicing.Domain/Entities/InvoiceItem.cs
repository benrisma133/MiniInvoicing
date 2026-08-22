namespace MiniInvoicing.Domain.Entities;

public partial class InvoiceItem
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;

    public InvoiceItem() { }

    public InvoiceItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Invalid Product Id.");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");
        if (unitPrice <= 0)
            throw new ArgumentException("Unit price must be greater than zero.");

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = quantity * unitPrice;
    }
}
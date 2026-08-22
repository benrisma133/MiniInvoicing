namespace MiniInvoicing.Domain.Entities;

public class InvoiceItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

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
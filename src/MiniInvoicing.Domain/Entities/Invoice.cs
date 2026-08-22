namespace MiniInvoicing.Domain.Entities;

public class Invoice
{
    private readonly List<InvoiceItem> _items = new();

    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalWithVat { get; private set; }

    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

    public Invoice(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required.");

        Id = Guid.NewGuid();
        InvoiceNumber = invoiceNumber;
        IssueDate = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice, decimal vatRate = 0.20m)
    {
        var item = new InvoiceItem(productId, quantity, unitPrice);
        _items.Add(item);

        RecalculateTotals(vatRate);
    }

    private void RecalculateTotals(decimal vatRate)
    {
        TotalAmount = _items.Sum(x => x.LineTotal);
        VatAmount = TotalAmount * vatRate;
        TotalWithVat = TotalAmount + VatAmount;
    }
}
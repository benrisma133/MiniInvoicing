namespace MiniInvoicing.Domain.Entities;

public partial class Invoice
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalWithVat { get; set; }

    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    public Invoice() { }

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
        var item = new InvoiceItem(productId, quantity, unitPrice)
        {
            InvoiceId = this.Id
        };
        Items.Add(item);

        RecalculateTotals(vatRate);
    }

    private void RecalculateTotals(decimal vatRate)
    {
        TotalAmount = Items.Sum(x => x.LineTotal);
        VatAmount = TotalAmount * vatRate;
        TotalWithVat = TotalAmount + VatAmount;
    }
}
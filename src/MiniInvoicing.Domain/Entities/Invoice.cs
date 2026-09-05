using MiniInvoicing.Domain.Enums.Invoice;

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
        Id = Guid.NewGuid();
        InvoiceNumber = invoiceNumber;
        IssueDate = DateTime.UtcNow;
    }

    public static enInvoiceOperationResult Validate(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return enInvoiceOperationResult.InvalidInvoiceNumber;

        return enInvoiceOperationResult.Success;
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

    public void RemoveItem(Guid itemId, decimal vatRate = 0.20m)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            Items.Remove(item);
            RecalculateTotals(vatRate);
        }
    }

    public void UpdateItemDetails(Guid itemId, int newQuantity, decimal newUnitPrice, decimal vatRate = 0.20m)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.Quantity = newQuantity;
            item.UnitPrice = newUnitPrice;
            item.LineTotal = newQuantity * newUnitPrice;

            RecalculateTotals(vatRate);
        }
    }

    public void RecalculateTotals(decimal vatRate = 0.20m)
    {
        TotalAmount = Items.Sum(x => x.LineTotal);
        VatAmount = TotalAmount * vatRate;
        TotalWithVat = TotalAmount + VatAmount;
    }
}
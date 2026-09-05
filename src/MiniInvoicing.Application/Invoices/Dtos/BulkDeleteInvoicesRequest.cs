namespace MiniInvoicing.Application.Invoices.Dtos
{
    public record BulkDeleteInvoicesRequest(List<Guid> InvoiceIds);
}

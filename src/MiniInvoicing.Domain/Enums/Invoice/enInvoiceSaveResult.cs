namespace MiniInvoicing.Domain.Enums.Invoice;

public enum enInvoiceSaveResult
{
    Saved,
    EmptyItems,
    InsufficientStock,
    ProductNotFound,
    InvalidAmount,
    Failed
}
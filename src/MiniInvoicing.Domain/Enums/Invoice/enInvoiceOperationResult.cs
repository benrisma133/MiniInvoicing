namespace MiniInvoicing.Domain.Enums.Invoice;

public enum enInvoiceOperationResult
{
    Success,
    EmptyItems,
    InvalidInvoiceNumber,
    ProductNotFound,
    InsufficientStock,
    DuplicateInvoiceNumber,
    NotFound,
    Failed,
    InvalidOperation
}
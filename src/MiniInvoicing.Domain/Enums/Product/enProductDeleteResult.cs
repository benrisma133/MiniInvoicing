namespace MiniInvoicing.Domain.Enums.Product;

public enum enProductDeleteResult
{
    Deleted,
    NotFound,
    HasAssociatedInvoices,
    Failed
}
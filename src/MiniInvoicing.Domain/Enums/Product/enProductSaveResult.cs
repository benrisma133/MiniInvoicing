namespace MiniInvoicing.Domain.Enums.Product;

public enum enProductSaveResult
{
    Saved,
    DuplicateName,
    InvalidPrice,
    InvalidStock,
    NotFound,
    Failed
}
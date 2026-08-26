namespace MiniInvoicing.Domain.Enums.Product;

public enum enProductOperationResult
{
    Success,
    InvalidName,
    InvalidPrice,
    InvalidStockQuantity,
    InvalidDeductQuantity,
    InsufficientStock,
    DuplicateName,
    NotFound,
    Failed
}
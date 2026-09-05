namespace MiniInvoicing.Domain.Enums.Product;

public enum enProductOperationResult
{
    Success,
    InvalidName,
    InvalidPrice,
    InvalidStockQuantity,
    InvalidDeductQuantity,
    InvalidRestoreQuantity,
    InsufficientStock,
    DuplicateName,
    NotFound,
    Failed
}
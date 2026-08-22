namespace MiniInvoicing.Application.Products.Dtos;

public record UpdateProductDto(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity
);
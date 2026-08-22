namespace MiniInvoicing.Application.Products.Dtos;

public record CreateProductDto(
    string Name,
    decimal Price,
    int StockQuantity
);
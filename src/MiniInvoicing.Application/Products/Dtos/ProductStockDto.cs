namespace MiniInvoicing.Application.DTOs;

public record ProductStockDto(
    Guid Id,
    string Name,
    int StockQuantity
);
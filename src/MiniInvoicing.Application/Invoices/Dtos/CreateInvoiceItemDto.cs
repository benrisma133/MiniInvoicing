namespace MiniInvoicing.Application.Invoices.Dtos;

public record CreateInvoiceItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);
namespace MiniInvoicing.Application.Invoices.Dtos;

public record InvoiceItemDto(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
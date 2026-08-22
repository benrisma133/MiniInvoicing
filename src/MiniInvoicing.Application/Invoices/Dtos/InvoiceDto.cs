namespace MiniInvoicing.Application.Invoices.Dtos;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    DateTime IssueDate,
    decimal TotalAmount,
    decimal VatAmount,
    decimal TotalWithVat,
    List<InvoiceItemDto> Items
);
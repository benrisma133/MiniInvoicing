namespace MiniInvoicing.Application.Invoices.Dtos;

public record CreateInvoiceDto(
    string InvoiceNumber,
    List<CreateInvoiceItemDto> Items
);
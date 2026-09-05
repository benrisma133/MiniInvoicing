namespace MiniInvoicing.Application.Invoices.Dtos;

public record UpdateInvoiceItemDto(
    Guid? Id, // Nullable: إذا كان عنصر جديد سينضاف للفاتورة
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);

public record UpdateInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    List<UpdateInvoiceItemDto> Items
);
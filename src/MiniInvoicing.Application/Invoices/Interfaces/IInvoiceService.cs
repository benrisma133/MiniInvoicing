using MiniInvoicing.Application.Invoices.Dtos;
using MiniInvoicing.Domain.Enums.Invoice;

namespace MiniInvoicing.Application.Invoices.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<enInvoiceSaveResult> CreateAsync(CreateInvoiceDto dto);
}
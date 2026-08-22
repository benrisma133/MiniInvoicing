using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Invoice;

namespace MiniInvoicing.Application.Common.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<enInvoiceSaveResult> AddAsync(Invoice invoice);
}
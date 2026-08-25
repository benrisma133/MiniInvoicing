using MiniInvoicing.Domain.Entities;

namespace MiniInvoicing.Application.Common.Interfaces;

public interface IInvoiceRepository
{
    Task CreateInvoiceWithStockUpdateAsync(
        Invoice invoice,
        Dictionary<Guid, int> requestedItems,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken cancellationToken = default);
}
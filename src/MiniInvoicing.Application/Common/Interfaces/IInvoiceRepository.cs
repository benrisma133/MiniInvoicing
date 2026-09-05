using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Invoice;

namespace MiniInvoicing.Application.Common.Interfaces;

public interface IInvoiceRepository
{

    Task<Invoice?> GetByIdWithItemsAndProductsAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetByIdWithItemsAsync(
        Guid id, 
        bool asNoTracking = true, 
        CancellationToken cancellationToken = default);

    Task<List<Invoice>> GetByIdsWithItemsAsync(
        List<Guid> ids,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNumberAsync(
        string invoiceNumber, 
        CancellationToken cancellationToken = default);
    Task<enInvoiceOperationResult> CreateInvoiceWithStockUpdateAsync(
        Invoice invoice, 
        Dictionary<Guid, int> requestedItems, 
        CancellationToken cancellationToken = default);

    Task<enInvoiceOperationResult> CreateInvoicesRangeWithStockUpdateAsync(
        List<Invoice> invoices,
        Dictionary<Guid, int> aggregatedRequestedItems,
        CancellationToken cancellationToken = default);

    Task<enInvoiceOperationResult> UpdateInvoiceWithStockUpdateAsync(
        Invoice invoice, 
        Dictionary<Guid, int> stockAdjustments, 
        List<InvoiceItem> removedItems, 
        List<InvoiceItem> addedItems, 
        CancellationToken cancellationToken = default);

    Task<enInvoiceOperationResult> UpdateInvoicesRangeWithStockUpdateAsync(
        List<Invoice> invoices,
        Dictionary<Guid, int> aggregatedStockAdjustments,
        List<InvoiceItem> removedItems,
        List<InvoiceItem> addedItems,
        CancellationToken cancellationToken = default);

    Task<enInvoiceDeleteResult> DeleteWithStockRestoreAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // 👈 حذف مجموعة فواتير دفعة واحدة مع إرجاع المخزون التجميعي
    Task<enInvoiceDeleteResult> DeleteRangeWithStockRestoreAsync(
        List<Guid> ids,
        CancellationToken cancellationToken = default);
}
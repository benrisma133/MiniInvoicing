using Microsoft.EntityFrameworkCore;
using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Invoice;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdWithItemsAndProductsAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    // للفاتورة الفردية فـ الـ UpdateAsync
    public async Task<Invoice?> GetByIdWithItemsAsync(
        Guid id,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .Include(i => i.Items)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    // للفواتير المتعددة فـ الـ UpdateRangeAsync
    public async Task<List<Invoice>> GetByIdsWithItemsAsync(
        List<Guid> ids,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .Include(i => i.Items)
            .Where(i => ids.Contains(i.Id));

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<enInvoiceOperationResult> CreateInvoiceWithStockUpdateAsync(
        Invoice invoice,
        Dictionary<Guid, int> requestedItems,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var productIds = requestedItems.Keys.ToList();

            var productsToUpdate = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var (productId, requestedQty) in requestedItems)
            {
                var product = productsToUpdate.FirstOrDefault(p => p.Id == productId);

                if (product == null)
                    return enInvoiceOperationResult.ProductNotFound;

                var deductResult = product.DeductStock(requestedQty);
                if (deductResult != enProductOperationResult.Success)
                {
                    return deductResult switch
                    {
                        enProductOperationResult.InsufficientStock => enInvoiceOperationResult.InsufficientStock,
                        _ => enInvoiceOperationResult.Failed
                    };
                }
            }

            await _context.Invoices.AddAsync(invoice, cancellationToken);

            var affectedRows = await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return affectedRows > 0 ? enInvoiceOperationResult.Success : enInvoiceOperationResult.Failed;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return enInvoiceOperationResult.Failed;
        }
    }

    public async Task<enInvoiceOperationResult> CreateInvoicesRangeWithStockUpdateAsync(
        List<Invoice> invoices,
        Dictionary<Guid, int> aggregatedRequestedItems,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. جلب كاع المنتجات المعنية فـ الفواتير دفعة واحدة
            var productIds = aggregatedRequestedItems.Keys.ToList();

            var productsToUpdate = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // 2. التحقق والخصم التجميعي من المخزون لكاع المنتجات
            foreach (var (productId, requestedQty) in aggregatedRequestedItems)
            {
                var product = productsToUpdate.FirstOrDefault(p => p.Id == productId);

                if (product == null)
                    return enInvoiceOperationResult.ProductNotFound;

                var deductResult = product.DeductStock(requestedQty);
                if (deductResult != enProductOperationResult.Success)
                {
                    return deductResult switch
                    {
                        enProductOperationResult.InsufficientStock => enInvoiceOperationResult.InsufficientStock,
                        _ => enInvoiceOperationResult.Failed
                    };
                }
            }

            // 3. إضافة قائمة الفواتير بالكامل فـ الـ DbContext
            await _context.Invoices.AddRangeAsync(invoices, cancellationToken);

            // 4. حفظ التغييرات وتأكيد العملية (Atomic Commit)
            var affectedRows = await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return affectedRows > 0 ? enInvoiceOperationResult.Success : enInvoiceOperationResult.Failed;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return enInvoiceOperationResult.Failed;
        }
    }

    public async Task<enInvoiceOperationResult> UpdateInvoiceWithStockUpdateAsync(
        Invoice invoice,
        Dictionary<Guid, int> stockAdjustments,
        List<InvoiceItem> removedItems,
        List<InvoiceItem> addedItems, // 👈 1. استقبال قائمة العناصر المضافة
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 2. مسح العناصر المحذوفة صراحة
            if (removedItems != null && removedItems.Any())
            {
                _context.InvoiceItems.RemoveRange(removedItems);
            }

            // 👈 3. إضافة العناصر الجدد صراحة فـ الـ DbContext (تتبع كـ INSERT)
            if (addedItems != null && addedItems.Any())
            {
                _context.InvoiceItems.AddRange(addedItems);
            }

            // 4. Fetch Products & Apply Stock Adjustments
            var productIds = stockAdjustments.Keys.ToList();
            var productsToUpdate = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var (productId, adjustmentQty) in stockAdjustments)
            {
                var product = productsToUpdate.FirstOrDefault(p => p.Id == productId);
                if (product == null)
                    return enInvoiceOperationResult.ProductNotFound;

                if (adjustmentQty > 0)
                {
                    var deductResult = product.DeductStock(adjustmentQty);
                    if (deductResult != enProductOperationResult.Success)
                        return enInvoiceOperationResult.InsufficientStock;
                }
                else if (adjustmentQty < 0)
                {
                    var restoreResult = product.RestoreStock(Math.Abs(adjustmentQty));
                    if (restoreResult != enProductOperationResult.Success)
                        return enInvoiceOperationResult.InvalidOperation;
                }
            }

            // 5. Save Changes
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return enInvoiceOperationResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return enInvoiceOperationResult.Failed;
        }
    }

    public async Task<enInvoiceOperationResult> UpdateInvoicesRangeWithStockUpdateAsync(
        List<Invoice> invoices,
        Dictionary<Guid, int> aggregatedStockAdjustments,
        List<InvoiceItem> removedItems,
        List<InvoiceItem> addedItems,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. مسح العناصر المحذوفة صراحة عبر كاع الفواتير
            if (removedItems != null && removedItems.Any())
            {
                _context.InvoiceItems.RemoveRange(removedItems);
            }

            // 2. إضافة العناصر الجديدة صراحة عبر كاع الفواتير (INSERT)
            if (addedItems != null && addedItems.Any())
            {
                _context.InvoiceItems.AddRange(addedItems);
            }

            // 3. جلب وتحديث المخزون التجميعي
            var productIds = aggregatedStockAdjustments.Keys.ToList();
            var productsToUpdate = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var (productId, adjustmentQty) in aggregatedStockAdjustments)
            {
                if (adjustmentQty == 0) continue;

                var product = productsToUpdate.FirstOrDefault(p => p.Id == productId);
                if (product == null)
                    return enInvoiceOperationResult.ProductNotFound;

                if (adjustmentQty > 0)
                {
                    var deductResult = product.DeductStock(adjustmentQty);
                    if (deductResult != enProductOperationResult.Success)
                        return enInvoiceOperationResult.InsufficientStock;
                }
                else if (adjustmentQty < 0)
                {
                    var restoreResult = product.RestoreStock(Math.Abs(adjustmentQty));
                    if (restoreResult != enProductOperationResult.Success)
                        return enInvoiceOperationResult.InvalidOperation;
                }
            }

            // 4. Save & Commit
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return enInvoiceOperationResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return enInvoiceOperationResult.Failed;
        }
    }


    public async Task<enInvoiceDeleteResult> DeleteWithStockRestoreAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. جلب الفاتورة وعناصرها فقط لمعرفة الكميات والمنتجات الواجب إرجاعها للمخزون
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (invoice == null)
                return enInvoiceDeleteResult.NotFound;

            // 2. تجميع الكميات المبيعة لإرجاعها للمخزون
            var stockToRestore = invoice.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            // 3. جلب المنتجات وإرجاع المخزون
            var productIds = stockToRestore.Keys.ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var (productId, qtyToRestore) in stockToRestore)
            {
                var product = products.FirstOrDefault(p => p.Id == productId);
                product?.RestoreStock(qtyToRestore);
            }

            // 4. حذف الفاتورة فقط (Cascade Delete سيمسح العناصر أوتوماتيكياً فـ SQL)
            _context.Invoices.Remove(invoice);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return enInvoiceDeleteResult.Deleted;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return enInvoiceDeleteResult.Failed;
        }
    }

    public async Task<enInvoiceDeleteResult> DeleteRangeWithStockRestoreAsync(
        List<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. جلب الفواتير وعناصرها للتعرف على الكميات
            var invoices = await _context.Invoices
                .Include(i => i.Items)
                .Where(i => ids.Contains(i.Id))
                .ToListAsync(cancellationToken);

            if (invoices.Count != ids.Distinct().Count())
                return enInvoiceDeleteResult.NotFound;

            // 2. تجميع الكميات عبر كافة الفواتير
            var aggregatedStockToRestore = invoices
                .SelectMany(i => i.Items)
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            // 3. تحديث المخزون للمنتجات
            var productIds = aggregatedStockToRestore.Keys.ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var (productId, qtyToRestore) in aggregatedStockToRestore)
            {
                var product = products.FirstOrDefault(p => p.Id == productId);
                product?.RestoreStock(qtyToRestore);
            }

            // 4. حذف الفواتير دفعة واحدة (Cascade Delete يتكفل بالعناصر)
            _context.Invoices.RemoveRange(invoices);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return enInvoiceDeleteResult.Deleted;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return enInvoiceDeleteResult.Failed;
        }
    }
}
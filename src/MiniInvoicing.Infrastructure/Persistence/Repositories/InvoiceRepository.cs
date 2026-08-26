using Microsoft.EntityFrameworkCore;
using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateInvoiceWithStockUpdateAsync(
    Invoice invoice,
    Dictionary<Guid, int> requestedItems,
    CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var productIds = requestedItems.Keys.ToList();

            // 1. Fetch tracked entities directly (Single DB query)
            var productsToUpdate = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // 2. Validate existence and stock quantities directly on tracked entities
            var validationErrors = new List<string>();

            foreach (var (productId, requestedQty) in requestedItems)
            {
                var product = productsToUpdate.FirstOrDefault(p => p.Id == productId);

                if (product == null)
                {
                    validationErrors.Add($"Product with ID '{productId}' was not found.");
                }
                else if (product.StockQuantity < requestedQty)
                {
                    validationErrors.Add(
                        $"Product '{product.Name}' has insufficient stock. Requested: {requestedQty}, Available: {product.StockQuantity}.");
                }
            }

            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(" | ", validationErrors));
            }

            // 3. Deduct stock (EF Core Change Tracker records modifications automatically)
            // 3. Deduct stock using the returned ProductValidationResult
            foreach (var product in productsToUpdate)
            {
                int qty = requestedItems[product.Id];

                var result = product.DeductStock(qty);
                if (result != enProductOperationResult.Success)
                {
                    // Handle failure if needed, or throw to rollback the transaction
                    throw new InvalidOperationException($"Failed to deduct stock for '{product.Name}': {result}");
                }
            }

            // 4. Add invoice entity
            await _context.Invoices.AddAsync(invoice, cancellationToken);

            // 5. Commit both Invoice INSERT and Product UPDATE statements in one atomic transaction
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Invoice?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .ThenInclude(item => item.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
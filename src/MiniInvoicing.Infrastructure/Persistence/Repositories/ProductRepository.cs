using Microsoft.EntityFrameworkCore;
using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Infrastructure.Persistence.Repositories
{
    internal class ProductRepository : IProductRepository
    {
        readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<enProductSaveResult> AddAsync(Product product)
        {
            if (await ExistsByNameAsync(product.Name))
                return enProductSaveResult.DuplicateName;

            await _context.Products.AddAsync(product);

            var affectedRow = await _context.SaveChangesAsync();

            return affectedRow > 0 ? enProductSaveResult.Saved : enProductSaveResult.Failed;

        }

        public async Task<enProductDeleteResult> DeleteAsync(Guid id)
        {
            var product = await GetByIdAsync(id);
            if (product == null)
                return enProductDeleteResult.NotFound;

            _context.Products.Remove(product);
            var affectedRows = await _context.SaveChangesAsync();

            return affectedRows > 0 ? enProductDeleteResult.Deleted : enProductDeleteResult.Failed;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Products.AnyAsync(p => p.Name == name);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
           return await _context.Products.AsNoTracking().ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<enProductSaveResult> UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            var affectedRows = await _context.SaveChangesAsync();

            return affectedRows > 0 ? enProductSaveResult.Saved : enProductSaveResult.Failed;
        }
    }
}

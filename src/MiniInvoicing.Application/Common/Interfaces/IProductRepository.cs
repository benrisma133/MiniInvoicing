using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<bool> ExistsByNameAsync(string name);
    Task<enProductSaveResult> AddAsync(Product product);
    Task<enProductSaveResult> UpdateAsync(Product product);
    Task<enProductDeleteResult> DeleteAsync(Guid id);
}
using MiniInvoicing.Application.Products.Dtos;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Application.Products.Interfaces;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<enProductSaveResult> CreateAsync(CreateProductDto dto);
    Task<enProductSaveResult> UpdateAsync(UpdateProductDto dto);
    Task<enProductDeleteResult> DeleteAsync(Guid id);
}
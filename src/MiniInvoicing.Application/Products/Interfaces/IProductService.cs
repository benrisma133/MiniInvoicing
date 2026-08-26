using MiniInvoicing.Application.Products.Dtos;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Application.Products.Interfaces;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<(enProductOperationResult Result, ProductDto? Product)> CreateAsync(CreateProductDto dto);
    Task<(enProductOperationResult Result, ProductDto? Product)> UpdateAsync(UpdateProductDto dto);
    Task<(enProductDeleteResult Result, string? DeletedProductName)> DeleteAsync(Guid id);
}
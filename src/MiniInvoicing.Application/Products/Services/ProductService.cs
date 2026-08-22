using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Application.Products.Dtos;
using MiniInvoicing.Application.Products.Interfaces;
using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Application.Products.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    // بنشير للـ Interface فقط، ما عارفينش شكون غينفذها فـ Infrastructure
    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return null;

        return new ProductDto(product.Id, product.Name, product.Price, product.StockQuantity);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.StockQuantity));
    }

    public async Task<enProductSaveResult> CreateAsync(CreateProductDto dto)
    {
        // 1. التحقق من التكرار عبر الـ Repository Interface
        if (await _productRepository.ExistsByNameAsync(dto.Name))
            return enProductSaveResult.DuplicateName;

        // 2. إنشاء الـ Domain Entity (هنا كيطبقو شروط الـ Domain)
        Product product;
        try
        {
            product = new Product(dto.Name, dto.Price, dto.StockQuantity);
        }
        catch (ArgumentException)
        {
            return enProductSaveResult.InvalidPrice; // أو أي إينوم كيوافق الخطأ
        }

        // 3. الحفظ عبر الـ Interface
        return await _productRepository.AddAsync(product);
    }

    public async Task<enProductSaveResult> UpdateAsync(UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id);
        if (product == null)
            return enProductSaveResult.NotFound;

        try
        {
            product.UpdateDetails(dto.Name, dto.Price, dto.StockQuantity);
        }
        catch (ArgumentException)
        {
            return enProductSaveResult.InvalidPrice;
        }

        return await _productRepository.UpdateAsync(product);
    }

    public async Task<enProductDeleteResult> DeleteAsync(Guid id)
    {
        return await _productRepository.DeleteAsync(id);
    }
}
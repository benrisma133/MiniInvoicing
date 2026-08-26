using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Application.Products.Dtos;
using MiniInvoicing.Application.Products.Interfaces;
using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Application.Products.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

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

    public async Task<(enProductOperationResult Result, ProductDto? Product)> CreateAsync(CreateProductDto dto)
    {
        // 1. Domain Validation
        var validationResult = Product.Validate(dto.Name, dto.Price, dto.StockQuantity);
        if (validationResult != enProductOperationResult.Success)
            return (validationResult, null);

        // 2. Duplicate Check
        if (await _productRepository.ExistsByNameAsync(dto.Name))
            return (enProductOperationResult.DuplicateName, null);

        // 3. Create & Add Entity
        var product = new Product(dto.Name, dto.Price, dto.StockQuantity);
        var addResult = await _productRepository.AddAsync(product);

        if (addResult != enProductOperationResult.Success)
            return (addResult, null);

        var createdDto = new ProductDto(product.Id, product.Name, product.Price, product.StockQuantity);
        return (enProductOperationResult.Success, createdDto);
    }

    public async Task<(enProductOperationResult Result, ProductDto? Product)> UpdateAsync(UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id);
        if (product == null)
            return (enProductOperationResult.NotFound, null);

        // Domain Validation & Update
        var validationResult = product.UpdateDetails(dto.Name, dto.Price, dto.StockQuantity);
        if (validationResult != enProductOperationResult.Success)
            return (validationResult, null);

        var updateResult = await _productRepository.UpdateAsync(product);
        if (updateResult != enProductOperationResult.Success)
            return (updateResult, null);

        var updatedDto = new ProductDto(product.Id, product.Name, product.Price, product.StockQuantity);
        return (enProductOperationResult.Success, updatedDto);
    }

    public async Task<(enProductDeleteResult Result, string? DeletedProductName)> DeleteAsync(Guid id)
    {
        // 1. Fetch the product to get its name before deleting
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return (enProductDeleteResult.NotFound, null);

        // 2. Perform the deletion
        var deleteResult = await _productRepository.DeleteAsync(id);
        if (deleteResult != enProductDeleteResult.Deleted)
            return (deleteResult, null);

        // 3. Return Success along with the name of the deleted product
        return (enProductDeleteResult.Deleted, product.Name);
    }
}
using Microsoft.AspNetCore.Mvc;
using MiniInvoicing.Api.Common;
using MiniInvoicing.Application.Products.Dtos;
using MiniInvoicing.Application.Products.Interfaces;
using MiniInvoicing.Domain.Enums.Product;

namespace MiniInvoicing.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound(ApiResponse<object>.ErrorResponse($"Product with ID '{id}' was not found."));

        return Ok(ApiResponse<ProductDto>.SuccessResponse("Product retrieved successfully.", product));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<ProductDto>>.SuccessResponse("Products retrieved successfully.", products));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var (result, createdProduct) = await _productService.CreateAsync(dto);

        return result switch
        {
            enProductOperationResult.Success => Ok(ApiResponse<ProductDto>.SuccessResponse("Product created successfully.", createdProduct!)),
            enProductOperationResult.DuplicateName => Conflict(ApiResponse<object>.ErrorResponse($"A product named '{dto.Name}' already exists.")),
            enProductOperationResult.InvalidName => BadRequest(ApiResponse<object>.ErrorResponse("Product name is required.")),
            enProductOperationResult.InvalidPrice => BadRequest(ApiResponse<object>.ErrorResponse("Price must be greater than zero.")),
            enProductOperationResult.InvalidStockQuantity => BadRequest(ApiResponse<object>.ErrorResponse("Stock quantity cannot be negative.")),
            _ => BadRequest(ApiResponse<object>.ErrorResponse("Failed to create product."))
        };
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        if (id != dto.Id)
            return BadRequest(ApiResponse<object>.ErrorResponse("ID in route does not match ID in payload."));

        var (result, updatedProduct) = await _productService.UpdateAsync(dto);

        return result switch
        {
            enProductOperationResult.Success => Ok(ApiResponse<ProductDto>.SuccessResponse("Product updated successfully.", updatedProduct!)),
            enProductOperationResult.NotFound => NotFound(ApiResponse<object>.ErrorResponse($"Product with ID '{id}' was not found.")),
            enProductOperationResult.InvalidName => BadRequest(ApiResponse<object>.ErrorResponse("Product name is required.")),
            enProductOperationResult.InvalidPrice => BadRequest(ApiResponse<object>.ErrorResponse("Price must be greater than zero.")),
            enProductOperationResult.InvalidStockQuantity => BadRequest(ApiResponse<object>.ErrorResponse("Stock quantity cannot be negative.")),
            _ => BadRequest(ApiResponse<object>.ErrorResponse("Failed to update product."))
        };
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (result, deletedProductName) = await _productService.DeleteAsync(id);

        return result switch
        {
            enProductDeleteResult.Deleted => Ok(ApiResponse<object>.SuccessResponse($"Product with Name '{deletedProductName}' deleted successfully.", null!)),
            enProductDeleteResult.NotFound => NotFound(ApiResponse<object>.ErrorResponse($"Product with ID '{id}' was not found.")),
            _ => BadRequest(ApiResponse<object>.ErrorResponse("Failed to delete product."))
        };
    }
}
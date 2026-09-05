using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniInvoicing.Api.Common;
using MiniInvoicing.Application.Invoices.Dtos;
using MiniInvoicing.Application.Invoices.Interfaces;
using MiniInvoicing.Domain.Enums.Invoice;

namespace MiniInvoicing.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
            return NotFound(ApiResponse<object>.ErrorResponse($"Invoice with ID '{id}' was not found."));

        return Ok(ApiResponse<InvoiceDto>.SuccessResponse("Invoice retrieved successfully.", invoice));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _invoiceService.GetAllAsync();

        //invoices = Enumerable.Empty<InvoiceDto>();

        if (!invoices.Any())
        {
            return Ok(ApiResponse<IEnumerable<InvoiceDto>>.SuccessResponse("No invoices found.", invoices));
        }

        return Ok(ApiResponse<IEnumerable<InvoiceDto>>.SuccessResponse("Invoices retrieved successfully.", invoices));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        var (result, createdInvoice) = await _invoiceService.CreateAsync(dto);

        return result switch
        {
            enInvoiceOperationResult.Success => CreatedAtAction(
                nameof(GetById),
                new { id = createdInvoice!.Id },
                ApiResponse<InvoiceDto>.SuccessResponse("Invoice created successfully.", createdInvoice)),

            enInvoiceOperationResult.EmptyItems =>
                BadRequest(ApiResponse<object>.ErrorResponse("Invoice must contain at least one item.")),

            enInvoiceOperationResult.InvalidInvoiceNumber =>
                BadRequest(ApiResponse<object>.ErrorResponse("Invoice number is required.")),

            enInvoiceOperationResult.ProductNotFound =>
                NotFound(ApiResponse<object>.ErrorResponse("One or more products specified in the invoice were not found.")),

            enInvoiceOperationResult.InsufficientStock =>
                BadRequest(ApiResponse<object>.ErrorResponse("Insufficient stock for one or more requested products.")),

            enInvoiceOperationResult.DuplicateInvoiceNumber =>
                BadRequest(ApiResponse<object>.ErrorResponse($"Invoice number '{dto.InvoiceNumber}' already exists.")),

            _ => BadRequest(ApiResponse<object>.ErrorResponse("Failed to create invoice."))
        };
    }

    /// <summary>
    /// Creates multiple invoices in a single atomic transaction and updates product stock levels.
    /// </summary>
    /// <param name="dtos">List of invoices to be created.</param>
    /// <returns>A list of created invoices with generated IDs and calculated totals.</returns>
    /// <response code="200">Invoices created successfully and stock adjusted.</response>
    /// <response code="400">Validation failed (e.g., insufficient stock, duplicate invoice number, missing items, or empty payload).</response>
    /// <response code="500">Internal server error during transaction processing.</response>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRange([FromBody] List<CreateInvoiceDto> dtos)
    {
        var (result, invoices) = await _invoiceService.CreateRangeAsync(dtos);

        if (result != enInvoiceOperationResult.Success)
        {
            return result switch
            {
                enInvoiceOperationResult.ProductNotFound => BadRequest(new { message = "One or more requested products were not found." }),
                enInvoiceOperationResult.InsufficientStock => BadRequest(new { message = "Insufficient stock for one or more requested products." }),
                enInvoiceOperationResult.DuplicateInvoiceNumber => BadRequest(new { message = "One or more invoice numbers already exist in the database or payload." }),
                enInvoiceOperationResult.EmptyItems => BadRequest(new { message = "All invoices must contain at least one item and payload cannot be empty." }),
                enInvoiceOperationResult.InvalidInvoiceNumber => BadRequest(new { message = "One or more invoice numbers are invalid or missing." }),
                _ => BadRequest(new { message = "An error occurred while creating invoices." })
            };
        }

        return Ok(new { message = "Invoices created successfully.", data = invoices });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("The ID in the URL does not match the ID in the request body."));
        }

        var (result, updatedInvoice) = await _invoiceService.UpdateAsync(dto);

        return result switch
        {
            enInvoiceOperationResult.Success =>
                Ok(ApiResponse<InvoiceDto>.SuccessResponse("Invoice updated successfully.", updatedInvoice!)),

            enInvoiceOperationResult.NotFound =>
                NotFound(ApiResponse<object>.ErrorResponse($"Invoice with ID '{id}' was not found.")),

            enInvoiceOperationResult.EmptyItems =>
                BadRequest(ApiResponse<object>.ErrorResponse("Invoice must contain at least one item.")),

            enInvoiceOperationResult.InvalidInvoiceNumber =>
                BadRequest(ApiResponse<object>.ErrorResponse("Invoice number is required.")),

            enInvoiceOperationResult.ProductNotFound =>
                NotFound(ApiResponse<object>.ErrorResponse("One or more specified products were not found.")),

            enInvoiceOperationResult.InsufficientStock =>
                BadRequest(ApiResponse<object>.ErrorResponse("Insufficient stock to cover the requested adjustments.")),

            enInvoiceOperationResult.DuplicateInvoiceNumber =>
                BadRequest(ApiResponse<object>.ErrorResponse($"Invoice number '{dto.InvoiceNumber}' is already in use by another invoice.")),

            enInvoiceOperationResult.Failed =>
                BadRequest(ApiResponse<object>.ErrorResponse("An unexpected error occurred while updating the invoice in the database.")),

            enInvoiceOperationResult.InvalidOperation =>
                BadRequest(ApiResponse<object>.ErrorResponse("Invalid operation during stock adjustment.")),

            _ => BadRequest(ApiResponse<object>.ErrorResponse($"Failed to update invoice ({result})."))
        };
    }

    /// <summary>
    /// Updates multiple invoices in a single atomic transaction and reconciles product stock levels.
    /// </summary>
    /// <param name="dtos">List of invoices with updated details and items.</param>
    /// <returns>A list of updated invoices with recalculated totals and line items.</returns>
    /// <response code="200">Invoices updated successfully and stock adjustments applied.</response>
    /// <response code="400">Validation failed (e.g., insufficient stock, product not found, or empty items).</response>
    /// <response code="404">One or more specified invoice IDs do not exist.</response>
    /// <response code="500">Internal server error during transaction processing.</response>
    [HttpPut("bulk")]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRange([FromBody] List<UpdateInvoiceDto> dtos)
    {
        var (result, invoices) = await _invoiceService.UpdateRangeAsync(dtos);

        if (result != enInvoiceOperationResult.Success)
        {
            return result switch
            {
                enInvoiceOperationResult.NotFound => NotFound(new { message = "One or more invoices were not found." }),
                enInvoiceOperationResult.ProductNotFound => BadRequest(new { message = "One or more requested products were not found." }),
                enInvoiceOperationResult.InsufficientStock => BadRequest(new { message = "Insufficient stock for one or more requested product adjustments." }),
                enInvoiceOperationResult.EmptyItems => BadRequest(new { message = "Invoices must contain at least one item." }),
                enInvoiceOperationResult.InvalidInvoiceNumber => BadRequest(new { message = "One or more invoice numbers are invalid." }),
                _ => BadRequest(new { message = "An error occurred while updating invoices." })
            };
        }

        return Ok(new { message = "Invoices updated successfully.", data = invoices });
    }

    /// <summary>
    /// حذف فاتورة واحدة وتحديث المخزون
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.DeleteAsync(id, cancellationToken);

        return result switch
        {
            enInvoiceDeleteResult.Deleted => Ok(new { Message = "Invoice deleted successfully and stock restored." }), // 👈 200 OK with Message
            enInvoiceDeleteResult.NotFound => NotFound(new { Message = $"Invoice with ID '{id}' was not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while deleting the invoice." })
        };
    }

    /// <summary>
    /// حذف مجموعة من الفواتير دفعة واحدة وتحديث المخزون التجميعي
    /// </summary>
    [HttpDelete("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteInvoicesRequest request, CancellationToken cancellationToken)
    {
        if (request == null || request.InvoiceIds == null || !request.InvoiceIds.Any())
        {
            return BadRequest(new { Message = "Please provide at least one valid Invoice ID to delete." });
        }

        var result = await _invoiceService.DeleteRangeAsync(request.InvoiceIds, cancellationToken);

        return result switch
        {
            enInvoiceDeleteResult.Deleted => Ok(new { Message = "Invoices deleted successfully and stock restored." }), // 👈 200 OK with Message
            enInvoiceDeleteResult.NotFound => NotFound(new { Message = "One or more requested invoices were not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while deleting the invoices." })
        };
    }

}
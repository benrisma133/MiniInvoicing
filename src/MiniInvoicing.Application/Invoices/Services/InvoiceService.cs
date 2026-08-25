using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Application.Invoices.Dtos;
using MiniInvoicing.Application.Invoices.Interfaces;
using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Invoice;

namespace MiniInvoicing.Application.Invoices.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id);
        if (invoice == null) return null;

        return MapToDto(invoice);
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        var invoices = await _invoiceRepository.GetAllAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<enInvoiceSaveResult> CreateAsync(CreateInvoiceDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return enInvoiceSaveResult.EmptyItems;

        var requestedItems = dto.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var invoice = new Invoice(dto.InvoiceNumber);

        foreach (var itemDto in dto.Items)
        {
            invoice.AddItem(itemDto.ProductId, itemDto.Quantity, itemDto.UnitPrice);
        }

        try
        {
            await _invoiceRepository.CreateInvoiceWithStockUpdateAsync(invoice, requestedItems);
            return enInvoiceSaveResult.Saved;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found"))
        {
            return enInvoiceSaveResult.ProductNotFound;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("insufficient stock"))
        {
            return enInvoiceSaveResult.InsufficientStock;
        }
        catch
        {
            return enInvoiceSaveResult.Failed;
        }
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        var items = invoice.Items?.Select(item =>
            new InvoiceItemDto(item.Id, item.ProductId, item.Quantity, item.UnitPrice, item.LineTotal)
        ).ToList() ?? new List<InvoiceItemDto>();

        return new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.IssueDate,
            invoice.TotalAmount,
            invoice.VatAmount,
            invoice.TotalWithVat,
            items
        );
    }
}
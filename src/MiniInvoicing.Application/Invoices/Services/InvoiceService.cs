using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Application.Invoices.Dtos;
using MiniInvoicing.Application.Invoices.Interfaces;
using MiniInvoicing.Domain.Entities;
using MiniInvoicing.Domain.Enums.Invoice;

namespace MiniInvoicing.Application.Invoices.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IProductRepository _productRepository;

    public InvoiceService(IInvoiceRepository invoiceRepository, IProductRepository productRepository)
    {
        _invoiceRepository = invoiceRepository;
        _productRepository = productRepository;
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id);
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

        var invoice = new Invoice(dto.InvoiceNumber);

        foreach (var itemDto in dto.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemDto.ProductId);
            if (product == null)
                return enInvoiceSaveResult.ProductNotFound;

            try
            {
                // اقتطاع المخزون من الـ Domain Entity
                product.DeductStock(itemDto.Quantity);
            }
            catch (InvalidOperationException)
            {
                return enInvoiceSaveResult.InsufficientStock;
            }

            // إضافة البند للحاسبة
            invoice.AddItem(product.Id, itemDto.Quantity, product.Price);

            // تحديث المخزون في الـ Repository
            await _productRepository.UpdateAsync(product);
        }

        return await _invoiceRepository.AddAsync(invoice);
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        var items = invoice.Items.Select(item =>
            new InvoiceItemDto(item.Id, item.ProductId, item.Quantity, item.UnitPrice, item.LineTotal)
        ).ToList();

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
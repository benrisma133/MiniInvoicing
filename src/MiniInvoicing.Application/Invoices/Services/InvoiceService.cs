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
        var invoice = await _invoiceRepository.GetByIdWithItemsAndProductsAsync(id);
        if (invoice == null) return null;

        return MapToDto(invoice);
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        var invoices = await _invoiceRepository.GetAllAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<(enInvoiceOperationResult Result, InvoiceDto? Invoice)> CreateAsync(CreateInvoiceDto dto)
    {
        // 1. Domain / Basic Validation
        if (dto.Items == null || !dto.Items.Any())
            return (enInvoiceOperationResult.EmptyItems, null);

        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            return (enInvoiceOperationResult.InvalidInvoiceNumber, null);

        // 2. Duplicate Number Check
        if (await _invoiceRepository.ExistsByNumberAsync(dto.InvoiceNumber))
            return (enInvoiceOperationResult.DuplicateInvoiceNumber, null);

        // 3. Aggregate quantities for stock check
        var requestedItems = dto.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        // 4. Construct Aggregate Entity
        var invoice = new Invoice(dto.InvoiceNumber);
        foreach (var itemDto in dto.Items)
        {
            invoice.AddItem(itemDto.ProductId, itemDto.Quantity, itemDto.UnitPrice);
        }

        // 5. Persistence via Repository
        var result = await _invoiceRepository.CreateInvoiceWithStockUpdateAsync(invoice, requestedItems);
        if (result != enInvoiceOperationResult.Success)
            return (result, null);

        var createdDto = MapToDto(invoice);
        return (enInvoiceOperationResult.Success, createdDto);
    }

    public async Task<(enInvoiceOperationResult Result, List<InvoiceDto>? Invoices)> CreateRangeAsync(
        List<CreateInvoiceDto> dtos,
        CancellationToken cancellationToken = default)
    {
        // 1. Validation أساسي للقائمة
        if (dtos == null || !dtos.Any())
            return (enInvoiceOperationResult.EmptyItems, null);

        // فحص الفواتير التي لا تحتوي على عناصر
        if (dtos.Any(d => d.Items == null || !d.Items.Any()))
            return (enInvoiceOperationResult.EmptyItems, null);

        // فحص أرقام الفواتير الفارغة
        if (dtos.Any(d => string.IsNullOrWhiteSpace(d.InvoiceNumber)))
            return (enInvoiceOperationResult.InvalidInvoiceNumber, null);

        // 2. فحص التكرار الداخلي فـ الـ Request نفسه
        var invoiceNumbers = dtos.Select(d => d.InvoiceNumber).ToList();
        if (invoiceNumbers.Count != invoiceNumbers.Distinct().Count())
            return (enInvoiceOperationResult.DuplicateInvoiceNumber, null);

        // 3. تجميع الكميات المطلوبة لكل منتج عبر كاع الفواتير (Overall Quantities)
        var aggregatedRequestedItems = dtos
            .SelectMany(d => d.Items)
            .GroupBy(i => i.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.Quantity)
            );

        // 4. بناء الـ Domain Entities
        var invoices = new List<Invoice>();
        foreach (var dto in dtos)
        {
            var invoice = new Invoice(dto.InvoiceNumber);
            foreach (var itemDto in dto.Items)
            {
                invoice.AddItem(itemDto.ProductId, itemDto.Quantity, itemDto.UnitPrice);
            }
            invoices.Add(invoice);
        }

        // 5. الإرسال للـ Repository لتنفيذ العملية دفعة واحدة
        var result = await _invoiceRepository.CreateInvoicesRangeWithStockUpdateAsync(
            invoices,
            aggregatedRequestedItems,
            cancellationToken);

        if (result != enInvoiceOperationResult.Success)
            return (result, null);

        // 6. Mapping لـ List of DTOs
        var resultDtos = invoices.Select(MapToDto).ToList();
        return (enInvoiceOperationResult.Success, resultDtos);
    }

    public async Task<(enInvoiceOperationResult Result, InvoiceDto? Invoice)> UpdateAsync(UpdateInvoiceDto dto)
    {
        // 1. جلب الفاتورة الحالية من الـ Database مع العناصر ديالها
        var existingInvoice = await _invoiceRepository.GetByIdWithItemsAsync(dto.Id, false);
        if (existingInvoice == null)
            return (enInvoiceOperationResult.NotFound, null);

        // 2. فحص صحة رقم الفاتورة الجديد
        var validationResult = Invoice.Validate(dto.InvoiceNumber);
        if (validationResult != enInvoiceOperationResult.Success)
            return (validationResult, null);

        existingInvoice.InvoiceNumber = dto.InvoiceNumber;

        var stockAdjustments = new Dictionary<Guid, int>();

        // 3. تحديد الأيديات ديال العناصر اللي جاو فـ الـ Request
        var updatedItemIds = dto.Items
            .Where(i => i.Id.HasValue)
            .Select(i => i.Id!.Value)
            .ToList();

        // 4. تحديد العناصر المحذوفة
        var removedItems = existingInvoice.Items
            .Where(i => !updatedItemIds.Contains(i.Id))
            .ToList();

        // 👈 5. لائحة جديدة لتجميع العناصر المضافة
        var addedItems = new List<InvoiceItem>();

        // أ- التعامل مع العناصر المحذوفة
        foreach (var removedItem in removedItems)
        {
            if (!stockAdjustments.ContainsKey(removedItem.ProductId))
                stockAdjustments[removedItem.ProductId] = 0;

            stockAdjustments[removedItem.ProductId] -= removedItem.Quantity;
        }

        // ب- التعامل مع العناصر المعدلة والجديدة
        foreach (var itemDto in dto.Items)
        {
            // حالة 1: عنصر معدّل
            if (itemDto.Id.HasValue)
            {
                var oldItem = existingInvoice.Items.FirstOrDefault(i => i.Id == itemDto.Id.Value);
                if (oldItem != null)
                {
                    int diff = itemDto.Quantity - oldItem.Quantity;
                    if (!stockAdjustments.ContainsKey(itemDto.ProductId))
                        stockAdjustments[itemDto.ProductId] = 0;

                    stockAdjustments[itemDto.ProductId] += diff;

                    existingInvoice.UpdateItemDetails(oldItem.Id, itemDto.Quantity, itemDto.UnitPrice);
                }
            }
            // حالة 2: عنصر جديد كلياً
            else
            {
                if (!stockAdjustments.ContainsKey(itemDto.ProductId))
                    stockAdjustments[itemDto.ProductId] = 0;

                stockAdjustments[itemDto.ProductId] += itemDto.Quantity;

                // أضف العنصر للفاتورة
                existingInvoice.AddItem(itemDto.ProductId, itemDto.Quantity, itemDto.UnitPrice);

                // 👈 احصل على آخر عنصر تمت إضافته وضعه في قائمة addedItems
                var newlyAddedItem = existingInvoice.Items.Last();
                addedItems.Add(newlyAddedItem);
            }
        }

        // 👈 إرسال addedItems جنب إرسال removedItems
        var updateResult = await _invoiceRepository.UpdateInvoiceWithStockUpdateAsync(
            existingInvoice,
            stockAdjustments,
            removedItems,
            addedItems);

        if (updateResult != enInvoiceOperationResult.Success)
            return (updateResult, null);

        var resultDto = MapToDto(existingInvoice);

        return (enInvoiceOperationResult.Success, resultDto);
    }

    public async Task<(enInvoiceOperationResult Result, List<InvoiceDto>? Invoices)> UpdateRangeAsync(
        List<UpdateInvoiceDto> dtos,
        CancellationToken cancellationToken = default)
    {
        if (dtos == null || !dtos.Any())
            return (enInvoiceOperationResult.EmptyItems, null);

        // ⛔ شرط حماية 1: منع صُنع أو تعديل فاتورة بدون أي عناصر (تفريغ كامل)
        if (dtos.Any(d => d.Items == null || !d.Items.Any()))
            return (enInvoiceOperationResult.EmptyItems, null);

        // 1. جلب كاع الفواتير المطلوبة دفعة واحدة بـ Tracked Query
        var invoiceIds = dtos.Select(d => d.Id).ToList();
        var existingInvoices = await _invoiceRepository.GetByIdsWithItemsAsync(invoiceIds, false);

        if (existingInvoices.Count != invoiceIds.Distinct().Count())
            return (enInvoiceOperationResult.NotFound, null);

        var aggregatedStockAdjustments = new Dictionary<Guid, int>();
        var removedItems = new List<InvoiceItem>();
        var addedItems = new List<InvoiceItem>();

        foreach (var dto in dtos)
        {
            var existingInvoice = existingInvoices.First(i => i.Id == dto.Id);

            var validationResult = Invoice.Validate(dto.InvoiceNumber);
            if (validationResult != enInvoiceOperationResult.Success)
                return (validationResult, null);

            existingInvoice.InvoiceNumber = dto.InvoiceNumber;

            // العناصر المعدلة أو الباقية فـ الـ Request
            var updatedItemIds = dto.Items
                .Where(i => i.Id.HasValue)
                .Select(i => i.Id!.Value)
                .ToList();

            // 2. تحديد العناصر المحذوفة لهاد الفاتورة
            var invoiceRemovedItems = existingInvoice.Items
                .Where(i => !updatedItemIds.Contains(i.Id))
                .ToList();

            foreach (var removedItem in invoiceRemovedItems)
            {
                if (!aggregatedStockAdjustments.ContainsKey(removedItem.ProductId))
                    aggregatedStockAdjustments[removedItem.ProductId] = 0;

                aggregatedStockAdjustments[removedItem.ProductId] -= removedItem.Quantity;
            }

            removedItems.AddRange(invoiceRemovedItems);

            // 3. التعديل والإضافة
            foreach (var itemDto in dto.Items)
            {
                // حالة 1: عنصر موجود كايتعدل
                if (itemDto.Id.HasValue)
                {
                    var oldItem = existingInvoice.Items.FirstOrDefault(i => i.Id == itemDto.Id.Value);
                    if (oldItem != null)
                    {
                        int diff = itemDto.Quantity - oldItem.Quantity;

                        if (!aggregatedStockAdjustments.ContainsKey(itemDto.ProductId))
                            aggregatedStockAdjustments[itemDto.ProductId] = 0;

                        aggregatedStockAdjustments[itemDto.ProductId] += diff;

                        existingInvoice.UpdateItemDetails(oldItem.Id, itemDto.Quantity, itemDto.UnitPrice);
                    }
                }
                // حالة 2: عنصر جديد كيتزاد فـ الفاتورة
                else
                {
                    if (!aggregatedStockAdjustments.ContainsKey(itemDto.ProductId))
                        aggregatedStockAdjustments[itemDto.ProductId] = 0;

                    aggregatedStockAdjustments[itemDto.ProductId] += itemDto.Quantity;

                    existingInvoice.AddItem(itemDto.ProductId, itemDto.Quantity, itemDto.UnitPrice);

                    var newlyAddedItem = existingInvoice.Items.Last();
                    addedItems.Add(newlyAddedItem);
                }
            }
        }

        // 4. تنفيذ الـ Bulk Update فـ الـ Repository
        var updateResult = await _invoiceRepository.UpdateInvoicesRangeWithStockUpdateAsync(
            existingInvoices,
            aggregatedStockAdjustments,
            removedItems,
            addedItems,
            cancellationToken);

        if (updateResult != enInvoiceOperationResult.Success)
            return (updateResult, null);

        var resultDtos = existingInvoices.Select(MapToDto).ToList();
        return (enInvoiceOperationResult.Success, resultDtos);
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

    public async Task<enInvoiceDeleteResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _invoiceRepository.DeleteWithStockRestoreAsync(id, cancellationToken);
    }

    public async Task<enInvoiceDeleteResult> DeleteRangeAsync(
        List<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null || !ids.Any())
            return enInvoiceDeleteResult.Failed;

        return await _invoiceRepository.DeleteRangeWithStockRestoreAsync(ids, cancellationToken);
    }

}
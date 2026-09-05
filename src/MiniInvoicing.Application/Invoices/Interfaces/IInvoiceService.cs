using MiniInvoicing.Application.Invoices.Dtos;
using MiniInvoicing.Domain.Enums.Invoice;

namespace MiniInvoicing.Application.Invoices.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<(enInvoiceOperationResult Result, InvoiceDto? Invoice)> CreateAsync(CreateInvoiceDto dto);
    Task<(enInvoiceOperationResult Result, List<InvoiceDto>? Invoices)> CreateRangeAsync(
        List<CreateInvoiceDto> dtos,
        CancellationToken cancellationToken = default);

    Task<(enInvoiceOperationResult Result, InvoiceDto? Invoice)> UpdateAsync(UpdateInvoiceDto dto);

    Task<(enInvoiceOperationResult Result, List<InvoiceDto>? Invoices)> UpdateRangeAsync(
        List<UpdateInvoiceDto> dtos,
        CancellationToken cancellationToken = default);


    Task<enInvoiceDeleteResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<enInvoiceDeleteResult> DeleteRangeAsync(
        List<Guid> ids,
        CancellationToken cancellationToken = default);

}
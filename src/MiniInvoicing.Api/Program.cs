using MiniInvoicing.Application.Invoices.Interfaces;
using MiniInvoicing.Application.Invoices.Services;
using MiniInvoicing.Application.Products.Interfaces;
using MiniInvoicing.Application.Products.Services;
using MiniInvoicing.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Infrastructure Services (DbContext & Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Add Application Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// 3. Add Controllers support
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// 4. Map Controllers
app.MapControllers();

app.Run();
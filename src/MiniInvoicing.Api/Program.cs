using MiniInvoicing.Application.Invoices.Interfaces;
using MiniInvoicing.Application.Invoices.Services;
using MiniInvoicing.Application.Products.Interfaces;
using MiniInvoicing.Application.Products.Services;
using MiniInvoicing.Infrastructure;
using MiniInvoicing.Infrastructure.Persistence;

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

// 4. Automatic Migration & Database Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// 5. Map Controllers
app.MapControllers();

app.Run();
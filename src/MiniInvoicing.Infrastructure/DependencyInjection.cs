using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniInvoicing.Application.Common.Interfaces;
using MiniInvoicing.Infrastructure.Persistence;
using MiniInvoicing.Infrastructure.Persistence.Repositories;

namespace MiniInvoicing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // تسجيل الـ DbContext فـ الـ Runtime
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // 2. تسجيل الـ Repositories (هنا كربطو الـ Interface بالـ Implementation)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        return services;
    }
}
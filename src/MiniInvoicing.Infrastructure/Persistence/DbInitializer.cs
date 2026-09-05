using Microsoft.EntityFrameworkCore;
using MiniInvoicing.Domain.Entities;

namespace MiniInvoicing.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Automatically apply any pending EF Core migrations
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync();
        }

        // 2. Seed initial catalog products if table is empty
        if (!await context.Products.AnyAsync())
        {
            var initialProducts = new List<Product>
            {
                new("Espresso Special", 14.50m, 20),
                new("Pizza Hut", 30.80m, 15),
                new("Cappuccino", 15.00m, 25),
                new("Laptop Lenovo ThinkPad", 8500.50m, 10)
            };

            await context.Products.AddRangeAsync(initialProducts);
            await context.SaveChangesAsync();
        }
    }
}
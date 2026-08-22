using Microsoft.EntityFrameworkCore;
using MiniInvoicing.Domain.Entities;

namespace MiniInvoicing.Infrastructure.Persistence;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Price)
                .HasPrecision(18, 2);
        });

        // 2. Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(i => i.Id);

            entity.Property(i => i.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(i => i.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(i => i.VatAmount)
                .HasPrecision(18, 2);

            entity.Property(i => i.TotalWithVat)
                .HasPrecision(18, 2);
        });

        // 3. InvoiceItem
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("InvoiceItems");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(item => item.LineTotal)
                .HasPrecision(18, 2);

            // ربط العلاقات باستعمال الـ Properties الموجودين فـ Domain
            entity.HasOne(item => item.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(item => item.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany(p => p.InvoiceItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Optional extension point
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
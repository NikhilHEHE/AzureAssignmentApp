using Microsoft.EntityFrameworkCore;
using AzureAssignmentApp.Models;

namespace AzureAssignmentApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // OrderItem → Order (cascade delete order items when order deleted)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItem → Product (restrict: don't delete product if it has order items)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Laptop Pro 15",
                Description = "High-performance laptop with 15-inch display, 16GB RAM, 512GB SSD.",
                Price = 1299.99m,
                StockQuantity = 50,
                Category = "Electronics",
                ImageUrl = null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 2,
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with long battery life and precision tracking.",
                Price = 29.99m,
                StockQuantity = 200,
                Category = "Accessories",
                ImageUrl = null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 3,
                Name = "USB-C Hub",
                Description = "7-in-1 USB-C hub with HDMI, USB 3.0, SD card reader, and PD charging.",
                Price = 49.99m,
                StockQuantity = 150,
                Category = "Accessories",
                ImageUrl = null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 4,
                Name = "Mechanical Keyboard",
                Description = "Compact TKL mechanical keyboard with RGB backlight and Cherry MX switches.",
                Price = 89.99m,
                StockQuantity = 75,
                Category = "Accessories",
                ImageUrl = null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
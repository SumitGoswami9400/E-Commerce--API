using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product>   Products   => Set<Product>();
    public DbSet<Order>     Orders     => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User>      Users      => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Global Query Filter: Soft Delete ──────────────────
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(oi => !oi.IsDeleted);

        // ── Product ───────────────────────────────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasIndex(p => p.Category);
        });

        // ── Order ─────────────────────────────────────────────
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CustomerEmail).IsRequired();
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.Status)
                  .HasConversion<string>(); // Store enum as string in DB
            entity.HasIndex(o => o.CustomerEmail);
            entity.HasIndex(o => o.Status);
        });

        // ── OrderItem ─────────────────────────────────────────
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(oi => oi.SubTotal); // Computed, not stored

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete products
        });

        // ── User ──────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}

// ──────────────────────────────────────────────
//  Database Seeder
// ──────────────────────────────────────────────
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Products.Any()) return; // Already seeded

        var products = new List<Product>
        {
            new() { Name = "iPhone 15 Pro",       Description = "Latest Apple smartphone",         Price = 129999, Stock = 50,  Category = "Electronics", ImageUrl = "https://example.com/iphone15.jpg" },
            new() { Name = "Samsung Galaxy S24",   Description = "Flagship Android phone",          Price = 89999,  Stock = 75,  Category = "Electronics", ImageUrl = "https://example.com/s24.jpg" },
            new() { Name = "Sony WH-1000XM5",      Description = "Noise-cancelling headphones",     Price = 29999,  Stock = 100, Category = "Audio",       ImageUrl = "https://example.com/sony.jpg" },
            new() { Name = "MacBook Air M3",       Description = "Ultra-thin laptop",               Price = 114999, Stock = 30,  Category = "Laptops",     ImageUrl = "https://example.com/macbook.jpg" },
            new() { Name = "Nike Air Max 270",     Description = "Comfortable running shoes",       Price = 8999,   Stock = 200, Category = "Footwear",    ImageUrl = "https://example.com/nike.jpg" },
            new() { Name = "Levi's 511 Jeans",     Description = "Classic slim-fit jeans",          Price = 3499,   Stock = 150, Category = "Clothing",    ImageUrl = "https://example.com/levis.jpg" },
            new() { Name = "Instant Pot Duo 7-in-1", Description = "Multi-use pressure cooker",    Price = 7999,   Stock = 60,  Category = "Kitchen",     ImageUrl = "https://example.com/instantpot.jpg" },
            new() { Name = "Kindle Paperwhite",    Description = "E-reader with backlight",         Price = 13999,  Stock = 5,   Category = "Electronics", ImageUrl = "https://example.com/kindle.jpg" }, // Low stock
        };

        db.Products.AddRange(products);
        db.SaveChanges();

        // Seed admin user (password: Admin@123)
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                FullName     = "Admin User",
                Email        = "admin@ecommerce.com",
                PasswordHash = BCryptHelper.HashPassword("Admin@123"),
                Role         = "Admin"
            });
            db.SaveChanges();
        }
    }
}

// Simple BCrypt helper to avoid external dependency in seeder
public static class BCryptHelper
{
    public static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}

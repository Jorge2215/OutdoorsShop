using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Infrastructure.Identity;

namespace OutdoorsShop.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCategory> Categories { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<SalesOrder> Orders { get; set; }
    public DbSet<SalesOrderDetail> OrderItems { get; set; }
    public DbSet<ProductInventory> Inventory { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Explicit primary keys (non-conventional names)
        builder.Entity<ProductCategory>().HasKey(c => c.CategoryID);
        builder.Entity<SalesOrder>().HasKey(o => o.OrderID);
        builder.Entity<SalesOrderDetail>().HasKey(d => d.OrderDetailID);

        // Table name overrides
        builder.Entity<ProductCategory>().ToTable("Categories");
        builder.Entity<SalesOrder>().ToTable("Orders");
        builder.Entity<SalesOrderDetail>().ToTable("OrderItems");
        builder.Entity<ProductInventory>().ToTable("Inventory");

        // Decimal precision
        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<SalesOrder>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<SalesOrderDetail>()
            .Property(d => d.UnitPrice)
            .HasColumnType("decimal(18,2)");

        // Enum conversions stored as strings for readability
        builder.Entity<SalesOrder>()
            .Property(o => o.Status)
            .HasConversion<string>();

        builder.Entity<SalesOrder>()
            .Property(o => o.PaymentStatus)
            .HasConversion<string>();

        // ProductInventory — ProductID is both PK and FK (1:1 with Product)
        builder.Entity<ProductInventory>()
            .HasKey(i => i.ProductID);

        builder.Entity<ProductInventory>()
            .HasOne(i => i.Product)
            .WithOne()
            .HasForeignKey<ProductInventory>(i => i.ProductID);

        // Global query filters for soft deletes
        builder.Entity<Product>()
            .HasQueryFilter(p => p.IsActive);

        builder.Entity<ProductCategory>()
            .HasQueryFilter(c => c.IsActive);

        // Seed initial categories
        builder.Entity<ProductCategory>().HasData(
            new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true },
            new ProductCategory { CategoryID = 2, Name = "Trekking", IsActive = true },
            new ProductCategory { CategoryID = 3, Name = "Cycling", IsActive = true },
            new ProductCategory { CategoryID = 4, Name = "Climbing", IsActive = true }
        );
    }
}

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
    public DbSet<StockUpdateLog> StockUpdateLogs { get; set; }
    public DbSet<ReportExportRequest> ReportExportRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProductCategory>().HasKey(c => c.CategoryID);
        builder.Entity<SalesOrder>().HasKey(o => o.OrderID);
        builder.Entity<SalesOrderDetail>().HasKey(d => d.OrderDetailID);

        builder.Entity<ProductCategory>().ToTable("Categories");
        builder.Entity<SalesOrder>().ToTable("Orders");
        builder.Entity<SalesOrderDetail>().ToTable("OrderItems");
        builder.Entity<ProductInventory>().ToTable("Inventory");

        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Product>()
            .Property(p => p.DiscountMultiplier)
            .HasColumnType("decimal(5,4)")
            .HasDefaultValue(1.0m);

        builder.Entity<SalesOrder>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<SalesOrder>()
            .Property(o => o.PaymentMethod)
            .HasMaxLength(100);

        builder.Entity<SalesOrder>()
            .Property(o => o.ShippingAddress)
            .HasMaxLength(500);

        builder.Entity<SalesOrderDetail>()
            .Property(d => d.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Entity<SalesOrder>()
            .Property(o => o.Status)
            .HasConversion<string>();

        builder.Entity<SalesOrder>()
            .Property(o => o.PaymentStatus)
            .HasConversion<string>();

        builder.Entity<Customer>()
            .Property(c => c.FirstName)
            .HasMaxLength(100);

        builder.Entity<Customer>()
            .Property(c => c.LastName)
            .HasMaxLength(100);

        builder.Entity<Customer>()
            .Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Entity<Customer>()
            .Property(c => c.Address)
            .HasMaxLength(500);

        builder.Entity<Customer>()
            .HasIndex(c => c.UserId)
            .IsUnique();

        builder.Entity<Customer>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductInventory>()
            .HasKey(i => i.ProductID);

        builder.Entity<ProductInventory>()
            .HasOne(i => i.Product)
            .WithOne()
            .HasForeignKey<ProductInventory>(i => i.ProductID);

        builder.Entity<Product>()
            .HasQueryFilter(p => p.IsActive);

        builder.Entity<ProductCategory>()
            .HasQueryFilter(c => c.IsActive);

        builder.Entity<Customer>()
            .HasQueryFilter(c => c.IsActive);

        builder.Entity<ProductCategory>().HasData(
            new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true },
            new ProductCategory { CategoryID = 2, Name = "Trekking", IsActive = true },
            new ProductCategory { CategoryID = 3, Name = "Cycling", IsActive = true },
            new ProductCategory { CategoryID = 4, Name = "Climbing", IsActive = true }
        );

        builder.Entity<StockUpdateLog>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Reason).HasMaxLength(50).IsRequired();
            entity.Property(s => s.Notes).HasMaxLength(500);
            entity.ToTable("StockUpdateLogs");
        });

        builder.Entity<ReportExportRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.ToTable("ReportExportRequests");
            entity.Property(r => r.ReportType).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Format).HasMaxLength(20).IsRequired();
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired();
            entity.Property(r => r.BlobName).HasMaxLength(500);
            entity.Property(r => r.BlobUrl).HasMaxLength(2000);
            entity.Property(r => r.FileName).HasMaxLength(255);
            entity.Property(r => r.ContentType).HasMaxLength(255);
            entity.Property(r => r.ErrorMessage).HasMaxLength(2000);
            entity.Property(r => r.RequestedByUserId).HasMaxLength(450);
        });
    }
}

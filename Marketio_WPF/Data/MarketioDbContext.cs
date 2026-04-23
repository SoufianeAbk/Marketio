using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_WPF.Models;

namespace Marketio_WPF.Data
{
    public class MarketioDbContext : IdentityDbContext<AppUser>
    {
        public MarketioDbContext(DbContextOptions<MarketioDbContext> options) : base(options)
        {
        }

        // DbSets for domain entities
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product Configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.Price)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.Stock)
                    .IsRequired();

                entity.Property(e => e.Category)
                    .IsRequired();

                entity.Property(e => e.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasMany(e => e.OrderItems)
                    .WithOne(oi => oi.Product)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Order Configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CustomerId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.OrderDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasDefaultValue(OrderStatus.Pending);

                entity.Property(e => e.PaymentMethod)
                    .IsRequired();

                entity.Property(e => e.TotalAmount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.ShippingAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.BillingAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                // Unique constraint on OrderNumber
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique();

                // Foreign key to Customer
                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OrderItem Configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OrderId)
                    .IsRequired();

                entity.Property(e => e.ProductId)
                    .IsRequired();

                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.UnitPrice)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.TotalPrice)
                    .HasPrecision(18, 2)
                    .IsRequired();

                // Composite unique constraint: one product per order
                entity.HasIndex(e => new { e.OrderId, e.ProductId })
                    .IsUnique();
            });

            // Customer Configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasMaxLength(450);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Unique constraint on Email
                entity.HasIndex(e => e.Email)
                    .IsUnique();
            });

            // Global query filters for soft-delete
            // Filter out inactive products
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => p.IsActive);

            // Seed some default data (optional)
            SeedDefaultData(modelBuilder);
        }

        private void SeedDefaultData(ModelBuilder modelBuilder)
        {
            // Default product categories seed (if needed)
            // This can be expanded based on requirements
        }
    }
}
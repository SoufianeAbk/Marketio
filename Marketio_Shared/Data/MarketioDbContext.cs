using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Marketio_Shared.Data
{
    public class MarketioDbContext : IdentityDbContext<AppUser, IdentityRole, string>
    {
        public MarketioDbContext(DbContextOptions<MarketioDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductTranslation> ProductTranslations { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<GdprAuditLog> GdprAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product Configuratie
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
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasMany(e => e.OrderItems)
                    .WithOne(oi => oi.Product)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Translations)
                    .WithOne(t => t.Product)
                    .HasForeignKey(t => t.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ProductTranslation Configuratie
            modelBuilder.Entity<ProductTranslation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Locale)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(2000);

                // Unieke index: per product slechts één vertaling per taal
                entity.HasIndex(e => new { e.ProductId, e.Locale })
                    .IsUnique();
            });

            // Order Configuratie
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
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

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

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique();

                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OrderItem Configuratie
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

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasIndex(e => new { e.OrderId, e.ProductId })
                    .IsUnique();
            });

            // Customer Configuratie
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
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasIndex(e => e.Email)
                    .IsUnique();
            });

            // GdprAuditLog Configuratie
            modelBuilder.Entity<GdprAuditLog>(entity =>
            {
                entity.ToTable("GdprAuditLogs");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(x => x.ApplicationUserId)
                    .HasMaxLength(450);

                entity.Property(x => x.EventType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.ConsentType)
                    .HasMaxLength(100);

                entity.Property(x => x.IpAddress)
                    .HasMaxLength(45);

                entity.Property(x => x.UserAgent)
                    .HasMaxLength(500);

                entity.Property(x => x.ProcessedBy)
                    .HasMaxLength(256);

                entity.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.EventType);
                entity.HasIndex(x => x.Timestamp);
                entity.HasIndex(x => new { x.UserId, x.EventType });

                entity.HasOne(x => x.AppUser)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Globale Query Filters (soft-delete)
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => p.IsActive);

            modelBuilder.Entity<Order>()
                .HasQueryFilter(o => o.IsActive);

            modelBuilder.Entity<OrderItem>()
                .HasQueryFilter(oi => oi.IsActive);

            modelBuilder.Entity<Customer>()
                .HasQueryFilter(c => c.IsActive);

            modelBuilder.Entity<GdprAuditLog>()
                .HasQueryFilter(g => g.IsActive);
        }
    }
}
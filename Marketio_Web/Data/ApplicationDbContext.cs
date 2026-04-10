using Marketio_Shared.Entities;
using Marketio_Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Marketio_Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<GdprAuditLog> GdprAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ApplicationUser configuration
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Address).HasMaxLength(500);
            });

            // GDPR Audit Log configuration
            modelBuilder.Entity<GdprAuditLog>(entity =>
            {
                entity.ToTable("GdprAuditLogs");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.EventType);
                entity.HasIndex(x => x.Timestamp);
                entity.HasIndex(x => new { x.UserId, x.EventType });

                entity.HasOne(x => x.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(p => p.Name).HasMaxLength(200);
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(o => o.CustomerId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(o => o.OrderDate)
                    .IsRequired();

                entity.Property(o => o.Status)
                    .IsRequired();

                entity.Property(o => o.PaymentMethod)
                    .IsRequired();

                entity.Property(o => o.TotalAmount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(o => o.ShippingAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(o => o.BillingAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(o => o.ShippedDate);
                entity.Property(o => o.DeliveredDate);

                entity.HasIndex(o => o.OrderNumber).IsUnique();
                entity.HasIndex(o => o.CustomerId);

                // Foreign key to ApplicationUser (CustomerId maps to ApplicationUser.Id)
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OrderItem configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
                entity.Property(oi => oi.TotalPrice).HasPrecision(18, 2);

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed data: 5 categories met elk 4 producten
            SeedProducts(modelBuilder);
        }

        private void SeedProducts(ModelBuilder modelBuilder)
        {
            // Use a fixed timestamp for all seed data to prevent migration issues
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var products = new List<Product>
            {
                // Electronics (4 producten)
                new Product { Id = 1, Name = "Laptop Dell XPS 15", Description = "Krachtige laptop met 16GB RAM en 512GB SSD", Price = 1299.99m, Stock = 15, Category = Marketio_Shared.Enums.ProductCategory.Electronics, ImageUrl = "/images/laptop.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 2, Name = "iPhone 15 Pro", Description = "Nieuwste iPhone met A17 Pro chip", Price = 1099.00m, Stock = 25, Category = Marketio_Shared.Enums.ProductCategory.Electronics, ImageUrl = "/images/iphone.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 3, Name = "Samsung 4K TV 55\"", Description = "Crystal UHD 4K Smart TV", Price = 699.99m, Stock = 10, Category = Marketio_Shared.Enums.ProductCategory.Electronics, ImageUrl = "/images/tv.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 4, Name = "Sony WH-1000XM5", Description = "Noise cancelling koptelefoon", Price = 349.99m, Stock = 30, Category = Marketio_Shared.Enums.ProductCategory.Electronics, ImageUrl = "/images/headphones.jpg", IsActive = true, CreatedAt = seedDate },
                
                // Clothing (4 producten)
                new Product { Id = 6, Name = "Nike Air Max Sneakers", Description = "Comfortabele sportschoenen", Price = 129.99m, Stock = 50, Category = Marketio_Shared.Enums.ProductCategory.Clothing, ImageUrl = "/images/sneakers.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 7, Name = "Levi's 501 Jeans", Description = "Klassieke straight fit jeans", Price = 89.99m, Stock = 40, Category = Marketio_Shared.Enums.ProductCategory.Clothing, ImageUrl = "/images/jeans.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 8, Name = "Adidas Hoodie", Description = "Warme hoodie met logo", Price = 59.99m, Stock = 35, Category = Marketio_Shared.Enums.ProductCategory.Clothing, ImageUrl = "/images/hoodie.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 9, Name = "Tommy Hilfiger Polo", Description = "Katoenen polo shirt", Price = 69.99m, Stock = 45, Category = Marketio_Shared.Enums.ProductCategory.Clothing, ImageUrl = "/images/polo.jpg", IsActive = true, CreatedAt = seedDate },

                // Books (4 producten)
                new Product { Id = 11, Name = "Clean Code - Robert Martin", Description = "Handbook of Agile Software Craftsmanship", Price = 39.99m, Stock = 60, Category = Marketio_Shared.Enums.ProductCategory.Books, ImageUrl = "/images/cleancode.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 12, Name = "The Pragmatic Programmer", Description = "From Journeyman to Master", Price = 44.99m, Stock = 55, Category = Marketio_Shared.Enums.ProductCategory.Books, ImageUrl = "/images/pragmatic.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 13, Name = "Design Patterns", Description = "Elements of Reusable Object-Oriented Software", Price = 49.99m, Stock = 40, Category = Marketio_Shared.Enums.ProductCategory.Books, ImageUrl = "/images/patterns.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 14, Name = "Harry Potter Box Set", Description = "Complete serie van 7 boeken", Price = 89.99m, Stock = 25, Category = Marketio_Shared.Enums.ProductCategory.Books, ImageUrl = "/images/harrypotter.jpg", IsActive = true, CreatedAt = seedDate },
                
                // HomeAndGarden (4 producten)
                new Product { Id = 16, Name = "Dyson V15 Stofzuiger", Description = "Draadloze stofzuiger met laser", Price = 599.99m, Stock = 15, Category = Marketio_Shared.Enums.ProductCategory.HomeAndGarden, ImageUrl = "/images/vacuum.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 17, Name = "Philips Airfryer XXL", Description = "Hetelucht friteuse 7.3L", Price = 249.99m, Stock = 20, Category = Marketio_Shared.Enums.ProductCategory.HomeAndGarden, ImageUrl = "/images/airfryer.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 18, Name = "IKEA Bureau BEKANT", Description = "Verstelbaar bureau 160x80cm", Price = 349.00m, Stock = 12, Category = Marketio_Shared.Enums.ProductCategory.HomeAndGarden, ImageUrl = "/images/desk.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 19, Name = "Nespresso Machine", Description = "Koffiemachine met melkopschuimer", Price = 199.99m, Stock = 30, Category = Marketio_Shared.Enums.ProductCategory.HomeAndGarden, ImageUrl = "/images/nespresso.jpg", IsActive = true, CreatedAt = seedDate },

                // Sports (4 producten)
                new Product { Id = 21, Name = "Yoga Mat Premium", Description = "Extra dikke yoga mat 6mm", Price = 39.99m, Stock = 50, Category = Marketio_Shared.Enums.ProductCategory.Sports, ImageUrl = "/images/yogamat.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 22, Name = "Dumbbells Set 20kg", Description = "Verstelbare dumbbell set", Price = 149.99m, Stock = 25, Category = Marketio_Shared.Enums.ProductCategory.Sports, ImageUrl = "/images/dumbbells.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 23, Name = "Garmin Forerunner 265", Description = "GPS hardloop smartwatch", Price = 449.99m, Stock = 18, Category = Marketio_Shared.Enums.ProductCategory.Sports, ImageUrl = "/images/garmin.jpg", IsActive = true, CreatedAt = seedDate },
                new Product { Id = 24, Name = "Voetbal Nike Strike", Description = "Officiële wedstrijdbal", Price = 29.99m, Stock = 40, Category = Marketio_Shared.Enums.ProductCategory.Sports, ImageUrl = "/images/football.jpg", IsActive = true, CreatedAt = seedDate },
            };

            modelBuilder.Entity<Product>().HasData(products);
        }
    }
}
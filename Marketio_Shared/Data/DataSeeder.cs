using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_Shared.Models;

namespace Marketio_Shared.Data
{
    public class DataSeeder
    {
        private readonly MarketioDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DataSeeder(
            MarketioDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            try
            {
                await SeedRolesAsync();
                await SeedUsersAsync();
                await SeedProductsAsync();
                await SeedProductTranslationsAsync();
                await SeedCustomersAsync();
                await SeedOrdersAsync();

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Seeding error: {ex.Message}");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { "Admin", "Manager", "Customer" };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private async Task SeedUsersAsync()
        {
            // Admin gebruiker
            var adminUser = new AppUser
            {
                UserName = "admin@marketio.be",
                Email = "admin@marketio.be",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Marketio",
                Address = "Rue de la Paix 1, 1000 Brussels, Belgium",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (await _userManager.FindByEmailAsync(adminUser.Email) == null)
            {
                var result = await _userManager.CreateAsync(adminUser, "Admin@12345");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Test gebruiker
            var testUser = new AppUser
            {
                UserName = "user@marketio.be",
                Email = "user@marketio.be",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Doe",
                Address = "Rue de la Loi 50, 1040 Brussels, Belgium",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (await _userManager.FindByEmailAsync(testUser.Email) == null)
            {
                var result = await _userManager.CreateAsync(testUser, "User@12345");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(testUser, "Customer");
                }
            }

            // Manager gebruiker
            var managerUser = new AppUser
            {
                UserName = "manager@marketio.be",
                Email = "manager@marketio.be",
                EmailConfirmed = true,
                FirstName = "Jane",
                LastName = "Smith",
                Address = "Avenue Louise 500, 1050 Brussels, Belgium",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (await _userManager.FindByEmailAsync(managerUser.Email) == null)
            {
                var result = await _userManager.CreateAsync(managerUser, "Manager@12345");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }
        }

        private async Task SeedProductsAsync()
        {
            if (await _context.Products.AnyAsync())
                return;

            var products = new List<Product>
            {
                // Electronica
                new Product
                {
                    Name = "Wireless Headphones",
                    Description = "Premium noise-cancelling wireless headphones with 30-hour battery life",
                    Price = 199.99m,
                    Stock = 50,
                    Category = ProductCategory.Electronics,
                    ImageUrl = "/images/headphones.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "USB-C Hub",
                    Description = "7-in-1 USB-C hub with HDMI, USB 3.0, SD card reader",
                    Price = 49.99m,
                    Stock = 100,
                    Category = ProductCategory.Electronics,
                    ImageUrl = "/images/laptop.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                // Kledij
                new Product
                {
                    Name = "Cotton T-Shirt",
                    Description = "100% organic cotton comfortable t-shirt available in multiple colors",
                    Price = 24.99m,
                    Stock = 200,
                    Category = ProductCategory.Clothing,
                    ImageUrl = "/images/polo.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Denim Jeans",
                    Description = "Classic blue denim jeans with comfortable fit",
                    Price = 79.99m,
                    Stock = 75,
                    Category = ProductCategory.Clothing,
                    ImageUrl = "/images/jeans.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                // Boeken
                new Product
                {
                    Name = "C# Programming Guide",
                    Description = "Comprehensive guide to C# programming with practical examples",
                    Price = 59.99m,
                    Stock = 30,
                    Category = ProductCategory.Books,
                    ImageUrl = "/images/cleancode.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Web Development Basics",
                    Description = "Learn web development from HTML to backend frameworks",
                    Price = 44.99m,
                    Stock = 40,
                    Category = ProductCategory.Books,
                    ImageUrl = "/images/pragmatic.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                // Huis en Tuin
                new Product
                {
                    Name = "Plant Pot Set",
                    Description = "Set of 5 ceramic plant pots with drainage holes",
                    Price = 34.99m,
                    Stock = 60,
                    Category = ProductCategory.HomeAndGarden,
                    ImageUrl = "/images/desk.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Garden Tool Set",
                    Description = "Complete garden tool set with 10 essential tools",
                    Price = 89.99m,
                    Stock = 25,
                    Category = ProductCategory.HomeAndGarden,
                    ImageUrl = "/images/vacuum.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                // Sports
                new Product
                {
                    Name = "Running Shoes",
                    Description = "Professional running shoes with advanced cushioning",
                    Price = 129.99m,
                    Stock = 45,
                    Category = ProductCategory.Sports,
                    ImageUrl = "/images/sneakers.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Yoga Mat",
                    Description = "Non-slip yoga mat with 8mm cushioning, eco-friendly",
                    Price = 39.99m,
                    Stock = 80,
                    Category = ProductCategory.Sports,
                    ImageUrl = "/images/yogamat.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();
        }

        private async Task SeedProductTranslationsAsync()
        {
            if (await _context.ProductTranslations.AnyAsync())
                return;

            // Laad alle producten (inclusief inactieve) om de IDs op te halen
            var products = await _context.Products
                .IgnoreQueryFilters()
                .ToListAsync();

            // Vertalingen per productnaam (Engels = fallback in de DB, hier enkel nl + fr)
            var translationMap = new Dictionary<string, (string NameNl, string DescNl, string NameFr, string DescFr)>
            {
                ["Wireless Headphones"] = (
                    "Draadloze Koptelefoon",
                    "Premium ruisonderdrukkende draadloze koptelefoon met 30 uur batterijduur",
                    "Casque sans fil",
                    "Casque sans fil à réduction de bruit premium avec 30 heures d'autonomie"),

                ["USB-C Hub"] = (
                    "USB-C Hub",
                    "7-in-1 USB-C hub met HDMI, USB 3.0 en SD-kaartlezer",
                    "Hub USB-C",
                    "Hub USB-C 7-en-1 avec HDMI, USB 3.0 et lecteur de carte SD"),

                ["Cotton T-Shirt"] = (
                    "Katoenen T-Shirt",
                    "100% biologisch katoenen t-shirt beschikbaar in meerdere kleuren",
                    "T-Shirt en Coton",
                    "T-shirt confortable en coton biologique 100% disponible en plusieurs couleurs"),

                ["Denim Jeans"] = (
                    "Spijkerbroek",
                    "Klassieke blauwe spijkerbroek met comfortabele pasvorm",
                    "Jean en Denim",
                    "Jean bleu classique avec une coupe confortable"),

                ["C# Programming Guide"] = (
                    "C# Programmeergids",
                    "Uitgebreide gids voor C# programmeren met praktische voorbeelden",
                    "Guide de programmation C#",
                    "Guide complet de programmation C# avec des exemples pratiques"),

                ["Web Development Basics"] = (
                    "Basis Webontwikkeling",
                    "Leer webontwikkeling van HTML tot backend-frameworks",
                    "Bases du développement web",
                    "Apprenez le développement web de HTML aux frameworks backend"),

                ["Plant Pot Set"] = (
                    "Bloempottenset",
                    "Set van 5 keramische bloempotten met afvoergaten",
                    "Set de pots de fleurs",
                    "Ensemble de 5 pots en céramique avec trous de drainage"),

                ["Garden Tool Set"] = (
                    "Tuingereedschapsset",
                    "Complete tuingereedschapsset met 10 essentiële gereedschappen",
                    "Set d'outils de jardin",
                    "Set d'outils de jardin complet avec 10 outils essentiels"),

                ["Running Shoes"] = (
                    "Hardloopschoenen",
                    "Professionele hardloopschoenen met geavanceerde demping",
                    "Chaussures de course",
                    "Chaussures de course professionnelles avec amorti avancé"),

                ["Yoga Mat"] = (
                    "Yogamat",
                    "Antislip yogamat met 8 mm demping, milieuvriendelijk",
                    "Tapis de yoga",
                    "Tapis de yoga antidérapant avec 8 mm de rembourrage, écologique")
            };

            var translations = new List<ProductTranslation>();

            foreach (var product in products)
            {
                if (!translationMap.TryGetValue(product.Name, out var t))
                    continue;

                translations.Add(new ProductTranslation
                {
                    ProductId = product.Id,
                    Locale = "nl",
                    Name = t.NameNl,
                    Description = t.DescNl
                });

                translations.Add(new ProductTranslation
                {
                    ProductId = product.Id,
                    Locale = "fr",
                    Name = t.NameFr,
                    Description = t.DescFr
                });
            }

            if (translations.Count > 0)
            {
                await _context.ProductTranslations.AddRangeAsync(translations);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedCustomersAsync()
        {
            if (await _context.Customers.AnyAsync())
                return;

            var customers = new List<Customer>
            {
                new Customer
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "john.doe@example.be",
                    FirstName = "John",
                    LastName = "Doe",
                    PhoneNumber = "+32 2 1234567",
                    Address = "Rue de la Paix 10, 1000 Brussels, Belgium",
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "marie.martin@example.be",
                    FirstName = "Marie",
                    LastName = "Martin",
                    PhoneNumber = "+32 2 7654321",
                    Address = "Avenue Louise 100, 1050 Brussels, Belgium",
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "alex.dupont@example.be",
                    FirstName = "Alex",
                    LastName = "Dupont",
                    PhoneNumber = "+32 3 9876543",
                    Address = "Grote Markt 5, 2000 Antwerp, Belgium",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Customers.AddRangeAsync(customers);
            await _context.SaveChangesAsync();
        }

        private async Task SeedOrdersAsync()
        {
            if (await _context.Orders.AnyAsync())
                return;

            var customers = await _context.Customers.ToListAsync();
            var products = await _context.Products.ToListAsync();

            if (!customers.Any() || !products.Any())
                return;

            var orders = new List<Order>
            {
                new Order
                {
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-001",
                    CustomerId = customers[0].Id,
                    OrderDate = DateTime.UtcNow.AddDays(-5),
                    Status = OrderStatus.Delivered,
                    PaymentMethod = PaymentMethod.CreditCard,
                    TotalAmount = 249.98m,
                    ShippingAddress = customers[0].Address,
                    BillingAddress = customers[0].Address,
                    DeliveredDate = DateTime.UtcNow.AddDays(-1),
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            ProductId = products[0].Id,
                            Quantity = 1,
                            UnitPrice = products[0].Price,
                            TotalPrice = products[0].Price
                        },
                        new OrderItem
                        {
                            ProductId = products[2].Id,
                            Quantity = 1,
                            UnitPrice = products[2].Price,
                            TotalPrice = products[2].Price
                        }
                    }
                },
                new Order
                {
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-002",
                    CustomerId = customers[1].Id,
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    Status = OrderStatus.Processing,
                    PaymentMethod = PaymentMethod.BankTransfer,
                    TotalAmount = 169.98m,
                    ShippingAddress = customers[1].Address,
                    BillingAddress = customers[1].Address,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            ProductId = products[4].Id,
                            Quantity = 2,
                            UnitPrice = products[4].Price,
                            TotalPrice = products[4].Price * 2
                        }
                    }
                },
                new Order
                {
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-003",
                    CustomerId = customers[2].Id,
                    OrderDate = DateTime.UtcNow.AddDays(-10),
                    Status = OrderStatus.Shipped,
                    PaymentMethod = PaymentMethod.PayPal,
                    TotalAmount = 129.99m,
                    ShippingAddress = customers[2].Address,
                    BillingAddress = customers[2].Address,
                    ShippedDate = DateTime.UtcNow.AddDays(-7),
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            ProductId = products[8].Id,
                            Quantity = 1,
                            UnitPrice = products[8].Price,
                            TotalPrice = products[8].Price
                        }
                    }
                }
            };

            await _context.Orders.AddRangeAsync(orders);
            await _context.SaveChangesAsync();
        }
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_WPF.Models;

namespace Marketio_WPF.Data
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
                // Seed roles
                await SeedRolesAsync();

                // Seed users
                await SeedUsersAsync();

                // Seed products
                await SeedProductsAsync();

                // Seed customers
                await SeedCustomersAsync();

                // Seed orders and order items
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
            var roles = new[] { "Admin", "Manager", "User" };

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
            // Admin user
            var adminUser = new AppUser
            {
                UserName = "admin@marketio.be",
                Email = "admin@marketio.be",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Marketio",
                DefaultAddress = "Rue de la Paix 1, 1000 Brussels, Belgium",
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

            // Test user
            var testUser = new AppUser
            {
                UserName = "user@marketio.be",
                Email = "user@marketio.be",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Doe",
                DefaultAddress = "Rue de la Loi 50, 1040 Brussels, Belgium",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (await _userManager.FindByEmailAsync(testUser.Email) == null)
            {
                var result = await _userManager.CreateAsync(testUser, "User@12345");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(testUser, "User");
                }
            }

            // Manager user
            var managerUser = new AppUser
            {
                UserName = "manager@marketio.be",
                Email = "manager@marketio.be",
                EmailConfirmed = true,
                FirstName = "Jane",
                LastName = "Smith",
                DefaultAddress = "Avenue Louise 500, 1050 Brussels, Belgium",
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
                // Electronics
                new Product
                {
                    Name = "Wireless Headphones",
                    Description = "Premium noise-cancelling wireless headphones with 30-hour battery life",
                    Price = 199.99m,
                    Stock = 50,
                    Category = ProductCategory.Electronics,
                    ImageUrl = "https://via.placeholder.com/300?text=Wireless+Headphones",
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
                    ImageUrl = "https://via.placeholder.com/300?text=USB-C+Hub",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                // Clothing
                new Product
                {
                    Name = "Cotton T-Shirt",
                    Description = "100% organic cotton comfortable t-shirt available in multiple colors",
                    Price = 24.99m,
                    Stock = 200,
                    Category = ProductCategory.Clothing,
                    ImageUrl = "https://via.placeholder.com/300?text=Cotton+T-Shirt",
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
                    ImageUrl = "https://via.placeholder.com/300?text=Denim+Jeans",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                // Books
                new Product
                {
                    Name = "C# Programming Guide",
                    Description = "Comprehensive guide to C# programming with practical examples",
                    Price = 59.99m,
                    Stock = 30,
                    Category = ProductCategory.Books,
                    ImageUrl = "https://via.placeholder.com/300?text=CSharp+Guide",
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
                    ImageUrl = "https://via.placeholder.com/300?text=Web+Dev+Basics",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                // Home and Garden
                new Product
                {
                    Name = "Plant Pot Set",
                    Description = "Set of 5 ceramic plant pots with drainage holes",
                    Price = 34.99m,
                    Stock = 60,
                    Category = ProductCategory.HomeAndGarden,
                    ImageUrl = "https://via.placeholder.com/300?text=Plant+Pots",
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
                    ImageUrl = "https://via.placeholder.com/300?text=Garden+Tools",
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
                    ImageUrl = "https://via.placeholder.com/300?text=Running+Shoes",
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
                    ImageUrl = "https://via.placeholder.com/300?text=Yoga+Mat",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();
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
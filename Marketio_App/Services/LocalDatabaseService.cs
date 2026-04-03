using Marketio_Shared.DTOs;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Marketio_App.Services
{
    public class LocalDatabaseService
    {
        private SQLiteAsyncConnection? _db;

        [Table("Products")]
        private class LocalProduct
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public int Category { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        [Table("Orders")]
        private class LocalOrder
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string OrderNumber { get; set; } = string.Empty;
            public string CustomerId { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; }
            public int Status { get; set; }
            public int PaymentMethod { get; set; }
            public decimal TotalAmount { get; set; }
            public string ShippingAddress { get; set; } = string.Empty;
            public string OrderItemsJson { get; set; } = string.Empty;
        }

        public async Task InitializeAsync()
        {
            if (_db != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "marketio.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<LocalProduct>();
            await _db.CreateTableAsync<LocalOrder>();
        }

        private static LocalProduct ToLocal(ProductDto p) => new LocalProduct
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            Category = (int)p.Category,
            CategoryName = p.CategoryName,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive
        };

        private static ProductDto FromLocal(LocalProduct p) => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            Category = (Marketio_Shared.Enums.ProductCategory)p.Category,
            CategoryName = p.CategoryName,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive
        };

        private static LocalOrder ToLocal(OrderDto o) => new LocalOrder
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            OrderDate = o.OrderDate,
            Status = (int)o.Status,
            PaymentMethod = (int)o.PaymentMethod,
            TotalAmount = o.TotalAmount,
            ShippingAddress = o.ShippingAddress,
            OrderItemsJson = System.Text.Json.JsonSerializer.Serialize(o.OrderItems)
        };

        private static OrderDto FromLocal(LocalOrder o) => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            OrderDate = o.OrderDate,
            Status = (Marketio_Shared.Enums.OrderStatus)o.Status,
            StatusName = ((Marketio_Shared.Enums.OrderStatus)o.Status).ToString(),
            PaymentMethod = (Marketio_Shared.Enums.PaymentMethod)o.PaymentMethod,
            PaymentMethodName = ((Marketio_Shared.Enums.PaymentMethod)o.PaymentMethod).ToString(),
            TotalAmount = o.TotalAmount,
            ShippingAddress = o.ShippingAddress,
            OrderItems = string.IsNullOrEmpty(o.OrderItemsJson)
                ? new List<OrderItemDto>()
                : System.Text.Json.JsonSerializer.Deserialize<List<OrderItemDto>>(o.OrderItemsJson) ?? new List<OrderItemDto>()
        };

        public async Task SaveProductsAsync(IEnumerable<ProductDto> products)
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return;

            var locals = products.Select(ToLocal).ToList();

            await _db.RunInTransactionAsync(conn =>
            {
                foreach (var lp in locals)
                {
                    conn.InsertOrReplace(lp);
                }
            });
        }

        public async Task SaveProductAsync(ProductDto product)
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return;

            var lp = ToLocal(product);
            await _db.InsertOrReplaceAsync(lp);
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return new List<ProductDto>();

            var list = await _db.Table<LocalProduct>().ToListAsync();
            return list.Select(FromLocal).ToList();
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return null;

            var p = await _db.FindAsync<LocalProduct>(id);
            return p == null ? null : FromLocal(p);
        }

        public async Task SaveOrdersAsync(IEnumerable<OrderDto> orders)
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return;

            var locals = orders.Select(ToLocal).ToList();

            await _db.RunInTransactionAsync(conn =>
            {
                foreach (var lo in locals)
                {
                    conn.InsertOrReplace(lo);
                }
            });
        }

        public async Task SaveOrderAsync(OrderDto order)
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return;

            var lo = ToLocal(order);
            await _db.InsertOrReplaceAsync(lo);
        }

        public async Task<List<OrderDto>> GetOrdersAsync()
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return new List<OrderDto>();

            var list = await _db.Table<LocalOrder>().OrderByDescending(o => o.OrderDate).ToListAsync();
            return list.Select(FromLocal).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return null;

            var o = await _db.FindAsync<LocalOrder>(id);
            return o == null ? null : FromLocal(o);
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            if (_db == null)
                await InitializeAsync();

            if (_db == null)
                return;

            await _db.DeleteAsync<LocalOrder>(orderId);
        }
    }
}
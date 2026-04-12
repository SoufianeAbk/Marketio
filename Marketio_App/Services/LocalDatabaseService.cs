using Marketio_Shared.DTOs;
using SQLite;
using System.Text.Json;

namespace Marketio_App.Services
{
    public class LocalDatabaseService
    {
        private SQLiteAsyncConnection? _db;

        // ─── Local entities ────────────────────────────────────────────────────────

        [Table("Products")]
        private class LocalProduct
        {
            [PrimaryKey] public int Id { get; set; }
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
            [PrimaryKey] public int Id { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
            public string CustomerId { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; }
            public int Status { get; set; }
            public int PaymentMethod { get; set; }
            public decimal TotalAmount { get; set; }
            public string ShippingAddress { get; set; } = string.Empty;
            public string OrderItemsJson { get; set; } = string.Empty;
        }

        [Table("PendingOrders")]
        public class PendingOrder
        {
            [PrimaryKey, AutoIncrement]
            public int LocalId { get; set; }
            public string CreateOrderJson { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; } = DateTime.Now;
        }

        // ─── Init ─────────────────────────────────────────────────────────────────

        public async Task InitializeAsync()
        {
            if (_db != null)
                return;

            var dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "marketio.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<LocalProduct>();
            await _db.CreateTableAsync<LocalOrder>();
            await _db.CreateTableAsync<PendingOrder>();
        }

        // ─── Products ─────────────────────────────────────────────────────────────

        private static LocalProduct ToLocal(ProductDto p) => new()
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

        private static ProductDto FromLocal(LocalProduct p) => new()
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

        public async Task SaveProductsAsync(IEnumerable<ProductDto> products)
        {
            if (_db == null) await InitializeAsync();
            var locals = products.Select(ToLocal).ToList();
            await _db!.RunInTransactionAsync(conn =>
            {
                foreach (var lp in locals) conn.InsertOrReplace(lp);
            });
        }

        public async Task SaveProductAsync(ProductDto product)
        {
            if (_db == null) await InitializeAsync();
            await _db!.InsertOrReplaceAsync(ToLocal(product));
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            if (_db == null) await InitializeAsync();
            var list = await _db!.Table<LocalProduct>().ToListAsync();
            return list.Select(FromLocal).ToList();
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            if (_db == null) await InitializeAsync();
            var p = await _db!.FindAsync<LocalProduct>(id);
            return p == null ? null : FromLocal(p);
        }

        // ─── Orders ───────────────────────────────────────────────────────────────

        private static LocalOrder ToLocal(OrderDto o) => new()
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            OrderDate = o.OrderDate,
            Status = (int)o.Status,
            PaymentMethod = (int)o.PaymentMethod,
            TotalAmount = o.TotalAmount,
            ShippingAddress = o.ShippingAddress,
            OrderItemsJson = JsonSerializer.Serialize(o.OrderItems)
        };

        private static OrderDto FromLocal(LocalOrder o) => new()
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
                : JsonSerializer.Deserialize<List<OrderItemDto>>(o.OrderItemsJson) ?? new List<OrderItemDto>()
        };

        public async Task SaveOrdersAsync(IEnumerable<OrderDto> orders)
        {
            if (_db == null) await InitializeAsync();
            var locals = orders.Select(ToLocal).ToList();
            await _db!.RunInTransactionAsync(conn =>
            {
                foreach (var lo in locals) conn.InsertOrReplace(lo);
            });
        }

        public async Task SaveOrderAsync(OrderDto order)
        {
            if (_db == null) await InitializeAsync();
            await _db!.InsertOrReplaceAsync(ToLocal(order));
        }

        public async Task<List<OrderDto>> GetOrdersAsync()
        {
            if (_db == null) await InitializeAsync();
            var list = await _db!.Table<LocalOrder>().OrderByDescending(o => o.OrderDate).ToListAsync();
            return list.Select(FromLocal).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            if (_db == null) await InitializeAsync();
            var o = await _db!.FindAsync<LocalOrder>(id);
            return o == null ? null : FromLocal(o);
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            if (_db == null) await InitializeAsync();
            await _db!.DeleteAsync<LocalOrder>(orderId);
        }

        // ─── Pending Orders (offline queue) ───────────────────────────────────────

        /// <summary>
        /// Slaat een offline bestelling op in de wachtrij. Geeft het lokale ID terug.
        /// </summary>
        public async Task<int> SavePendingOrderAsync(CreateOrderDto createOrderDto)
        {
            if (_db == null) await InitializeAsync();

            var pending = new PendingOrder
            {
                CreateOrderJson = JsonSerializer.Serialize(createOrderDto),
                CreatedAt = DateTime.Now
            };

            await _db!.InsertAsync(pending);
            return pending.LocalId;
        }

        public async Task<List<PendingOrder>> GetPendingOrdersAsync()
        {
            if (_db == null) await InitializeAsync();
            return await _db!.Table<PendingOrder>().OrderBy(p => p.LocalId).ToListAsync();
        }

        public async Task DeletePendingOrderAsync(int localId)
        {
            if (_db == null) await InitializeAsync();
            await _db!.DeleteAsync<PendingOrder>(localId);
        }

        public async Task<int> GetPendingOrderCountAsync()
        {
            if (_db == null) await InitializeAsync();
            return await _db!.Table<PendingOrder>().CountAsync();
        }
    }
}
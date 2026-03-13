using Marketio_Shared.DTOs;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Marketio_App.Services
{
    internal class LocalDatabaseService
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

        public async Task InitializeAsync()
        {
            if (_db != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "marketio.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<LocalProduct>();
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
    }
}
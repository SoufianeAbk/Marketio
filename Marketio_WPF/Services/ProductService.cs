using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_WPF.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marketio_WPF.Services
{
    internal class ProductService
    {
        private readonly MarketioDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(MarketioDbContext context, ILogger<ProductService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<dynamic>> GetAllProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .IgnoreQueryFilters()   // admin ziet ook inactieve producten
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .AsNoTracking()
                    .ToListAsync();

                return products.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                throw new InvalidOperationException("Error retrieving products.", ex);
            }
        }

        public async Task<List<dynamic>> GetProductsByCategoryAsync(ProductCategory category)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.Category == category)
                    .OrderBy(p => p.Name)
                    .AsNoTracking()
                    .ToListAsync();

                return products.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category: {Category}", category);
                throw new InvalidOperationException("Error retrieving products by category.", ex);
            }
        }

        public async Task<dynamic?> GetProductByIdAsync(int productId)
        {
            if (productId <= 0) return null;

            try
            {
                return await _context.Products
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", productId);
                throw new InvalidOperationException("Error retrieving product.", ex);
            }
        }

        public async Task<bool> CreateProductAsync(dynamic productData)
        {
            if (productData == null) return false;

            try
            {
                var product = new Product
                {
                    Name = (string)productData.Name,
                    Description = (string)productData.Description,
                    Price = (decimal)productData.Price,
                    Stock = (int)productData.Stock,
                    Category = (ProductCategory)productData.Category,
                    ImageUrl = (string)productData.ImageUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product '{Name}' created (Id={Id})", product.Name, product.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw new InvalidOperationException("Error creating product.", ex);
            }
        }

        public async Task<bool> UpdateProductAsync(int productId, dynamic productData)
        {
            if (productId <= 0 || productData == null) return false;

            try
            {
                var product = await _context.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null) return false;

                product.Name = (string)productData.Name;
                product.Description = (string)productData.Description;
                product.Price = (decimal)productData.Price;
                product.Stock = (int)productData.Stock;
                product.Category = (ProductCategory)productData.Category;
                product.ImageUrl = (string)productData.ImageUrl;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product {ProductId} updated", productId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", productId);
                throw new InvalidOperationException("Error updating product.", ex);
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            if (productId <= 0) return false;

            try
            {
                var product = await _context.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null) return false;

                // Soft-delete: IsActive = false
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Product {ProductId} soft-deleted", productId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", productId);
                throw new InvalidOperationException("Error deleting product.", ex);
            }
        }

        public async Task<List<dynamic>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<dynamic>();

            try
            {
                var term = searchTerm.Trim().ToLower();

                var products = await _context.Products
                    .IgnoreQueryFilters()
                    .Where(p => p.Name.ToLower().Contains(term) ||
                                p.Description.ToLower().Contains(term))
                    .OrderBy(p => p.Name)
                    .AsNoTracking()
                    .ToListAsync();

                return products.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products: {SearchTerm}", searchTerm);
                throw new InvalidOperationException("Error searching products.", ex);
            }
        }
    }
}
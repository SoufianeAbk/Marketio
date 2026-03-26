using Marketio_Shared.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketio_App.Services
{
    public class ProductApiService
    {
        private readonly ApiService _api;
        private readonly LocalDatabaseService _localDb;
        private readonly ConnectivityService _connectivity;
        private readonly ILogger<ProductApiService> _logger;

        public ProductApiService(
            ApiService api,
            LocalDatabaseService localDb,
            ConnectivityService connectivity,
            ILogger<ProductApiService> logger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _localDb = localDb ?? throw new ArgumentNullException(nameof(localDb));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Converts relative image URLs to absolute URLs for MAUI Image control compatibility.
        /// MAUI Image control requires full URLs (http/https), not relative paths.
        /// </summary>
        private string ConvertImageUrlToAbsolute(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return imageUrl;

            // If it's already an absolute URL (http/https), return as-is
            if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return imageUrl;
            }

            // If it's a relative path, prepend the API base URL
            if (imageUrl.StartsWith("/"))
            {
                var baseUrl = _api.BaseAddress?.ToString().TrimEnd('/');
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    return $"{baseUrl}{imageUrl}";
                }
            }

            return imageUrl;
        }

        /// <summary>
        /// Processes a collection of products to ensure image URLs are absolute
        /// </summary>
        private IEnumerable<ProductDto> ProcessProductImages(IEnumerable<ProductDto> products)
        {
            if (products == null)
                return products;

            foreach (var product in products)
            {
                if (product != null && !string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    product.ImageUrl = ConvertImageUrlToAbsolute(product.ImageUrl);
                    _logger.LogDebug("[ProductApiService] Converted ImageUrl for '{Name}': {ImageUrl}",
                        product.Name, product.ImageUrl);
                }
            }

            return products;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            _logger.LogInformation("[ProductApiService] GetAllProductsAsync called. IsConnected={Connected}", _connectivity.IsConnected);

            if (_connectivity.IsConnected)
            {
                try
                {
                    _logger.LogDebug("[ProductApiService] Fetching from API: api/products");
                    var products = await _api.GetAsync<IEnumerable<ProductDto>>("api/products");

                    if (products != null)
                    {
                        var productList = products.ToList();

                        // Convert image URLs to absolute paths for MAUI compatibility
                        productList = ProcessProductImages(productList).ToList();

                        _logger.LogInformation("[ProductApiService] ✓ Got {Count} products from API", productList.Count);

                        // Cache them
                        try
                        {
                            await _localDb.SaveProductsAsync(productList);
                            _logger.LogInformation("[ProductApiService] ✓ Cached {Count} products to LocalDB", productList.Count);
                        }
                        catch (Exception cacheEx)
                        {
                            _logger.LogWarning(cacheEx, "[ProductApiService] Failed to cache products, but returning API data");
                        }

                        return productList;
                    }
                    else
                    {
                        _logger.LogWarning("[ProductApiService] API returned null");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ProductApiService] API request failed, falling back to cache");
                }
            }
            else
            {
                _logger.LogInformation("[ProductApiService] No internet connection, using cached data");
            }

            // Fallback to cached products
            try
            {
                var cachedProducts = await _localDb.GetProductsAsync();
                _logger.LogInformation("[ProductApiService] Returning {Count} cached products", cachedProducts.Count);
                return ProcessProductImages(cachedProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductApiService] Failed to get cached products");
                return new List<ProductDto>();
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            _logger.LogInformation("[ProductApiService] GetProductByIdAsync called for ID={Id}", id);

            if (_connectivity.IsConnected)
            {
                try
                {
                    _logger.LogDebug("[ProductApiService] Fetching from API: api/products/{Id}", id);
                    var product = await _api.GetAsync<ProductDto>($"api/products/{id}");

                    if (product != null)
                    {
                        // Convert image URL to absolute path for MAUI compatibility
                        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                        {
                            product.ImageUrl = ConvertImageUrlToAbsolute(product.ImageUrl);
                            _logger.LogDebug("[ProductApiService] Converted ImageUrl: {ImageUrl}", product.ImageUrl);
                        }

                        _logger.LogInformation("[ProductApiService] ✓ Got product from API: {Name}", product.Name);

                        try
                        {
                            await _localDb.SaveProductAsync(product);
                            _logger.LogInformation("[ProductApiService] ✓ Cached product to LocalDB");
                        }
                        catch (Exception cacheEx)
                        {
                            _logger.LogWarning(cacheEx, "[ProductApiService] Failed to cache product, but returning API data");
                        }

                        return product;
                    }
                    else
                    {
                        _logger.LogWarning("[ProductApiService] API returned null for product ID={Id}", id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ProductApiService] API request failed for ID={Id}, falling back to cache", id);
                }
            }
            else
            {
                _logger.LogInformation("[ProductApiService] No internet connection, using cached product");
            }

            // Fallback to cached product
            try
            {
                var cachedProduct = await _localDb.GetProductByIdAsync(id);
                if (cachedProduct != null && !string.IsNullOrWhiteSpace(cachedProduct.ImageUrl))
                {
                    cachedProduct.ImageUrl = ConvertImageUrlToAbsolute(cachedProduct.ImageUrl);
                }
                _logger.LogInformation("[ProductApiService] Returning cached product: {Name}", cachedProduct?.Name ?? "NOT FOUND");
                return cachedProduct;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductApiService] Failed to get cached product");
                return null;
            }
        }
    }
}
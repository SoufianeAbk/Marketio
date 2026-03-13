using Marketio_Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketio_App.Services
{
    internal class ProductApiService
    {
        private readonly ApiService _api;
        private readonly LocalDatabaseService _localDb;
        private readonly ConnectivityService _connectivity;

        public ProductApiService(ApiService api, LocalDatabaseService localDb, ConnectivityService connectivity)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _localDb = localDb ?? throw new ArgumentNullException(nameof(localDb));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            if (_connectivity.IsConnected)
            {
                try
                {
                    var products = await _api.GetAsync<IEnumerable<ProductDto>>("api/products");
                    if (products != null)
                    {
                        await _localDb.SaveProductsAsync(products);
                        return products;
                    }
                }
                catch
                {
                    // fall back to cache
                }
            }

            return await _localDb.GetProductsAsync();
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            if (_connectivity.IsConnected)
            {
                try
                {
                    var product = await _api.GetAsync<ProductDto>($"api/products/{id}");
                    if (product != null)
                    {
                        await _localDb.SaveProductAsync(product);
                        return product;
                    }
                }
                catch
                {
                    // fall back to cache
                }
            }

            return await _localDb.GetProductByIdAsync(id);
        }
    }
}
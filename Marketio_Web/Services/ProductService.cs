using System.Globalization;
using Marketio_Shared.DTOs;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Marketio_Web.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(IProductRepository productRepository, IHttpContextAccessor httpContextAccessor)
        {
            _productRepository = productRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var locale = GetCurrentLocale();
            var products = await _productRepository.GetAllAsync();
            return products.Select(p => MapToDto(p, locale));
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            var locale = GetCurrentLocale();
            var product = await _productRepository.GetByIdAsync(productId);
            return product != null ? MapToDto(product, locale) : null;
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(ProductCategory category)
        {
            var locale = GetCurrentLocale();
            var products = await _productRepository.GetByCategoryAsync(category);
            return products.Select(p => MapToDto(p, locale));
        }

        public async Task<ProductDto> CreateProductAsync(ProductDto productDto)
        {
            var product = MapToEntity(productDto);
            var createdProduct = await _productRepository.AddAsync(product);
            return MapToDto(createdProduct, GetCurrentLocale());
        }

        public async Task UpdateProductAsync(ProductDto productDto)
        {
            var product = MapToEntity(productDto);
            await _productRepository.UpdateAsync(product);
        }

        public async Task DeleteProductAsync(int productId)
        {
            await _productRepository.DeleteAsync(productId);
        }

        // ── Hulpmethoden ────────────────────────────────────────────────────────

        /// <summary>
        /// Geeft de twee-letterige taalcode op basis van de actieve UI-cultuur.
        /// Valt terug op "en" als de taal niet ondersteund is.
        /// </summary>
        private string GetCurrentLocale()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return lang is "nl" or "fr" or "en" ? lang : "en";
        }

        private static ProductDto MapToDto(Product product, string locale)
        {
            // Zoek vertaling voor de huidige taal; val terug op de Engelse basistekst
            var translation = product.Translations
                .FirstOrDefault(t => t.Locale == locale);

            return new ProductDto
            {
                Id = product.Id,
                Name = translation?.Name ?? product.Name,
                Description = translation?.Description ?? product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                CategoryName = product.Category.ToString(),
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive
            };
        }

        private static Product MapToEntity(ProductDto dto)
        {
            return new Product
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                Category = dto.Category,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive
            };
        }
    }
}
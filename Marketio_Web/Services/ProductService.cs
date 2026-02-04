using Marketio_Shared.DTOs;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;

namespace Marketio_Web.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product != null ? MapToDto(product) : null;
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(ProductCategory category)
        {
            var products = await _productRepository.GetByCategoryAsync(category);
            return products.Select(MapToDto);
        }

        public async Task<ProductDto> CreateProductAsync(ProductDto productDto)
        {
            var product = MapToEntity(productDto);
            var createdProduct = await _productRepository.AddAsync(product);
            return MapToDto(createdProduct);
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

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
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
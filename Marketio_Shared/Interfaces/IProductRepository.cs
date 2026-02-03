using Marketio_Shared.Entities;
using Marketio_Shared.Enums;

namespace Marketio_Shared.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetByCategoryAsync(ProductCategory category);
        Task<IEnumerable<Product>> GetActiveProductsAsync();
        Task<bool> UpdateStockAsync(int productId, int quantity);
    }
}
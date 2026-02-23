using Marketio_Shared.DTOs;

namespace Marketio_Shared.Interfaces
{
    public interface ICartService
    {
        Task<List<CartItemDto>> GetCartItemsAsync();
        Task AddToCartAsync(int productId, int quantity = 1);
        Task UpdateCartItemAsync(int productId, int quantity);
        Task RemoveFromCartAsync(int productId);
        Task ClearCartAsync();
        Task<decimal> GetCartTotalAsync();
        Task<int> GetCartItemCountAsync();
    }
}
using System.Text.Json;
using Marketio_Shared.DTOs;

namespace Marketio_App.Services
{
    public class CartService
    {
        private const string CartPreferenceKey = "MarketioCart";
        private readonly LocalDatabaseService _database;

        public CartService(LocalDatabaseService database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<List<CartItemDto>> GetCartItemsAsync()
        {
            var cartJson = await SecureStorage.Default.GetAsync(CartPreferenceKey);

            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItemDto>();

            try
            {
                return JsonSerializer.Deserialize<List<CartItemDto>>(cartJson) ?? new List<CartItemDto>();
            }
            catch
            {
                return new List<CartItemDto>();
            }
        }

        public async Task AddToCartAsync(ProductDto product, int quantity = 1)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0");

            if (quantity > product.Stock)
                throw new InvalidOperationException($"Quantity exceeds available stock ({product.Stock})");

            var cartItems = await GetCartItemsAsync();
            var existingItem = cartItems.FirstOrDefault(x => x.ProductId == product.Id);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + quantity;
                if (newQuantity > product.Stock)
                    throw new InvalidOperationException($"Total quantity exceeds available stock ({product.Stock})");

                existingItem.Quantity = newQuantity;
            }
            else
            {
                cartItems.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    UnitPrice = product.Price,
                    Quantity = quantity,
                    AvailableStock = product.Stock
                });
            }

            await SaveCartAsync(cartItems);
        }

        public async Task UpdateCartItemAsync(int productId, int quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Quantity cannot be negative");

            if (quantity == 0)
            {
                await RemoveFromCartAsync(productId);
                return;
            }

            var cartItems = await GetCartItemsAsync();
            var item = cartItems.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                if (quantity > item.AvailableStock)
                    throw new InvalidOperationException($"Quantity exceeds available stock ({item.AvailableStock})");

                item.Quantity = quantity;
                await SaveCartAsync(cartItems);
            }
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            var cartItems = await GetCartItemsAsync();
            cartItems.RemoveAll(x => x.ProductId == productId);
            await SaveCartAsync(cartItems);
        }

        public async Task ClearCartAsync()
        {
            try
            {
                SecureStorage.Default.Remove(CartPreferenceKey);
            }
            catch
            {
                // Ignore errors if key doesn't exist
            }

            await Task.CompletedTask;
        }

        public async Task<decimal> GetCartTotalAsync()
        {
            var cartItems = await GetCartItemsAsync();
            return cartItems.Sum(x => x.TotalPrice);
        }

        public async Task<int> GetCartItemCountAsync()
        {
            var cartItems = await GetCartItemsAsync();
            return cartItems.Sum(x => x.Quantity);
        }

        private async Task SaveCartAsync(List<CartItemDto> cartItems)
        {
            var cartJson = JsonSerializer.Serialize(cartItems);
            await SecureStorage.Default.SetAsync(CartPreferenceKey, cartJson);
        }
    }
}
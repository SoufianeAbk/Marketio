using Marketio_Shared.DTOs;

namespace Marketio_App.Services
{
    public class CartService
    {
        private readonly LocalDatabaseService _database;

        public CartService(LocalDatabaseService database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<List<CartItemDto>> GetCartItemsAsync()
        {
            return await _database.GetCartItemsAsync();
        }

        public async Task AddToCartAsync(ProductDto product, int quantity = 1)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0");

            if (quantity > product.Stock)
                throw new InvalidOperationException($"Quantity exceeds available stock ({product.Stock})");

            var cartItems = await _database.GetCartItemsAsync();
            var existingItem = cartItems.FirstOrDefault(x => x.ProductId == product.Id);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + quantity;
                if (newQuantity > product.Stock)
                    throw new InvalidOperationException($"Total quantity exceeds available stock ({product.Stock})");

                existingItem.Quantity = newQuantity;
                await _database.UpsertCartItemAsync(existingItem);
            }
            else
            {
                await _database.UpsertCartItemAsync(new CartItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    UnitPrice = product.Price,
                    Quantity = quantity,
                    AvailableStock = product.Stock
                });
            }
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

            var cartItems = await _database.GetCartItemsAsync();
            var item = cartItems.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                if (quantity > item.AvailableStock)
                    throw new InvalidOperationException($"Quantity exceeds available stock ({item.AvailableStock})");

                item.Quantity = quantity;
                await _database.UpsertCartItemAsync(item);
            }
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            await _database.DeleteCartItemAsync(productId);
        }

        public async Task ClearCartAsync()
        {
            await _database.ClearCartAsync();
        }

        public async Task<decimal> GetCartTotalAsync()
        {
            var cartItems = await _database.GetCartItemsAsync();
            return cartItems.Sum(x => x.TotalPrice);
        }

        public async Task<int> GetCartItemCountAsync()
        {
            var cartItems = await _database.GetCartItemsAsync();
            return cartItems.Sum(x => x.Quantity);
        }
    }
}
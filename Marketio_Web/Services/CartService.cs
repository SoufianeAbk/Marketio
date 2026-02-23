using Marketio_Shared.DTOs;
using Marketio_Shared.Interfaces;
using System.Text.Json;

namespace Marketio_Web.Services
{
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;
        private const string CartSessionKey = "ShoppingCart";

        public CartService(IHttpContextAccessor httpContextAccessor, IProductService productService)
        {
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session is not available");

        public async Task<List<CartItemDto>> GetCartItemsAsync()
        {
            var cartJson = Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItemDto>();

            var cartItems = JsonSerializer.Deserialize<List<CartItemDto>>(cartJson) ?? new List<CartItemDto>();

            // Verifieer voorraad en prijzen
            foreach (var item in cartItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product != null)
                {
                    item.UnitPrice = product.Price;
                    item.AvailableStock = product.Stock;
                }
            }

            SaveCart(cartItems);
            return cartItems;
        }

        public async Task AddToCartAsync(int productId, int quantity = 1)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.IsActive || product.Stock < quantity)
                throw new InvalidOperationException("Product niet beschikbaar");

            var cartItems = await GetCartItemsAsync();
            var existingItem = cartItems.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + quantity;
                if (newQuantity > product.Stock)
                    throw new InvalidOperationException($"Maximale voorraad ({product.Stock}) bereikt");

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

            SaveCart(cartItems);
        }

        public async Task UpdateCartItemAsync(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                await RemoveFromCartAsync(productId);
                return;
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || quantity > product.Stock)
                throw new InvalidOperationException("Ongeldige hoeveelheid");

            var cartItems = await GetCartItemsAsync();
            var item = cartItems.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cartItems);
            }
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            var cartItems = await GetCartItemsAsync();
            cartItems.RemoveAll(x => x.ProductId == productId);
            SaveCart(cartItems);
        }

        public Task ClearCartAsync()
        {
            Session.Remove(CartSessionKey);
            return Task.CompletedTask;
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

        private void SaveCart(List<CartItemDto> cartItems)
        {
            var cartJson = JsonSerializer.Serialize(cartItems);
            Session.SetString(CartSessionKey, cartJson);
        }
    }
}
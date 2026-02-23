using Marketio_Shared.DTOs;
using Marketio_Shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketio_Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;

        public CartController(ICartService cartService, IProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var cartItems = await _cartService.GetCartItemsAsync();
            ViewBag.CartTotal = await _cartService.GetCartTotalAsync();
            return View(cartItems);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                await _cartService.AddToCartAsync(productId, quantity);
                TempData["Success"] = "Product toegevoegd aan winkelwagen!";

                // ✅ Verbeterde redirect logica
                var referer = Request.Headers["Referer"].ToString();

                // Als van product index pagina, blijf daar
                if (!string.IsNullOrEmpty(referer) && referer.Contains("/Products") && !referer.Contains("/Details"))
                {
                    return Redirect(referer);
                }

                // Anders naar product details
                return RedirectToAction("Details", "Products", new { id = productId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                // Bij fout ook terug naar oorspronkelijke pagina
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer) && referer.Contains("/Products"))
                {
                    return Redirect(referer);
                }

                return RedirectToAction("Details", "Products", new { id = productId });
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            try
            {
                await _cartService.UpdateCartItemAsync(productId, quantity);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            try
            {
                await _cartService.RemoveFromCartAsync(productId);
                TempData["Success"] = "Product verwijderd uit winkelwagen";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync();
            TempData["Success"] = "Winkelwagen is geleegd";
            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/Checkout - ✅ Beschermd met [Authorize]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cartItems = await _cartService.GetCartItemsAsync();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Uw winkelwagen is leeg";
                return RedirectToAction(nameof(Index));
            }

            // ✅ Automatisch gebruiker info ophalen
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var createOrderDto = new CreateOrderDto
            {
                CustomerId = userId ?? userEmail ?? "", // Gebruik user ID of email
                OrderItems = cartItems.Select(item => new CreateOrderItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList()
            };

            ViewBag.CartTotal = await _cartService.GetCartTotalAsync();
            ViewBag.UserEmail = userEmail;
            return View(createOrderDto);
        }

        // POST: Cart/PlaceOrder - ✅ Beschermd met [Authorize]
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CreateOrderDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CartTotal = await _cartService.GetCartTotalAsync();
                ViewBag.UserEmail = User.FindFirstValue(ClaimTypes.Email);
                return View("Checkout", model);
            }

            try
            {
                // ✅ Zorg dat CustomerId altijd gezet is
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                model.CustomerId = userId ?? userEmail ?? model.CustomerId;

                // Haal cart items op en voeg toe aan model
                var cartItems = await _cartService.GetCartItemsAsync();
                model.OrderItems = cartItems.Select(item => new CreateOrderItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList();

                // Maak bestelling aan via OrderService
                var orderService = HttpContext.RequestServices.GetRequiredService<IOrderService>();
                var order = await orderService.CreateOrderAsync(model);

                if (order != null)
                {
                    await _cartService.ClearCartAsync();
                    TempData["Success"] = $"Bestelling {order.OrderNumber} succesvol geplaatst!";
                    return RedirectToAction("Details", "Orders", new { id = order.Id });
                }

                TempData["Error"] = "Kon bestelling niet plaatsen. Controleer voorraad.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Fout bij plaatsen bestelling: {ex.Message}";
                ViewBag.CartTotal = await _cartService.GetCartTotalAsync();
                ViewBag.UserEmail = User.FindFirstValue(ClaimTypes.Email);
                return View("Checkout", model);
            }
        }

        // GET: Cart/GetCartCount - API endpoint voor cart count (gebruikt in layout)
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var count = await _cartService.GetCartItemCountAsync();
            return Json(new { count });
        }
    }
}
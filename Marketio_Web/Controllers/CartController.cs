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

        // POST: Cart/AddToCart - ✅ AJAX Compatible
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                await _cartService.AddToCartAsync(productId, quantity);

                // ✅ AJAX Response
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var cartCount = await _cartService.GetCartItemCountAsync();
                    var cartTotal = await _cartService.GetCartTotalAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Product toegevoegd aan winkelwagen!",
                        cartCount = cartCount,
                        cartTotal = cartTotal
                    });
                }

                TempData["Success"] = "Product toegevoegd aan winkelwagen!";

                // Normale redirect voor non-AJAX
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer) && referer.Contains("/Products") && !referer.Contains("/Details"))
                {
                    return Redirect(referer);
                }

                return RedirectToAction("Details", "Products", new { id = productId });
            }
            catch (Exception ex)
            {
                // ✅ AJAX Error Response
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

                TempData["Error"] = ex.Message;

                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer) && referer.Contains("/Products"))
                {
                    return Redirect(referer);
                }

                return RedirectToAction("Details", "Products", new { id = productId });
            }
        }

        // POST: Cart/UpdateQuantity - ✅ Already AJAX
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            try
            {
                await _cartService.UpdateCartItemAsync(productId, quantity);

                var cartTotal = await _cartService.GetCartTotalAsync();
                var cartCount = await _cartService.GetCartItemCountAsync();
                var cartItems = await _cartService.GetCartItemsAsync();
                var updatedItem = cartItems.FirstOrDefault(x => x.ProductId == productId);

                return Json(new
                {
                    success = true,
                    cartTotal = cartTotal,
                    cartCount = cartCount,
                    itemTotal = updatedItem?.TotalPrice ?? 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Cart/RemoveItem - ✅ AJAX Compatible
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            try
            {
                await _cartService.RemoveFromCartAsync(productId);

                // ✅ AJAX Response
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var cartCount = await _cartService.GetCartItemCountAsync();
                    var cartTotal = await _cartService.GetCartTotalAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Product verwijderd uit winkelwagen",
                        cartCount = cartCount,
                        cartTotal = cartTotal
                    });
                }

                TempData["Success"] = "Product verwijderd uit winkelwagen";
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }

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

        // GET: Cart/Checkout
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cartItems = await _cartService.GetCartItemsAsync();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Uw winkelwagen is leeg";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var createOrderDto = new CreateOrderDto
            {
                CustomerId = userId ?? userEmail ?? "",
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

        // POST: Cart/PlaceOrder
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                model.CustomerId = userId ?? userEmail ?? model.CustomerId;

                var cartItems = await _cartService.GetCartItemsAsync();
                model.OrderItems = cartItems.Select(item => new CreateOrderItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList();

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

        // GET: Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var count = await _cartService.GetCartItemCountAsync();
            return Json(new { count });
        }

        // ✅ NEW: Get Cart Summary (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetCartSummary()
        {
            var cartItems = await _cartService.GetCartItemsAsync();
            var cartTotal = await _cartService.GetCartTotalAsync();
            var cartCount = await _cartService.GetCartItemCountAsync();

            return Json(new
            {
                success = true,
                items = cartItems,
                total = cartTotal,
                count = cartCount
            });
        }
    }
}
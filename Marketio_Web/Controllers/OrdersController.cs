using Marketio_Shared.DTOs;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Marketio_Web.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            IProductService productService,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _productService = productService;
            _logger = logger;
        }

        // GET: Orders
        public IActionResult Index()
        {
            // Placeholder voor toekomstige implementatie met customer filtering
            return View();
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for ID: {OrderId}", id);
                TempData["Error"] = "Er is een fout opgetreden bij het laden van de bestelling.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products.Where(p => p.IsActive && p.Stock > 0).ToList();
                ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>();

                return View(new CreateOrderDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create order page");
                TempData["Error"] = "Er is een fout opgetreden bij het laden van de pagina.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderDto createOrderDto)
        {
            if (!ModelState.IsValid)
            {
                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products.Where(p => p.IsActive && p.Stock > 0).ToList();
                ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>();
                return View(createOrderDto);
            }

            try
            {
                var order = await _orderService.CreateOrderAsync(createOrderDto);
                if (order == null)
                {
                    ModelState.AddModelError("", "Er is een fout opgetreden bij het aanmaken van de bestelling.");
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = products.Where(p => p.IsActive && p.Stock > 0).ToList();
                    ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>();
                    return View(createOrderDto);
                }

                TempData["Success"] = $"Bestelling {order.OrderNumber} succesvol aangemaakt!";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during order creation");
                ModelState.AddModelError("", ex.Message);
                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products.Where(p => p.IsActive && p.Stock > 0).ToList();
                ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>();
                return View(createOrderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ModelState.AddModelError("", "Er is een onverwachte fout opgetreden bij het aanmaken van de bestelling.");
                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products.Where(p => p.IsActive && p.Stock > 0).ToList();
                ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>();
                return View(createOrderDto);
            }
        }

        // GET: Orders/CustomerOrders?customerId={customerId}
        public async Task<IActionResult> CustomerOrders(string customerId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return BadRequest("Customer ID is vereist.");
            }

            try
            {
                var orders = await _orderService.GetCustomerOrdersAsync(customerId);
                ViewBag.CustomerId = customerId;
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer orders for ID: {CustomerId}", customerId);
                TempData["Error"] = "Er is een fout opgetreden bij het laden van de bestellingen.";
                return View(new List<OrderDto>());
            }
        }

        // POST: Orders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var result = await _orderService.CancelOrderAsync(id);
                if (result)
                {
                    TempData["Success"] = "Bestelling succesvol geannuleerd!";
                }
                else
                {
                    TempData["Error"] = "De bestelling kon niet worden geannuleerd.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order, ID: {OrderId}", id);
                TempData["Error"] = "Er is een fout opgetreden bij het annuleren van de bestelling.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
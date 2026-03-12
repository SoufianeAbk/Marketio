using Marketio_Shared.DTOs;
using Marketio_Shared.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/orders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OrdersApiController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(
            IOrderService orderService,
            ILogger<OrdersApiController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's orders
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var orders = await _orderService.GetCustomerOrdersAsync(userId);
            return Ok(orders);
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null)
                return NotFound(new { message = $"Order with ID {id} not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if user owns this order or is admin/manager
            if (order.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
                return Forbid();

            return Ok(order);
        }

        /// <summary>
        /// Create new order
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto createOrderDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            createOrderDto.CustomerId = userId;
            var created = await _orderService.CreateOrderAsync(createOrderDto);

            if (created == null)
                return BadRequest(new { message = "Failed to create order" });

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Cancel order
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null)
                return NotFound(new { message = $"Order with ID {id} not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if user owns this order or is admin/manager
            if (order.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
                return Forbid();

            var success = await _orderService.CancelOrderAsync(id);

            if (!success)
                return BadRequest(new { message = "Failed to cancel order" });

            return NoContent();
        }
    }
}
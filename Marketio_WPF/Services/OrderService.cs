using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_WPF.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marketio_WPF.Services
{
    internal class OrderService
    {
        private readonly MarketioDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(MarketioDbContext context, ILogger<OrderService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<dynamic>> GetAllOrdersAsync()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .AsNoTracking()
                    .ToListAsync();

                return orders.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                throw new InvalidOperationException("Error retrieving orders.", ex);
            }
        }

        public async Task<List<dynamic>> GetCustomerOrdersAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return new List<dynamic>();

            try
            {
                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customerId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .AsNoTracking()
                    .ToListAsync();

                return orders.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", customerId);
                throw new InvalidOperationException("Error retrieving customer orders.", ex);
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            if (orderId <= 0) return false;

            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                order.Status = newStatus;

                // Datumvelden bijhouden op basis van status
                if (newStatus == OrderStatus.Shipped && order.ShippedDate == null)
                    order.ShippedDate = DateTime.UtcNow;

                if (newStatus == OrderStatus.Delivered && order.DeliveredDate == null)
                    order.DeliveredDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status", orderId);
                throw new InvalidOperationException("Error updating order status.", ex);
            }
        }

        public async Task<dynamic?> GetOrderByIdAsync(int orderId)
        {
            if (orderId <= 0) return null;

            try
            {
                return await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                throw new InvalidOperationException("Error retrieving order.", ex);
            }
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            if (orderId <= 0) return false;

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                // OrderItems worden via Cascade verwijderd (zie DbContext config)
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Order {OrderId} deleted", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                throw new InvalidOperationException("Error deleting order.", ex);
            }
        }
    }
}
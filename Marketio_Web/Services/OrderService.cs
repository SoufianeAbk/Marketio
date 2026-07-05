using Marketio_Shared.Data;
using Marketio_Shared.DTOs;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Marketio_Web.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly MarketioDbContext _context;

        public OrderService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            MarketioDbContext context)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _context = context;
        }

        public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ── Resolve CustomerId: accepteert zowel GUID als e-mailadres ──
                var appUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == createOrderDto.CustomerId
                                           || u.Email == createOrderDto.CustomerId);

                if (appUser == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                var resolvedCustomerId = appUser.Id; // altijd de echte GUID

                // ── Zorg dat Customer-record bestaat (FK vereiste) ──
                var customerExists = await _context.Customers
                    .AnyAsync(c => c.Id == resolvedCustomerId);

                if (!customerExists)
                {
                    _context.Customers.Add(new Customer
                    {
                        Id = appUser.Id,
                        Email = appUser.Email ?? string.Empty,
                        FirstName = appUser.FirstName,
                        LastName = appUser.LastName,
                        PhoneNumber = appUser.PhoneNumber ?? string.Empty,
                        Address = appUser.Address ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // ── Producten valideren & stock bijwerken ───────────
                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                foreach (var item in createOrderDto.OrderItems)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product == null || !product.IsActive || product.Stock < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return null;
                    }

                    orderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * item.Quantity,
                        Product = product
                    });
                    totalAmount += product.Price * item.Quantity;

                    product.Stock -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                }

                // ── Order aanmaken ──────────────────────────────────
                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    CustomerId = resolvedCustomerId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    PaymentMethod = createOrderDto.PaymentMethod,
                    TotalAmount = totalAmount,
                    ShippingAddress = createOrderDto.ShippingAddress,
                    BillingAddress = createOrderDto.BillingAddress,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToDto(order);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order != null ? MapToDto(order) : null;
        }

        public async Task<OrderDto?> GetOrderByOrderNumberAsync(string orderNumber)
        {
            var order = await _orderRepository.GetByOrderNumberAsync(orderNumber);
            return order != null ? MapToDto(order) : null;
        }

        public async Task<IEnumerable<OrderDto>> GetCustomerOrdersAsync(string customerId)
        {
            var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
            return orders.Select(MapToDto);
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.Status != OrderStatus.Pending)
                    return false;

                foreach (var item in order.OrderItems)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null) return;

                // Stock alleen herstellen als de order NIET al geannuleerd was
                // (bij annulatie werd stock al teruggestort door CancelOrderAsync)
                if (order.Status != OrderStatus.Cancelled)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var product = await _productRepository.GetByIdAsync(item.ProductId);
                        if (product != null)
                        {
                            product.Stock += item.Quantity;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _orderRepository.DeleteAsync(orderId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                StatusName = order.Status.ToString(),
                PaymentMethod = order.PaymentMethod,
                PaymentMethodName = order.PaymentMethod.ToString(),
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            };
        }
    }
}
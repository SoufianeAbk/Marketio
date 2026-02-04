using Marketio_Shared.DTOs;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;

namespace Marketio_Web.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            // Validatie producten en totale berekening
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var item in createOrderDto.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null || !product.IsActive || product.Stock < item.Quantity)
                    return null;

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * item.Quantity,
                    Product = product
                };

                orderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;

                // Update stock
                await _productRepository.UpdateStockAsync(product.Id, product.Stock - item.Quantity);
            }

            // Create order
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                CustomerId = createOrderDto.CustomerId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                PaymentMethod = createOrderDto.PaymentMethod,
                TotalAmount = totalAmount,
                ShippingAddress = createOrderDto.ShippingAddress,
                BillingAddress = createOrderDto.BillingAddress,
                OrderItems = orderItems
            };

            var createdOrder = await _orderRepository.AddAsync(order);
            return MapToDto(createdOrder);
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order != null ? MapToDto(order) : null;
        }

        public async Task<IEnumerable<OrderDto>> GetCustomerOrdersAsync(string customerId)
        {
            var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
            return orders.Select(MapToDto);
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.Pending)
                return false;

            // Restore stock voor alle items
            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    await _productRepository.UpdateStockAsync(product.Id, product.Stock + item.Quantity);
                }
            }

            return await _orderRepository.UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled);
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
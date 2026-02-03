using Marketio_Shared.DTOs;

namespace Marketio_Shared.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto?> CreateOrderAsync(CreateOrderDto createOrderDto);
        Task<OrderDto?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<OrderDto>> GetCustomerOrdersAsync(string customerId);
        Task<bool> CancelOrderAsync(int orderId);
    }
}
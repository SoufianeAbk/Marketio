using Marketio_Shared.Enums;

namespace Marketio_Shared.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
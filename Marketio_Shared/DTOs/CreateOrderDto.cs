using Marketio_Shared.Enums;

namespace Marketio_Shared.DTOs
{
    public class CreateOrderDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public List<CreateOrderItemDto> OrderItems { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
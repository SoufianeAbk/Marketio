using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Marketio_Shared.DTOs;


namespace Marketio_App.Services
{
    public class OrderApiService
    {
        private readonly ApiService _api;
        private readonly ConnectivityService _connectivity;

        public OrderApiService(ApiService api, ConnectivityService connectivity)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        }

        public async Task<IEnumerable<OrderDto>?> GetMyOrdersAsync()
        {
            if (!_connectivity.IsConnected)
                return null;

            try
            {
                var orders = await _api.GetAsync<IEnumerable<OrderDto>>("api/orders/my-orders");
                return orders;
            }
            catch
            {
                return null;
            }
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            if (!_connectivity.IsConnected)
                return null;

            try
            {
                var order = await _api.GetAsync<OrderDto>($"api/orders/{orderId}");
                return order;
            }
            catch
            {
                return null;
            }
        }

        public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            if (!_connectivity.IsConnected)
                return null;

            try
            {
                var created = await _api.PostAsync<CreateOrderDto, OrderDto>("api/orders", createOrderDto);
                return created;
            }
            catch
            {
                return null;
            }
        }
    }
}
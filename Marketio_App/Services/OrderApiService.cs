using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Marketio_Shared.DTOs;


namespace Marketio_App.Services
{
    public class OrderApiService
    {
        private readonly ApiService _api;
        private readonly LocalDatabaseService _localDb;
        private readonly ConnectivityService _connectivity;

        public OrderApiService(ApiService api, LocalDatabaseService localDb, ConnectivityService connectivity)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _localDb = localDb ?? throw new ArgumentNullException(nameof(localDb));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        }

        public async Task<IEnumerable<OrderDto>?> GetMyOrdersAsync()
        {
            if (_connectivity.IsConnected)
            {
                try
                {
                    var orders = await _api.GetAsync<IEnumerable<OrderDto>>("api/orders/my-orders");
                    if (orders != null)
                    {
                        await _localDb.SaveOrdersAsync(orders);
                        return orders;
                    }
                }
                catch
                {
                    // fall back to cache
                }
            }

            return await _localDb.GetOrdersAsync();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            if (_connectivity.IsConnected)
            {
                try
                {
                    var order = await _api.GetAsync<OrderDto>($"api/orders/{orderId}");
                    if (order != null)
                    {
                        await _localDb.SaveOrderAsync(order);
                        return order;
                    }
                }
                catch
                {
                    // fall back to cache
                }
            }

            return await _localDb.GetOrderByIdAsync(orderId);
        }

        public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            if (!_connectivity.IsConnected)
                return null;

            try
            {
                var created = await _api.PostAsync<CreateOrderDto, OrderDto>("api/orders", createOrderDto);
                if (created != null)
                {
                    await _localDb.SaveOrderAsync(created);
                    return created;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
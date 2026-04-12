using Marketio_Shared.DTOs;
using Marketio_Shared.Enums;
using System.Text.Json;

namespace Marketio_App.Services
{
    public class OrderApiService
    {
        private readonly ApiService _api;
        private readonly LocalDatabaseService _localDb;
        private readonly ConnectivityService _connectivity;

        /// <summary>
        /// Wordt aangevuurd na een succesvolle synchronisatie van offline bestellingen.
        /// Parameter = aantal gesynchroniseerde bestellingen.
        /// </summary>
        public event EventHandler<int>? PendingOrdersSynced;

        public OrderApiService(ApiService api, LocalDatabaseService localDb, ConnectivityService connectivity)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _localDb = localDb ?? throw new ArgumentNullException(nameof(localDb));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));

            // Auto-sync bij verbindingsherstel
            _connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        private async void OnConnectivityChanged(object? sender, bool isConnected)
        {
            if (!isConnected) return;

            var synced = await SyncPendingOrdersAsync();
            if (synced > 0)
            {
                PendingOrdersSynced?.Invoke(this, synced);
            }
        }

        // ─── Ophalen ─────────────────────────────────────────────────────────────

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
                        var pending = await BuildPendingOrderDtosAsync();
                        return orders.Concat(pending);
                    }
                }
                catch
                {
                    // fall back to cache
                }
            }

            var cached = await _localDb.GetOrdersAsync();
            var pendingOffline = await BuildPendingOrderDtosAsync();
            return cached.Concat(pendingOffline);
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

        // ─── Aanmaken (met offline queue) ─────────────────────────────────────────

        /// <summary>
        /// Plaatst een bestelling. Wanneer er geen verbinding is, wordt de bestelling
        /// in de lokale wachtrij opgeslagen en zodra de verbinding hersteld is verzonden.
        /// Geeft altijd een OrderDto terug: online = definitief, offline = Id=0 (wachtrij).
        /// </summary>
        public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            if (_connectivity.IsConnected)
            {
                return await CreateOnlineAsync(createOrderDto);
            }

            // Offline: sla op in wachtrij
            return await QueueOfflineOrderAsync(createOrderDto);
        }

        private async Task<OrderDto?> CreateOnlineAsync(CreateOrderDto createOrderDto)
        {
            try
            {
                var created = await _api.PostAsync<CreateOrderDto, OrderDto>("api/orders", createOrderDto);
                if (created != null && created.Id > 0)
                {
                    await _localDb.SaveOrderAsync(created);
                    System.Diagnostics.Debug.WriteLine(
                        $"[OrderApiService] Bestelling aangemaakt: ID={created.Id}, Nr={created.OrderNumber}");
                    return created;
                }

                System.Diagnostics.Debug.WriteLine("[OrderApiService] Server gaf geen geldig ID terug.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderApiService] Online aanmaken mislukt: {ex.Message}");
                return null;
            }
        }

        private async Task<OrderDto> QueueOfflineOrderAsync(CreateOrderDto createOrderDto)
        {
            var localId = await _localDb.SavePendingOrderAsync(createOrderDto);

            System.Diagnostics.Debug.WriteLine(
                $"[OrderApiService] Bestelling opgeslagen in wachtrij (LocalId={localId})");

            // Bouw een preview-DTO zodat de UI direct kan reageren
            return new OrderDto
            {
                Id = 0,
                OrderNumber = $"WACHTRIJ-{localId}",
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                StatusName = "In wachtrij (offline)",
                PaymentMethod = createOrderDto.PaymentMethod,
                PaymentMethodName = createOrderDto.PaymentMethod.ToString(),
                ShippingAddress = createOrderDto.ShippingAddress,
                TotalAmount = 0, // onbekend zonder serverberekening
                OrderItems = createOrderDto.OrderItems.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };
        }

        // ─── Synchronisatie ───────────────────────────────────────────────────────

        /// <summary>
        /// Verzendt alle bestellingen uit de offline wachtrij naar de API.
        /// Geeft het aantal succesvol gesynchroniseerde bestellingen terug.
        /// </summary>
        public async Task<int> SyncPendingOrdersAsync()
        {
            if (!_connectivity.IsConnected)
                return 0;

            List<LocalDatabaseService.PendingOrder> pendingOrders;
            try
            {
                pendingOrders = await _localDb.GetPendingOrdersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderApiService] Fout bij ophalen wachtrij: {ex.Message}");
                return 0;
            }

            if (pendingOrders.Count == 0)
                return 0;

            System.Diagnostics.Debug.WriteLine(
                $"[OrderApiService] Synchroniseren van {pendingOrders.Count} offline bestelling(en)...");

            int synced = 0;

            foreach (var pending in pendingOrders)
            {
                try
                {
                    var createDto = JsonSerializer.Deserialize<CreateOrderDto>(
                        pending.CreateOrderJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (createDto == null)
                    {
                        // Corrupte entry, verwijderen
                        await _localDb.DeletePendingOrderAsync(pending.LocalId);
                        continue;
                    }

                    var created = await CreateOnlineAsync(createDto);

                    if (created != null && created.Id > 0)
                    {
                        await _localDb.DeletePendingOrderAsync(pending.LocalId);
                        synced++;
                        System.Diagnostics.Debug.WriteLine(
                            $"[OrderApiService] Wachtrij-item {pending.LocalId} gesynchroniseerd → ID={created.Id}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[OrderApiService] Wachtrij-item {pending.LocalId}: server gaf geen geldig antwoord.");
                    }
                }
                catch (Exception ex)
                {
                    // Niet fataal: volgende sync-poging pakt dit op
                    System.Diagnostics.Debug.WriteLine(
                        $"[OrderApiService] Wachtrij-item {pending.LocalId} mislukt: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[OrderApiService] Sync klaar: {synced}/{pendingOrders.Count} gesynchroniseerd.");

            return synced;
        }

        public async Task<int> GetPendingOrderCountAsync()
        {
            return await _localDb.GetPendingOrderCountAsync();
        }

        // ─── Verwijderen ─────────────────────────────────────────────────────────

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            if (!_connectivity.IsConnected)
                return false;

            try
            {
                await _api.DeleteAsync($"api/orders/{orderId}");
                await _localDb.DeleteOrderAsync(orderId);
                System.Diagnostics.Debug.WriteLine($"[OrderApiService] Bestelling verwijderd: ID={orderId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderApiService] Verwijderen mislukt: {ex.Message}");
                return false;
            }
        }

        // ─── Hulpmethoden ─────────────────────────────────────────────────────────

        /// <summary>
        /// Zet PendingOrders om naar preview-OrderDtos voor weergave in de lijst.
        /// </summary>
        private async Task<IEnumerable<OrderDto>> BuildPendingOrderDtosAsync()
        {
            try
            {
                var pending = await _localDb.GetPendingOrdersAsync();

                return pending.Select(p => new OrderDto
                {
                    Id = 0,
                    OrderNumber = $"WACHTRIJ-{p.LocalId}",
                    OrderDate = p.CreatedAt,
                    Status = OrderStatus.Pending,
                    StatusName = "In wachtrij (offline)",
                    TotalAmount = 0
                });
            }
            catch
            {
                return Enumerable.Empty<OrderDto>();
            }
        }
    }
}
using System;
using Microsoft.Maui.Networking;

namespace Marketio_App.Services
{
    public class ConnectivityService : IDisposable
    {
        public bool IsConnected { get; private set; }

        public event EventHandler<bool>? ConnectivityChanged;

        public ConnectivityService()
        {
            UpdateNetworkStatus();
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        private void UpdateNetworkStatus()
        {
            IsConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            UpdateNetworkStatus();
            ConnectivityChanged?.Invoke(this, IsConnected);
        }

        public void Dispose()
        {
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }
    }
}
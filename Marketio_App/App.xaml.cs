using Marketio_App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketio_App
{
    public partial class App : Application
    {
        private readonly AuthService _authService;
        private readonly ApiService _apiService;
        private bool _hasNavigated = false;

        public App(AuthService authService, ApiService apiService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            // Subscribe to token expiration events
            _apiService.TokenExpired += OnApiServiceTokenExpired;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var appShell = IPlatformApplication.Current?.Services.GetService<AppShell>()
                ?? throw new InvalidOperationException("AppShell not found in DI container");

            var window = new Window(appShell);

            // Perform authentication check after Shell is created
            MainThread.BeginInvokeOnMainThread(async () => await PerformInitialNavigationAsync());

            return window;
        }

        protected override async void OnStart()
        {
            base.OnStart();
            // OnStart is called too early on Windows; Shell may not be ready yet
            // Navigation is deferred to CreateWindow via MainThread.BeginInvokeOnMainThread
        }

        private async Task PerformInitialNavigationAsync()
        {
            try
            {
                // Add delay to ensure Shell.Current is fully initialized on all platforms
                await Task.Delay(100);

                if (_hasNavigated)
                    return;

                _hasNavigated = true;

                // Check for existing token and validate its expiration
                var isTokenValid = await _authService.IsTokenValidAsync();

                if (isTokenValid)
                {
                    // Token exists and is still valid, navigate to products
                    System.Diagnostics.Debug.WriteLine("[App] Valid token found — navigating to products");
                    await Shell.Current.GoToAsync("///producten", animate: false);
                }
                else
                {
                    // No valid token, navigate to login
                    System.Diagnostics.Debug.WriteLine("[App] No valid token found — navigating to login");
                    await Shell.Current.GoToAsync("///login", animate: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Navigation error: {ex.Message}");
                // Fallback to login on any error
                await Shell.Current.GoToAsync("///login", animate: false);
            }
        }

        private async void OnApiServiceTokenExpired(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[App] Token expired event triggered — clearing token and redirecting to login");

            try
            {
                // Clear the expired token
                await _authService.LogoutAsync();

                // Navigate to login on the main thread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("///login", animate: false);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error handling token expiration: {ex.Message}");
            }
        }
    }
}
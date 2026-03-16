using Marketio_App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketio_App
{
    public partial class App : Application
    {
        private readonly AuthService _authService;
        private bool _hasNavigated = false;

        public App(AuthService authService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
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
                if (_hasNavigated || Shell.Current == null)
                    return;

                _hasNavigated = true;

                // Check for existing token at startup
                var token = await _authService.GetTokenAsync();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    // Token exists, navigate to products
                    await Shell.Current.GoToAsync("///producten", animate: false);
                }
                else
                {
                    // No token, navigate to login
                    await Shell.Current.GoToAsync("///login", animate: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
    }
}
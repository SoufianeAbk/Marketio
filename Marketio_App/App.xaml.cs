using Marketio_App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketio_App
{
    public partial class App : Application
    {
        private readonly AuthService _authService;

        public App()
        {
            InitializeComponent();

            _authService = IPlatformApplication.Current?.Services.GetRequiredService<AuthService>()
                ?? throw new InvalidOperationException("AuthService not configured");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override async void OnStart()
        {
            base.OnStart();

            // Check for existing token at startup
            var token = await _authService.GetTokenAsync();
            var shell = MainPage as AppShell;

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
    }
}
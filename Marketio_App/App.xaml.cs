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
            // Abonneer op token-verloopgebeurtenissen
            _apiService.TokenExpired += OnApiServiceTokenExpired;
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var appShell = IPlatformApplication.Current?.Services.GetService<AppShell>()
                ?? throw new InvalidOperationException("AppShell not found in DI container");
            var window = new Window(appShell);
            // Voer authenticatiecontrole uit nadat de Shell is aangemaakt
            MainThread.BeginInvokeOnMainThread(async () => await PerformInitialNavigationAsync());
            return window;
        }
        protected override async void OnStart()
        {
            base.OnStart();
            // OnStart wordt te vroeg aangeroepen op Windows; Shell is mogelijk nog niet gereed
            // Navigatie wordt uitgesteld naar CreateWindow via MainThread.BeginInvokeOnMainThread
        }
        private async Task PerformInitialNavigationAsync()
        {
            try
            {
                // Wacht kort zodat Shell.Current volledig geïnitialiseerd is op alle platformen
                await Task.Delay(100);
                if (_hasNavigated)
                    return;
                _hasNavigated = true;
                // Controleer het bestaande token en valideer de vervaldatum
                var isTokenValid = await _authService.IsTokenValidAsync();
                if (isTokenValid)
                {
                    // Token bestaat en is nog geldig, navigeer naar producten
                    System.Diagnostics.Debug.WriteLine("[App] Geldig token gevonden — navigeren naar producten");
                    await Shell.Current.GoToAsync("///producten", animate: false);
                }
                else
                {
                    // Geen geldig token, navigeer naar inlogpagina
                    System.Diagnostics.Debug.WriteLine("[App] Geen geldig token gevonden — navigeren naar login");
                    await Shell.Current.GoToAsync("///login", animate: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Navigatiefout: {ex.Message}");
                // Terugvallen op inlogpagina bij elke fout
                await Shell.Current.GoToAsync("///login", animate: false);
            }
        }
        private async void OnApiServiceTokenExpired(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[App] Token-verloopgebeurtenis geactiveerd — token wissen en doorsturen naar login");
            try
            {
                // Verwijder het verlopen token
                await _authService.LogoutAsync();
                // Navigeer naar de inlogpagina op de hoofdthread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("///login", animate: false);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Fout bij verwerken van tokenverloop: {ex.Message}");
            }
        }
    }
}
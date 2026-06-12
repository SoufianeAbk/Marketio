using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Marketio_App.Services;
using Marketio_App.ViewModels;
using Marketio_App.Pages;

namespace Marketio_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Bepaal de platform-specifieke API-basis-URL
            var apiBaseUrl = GetPlatformApiBaseUrl();

            // HttpClient met SSL-bypass (alleen voor ontwikkeling)
            // Registreer ApiService als zowel HttpClient als Singleton voor DI-compatibiliteit
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
#if DEBUG
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Vertrouw zelfondertekende certificaten van de ASP.NET Core-ontwikkelserver
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
#else
            ;
#endif

            // Beveiligingsservices
            builder.Services.AddSingleton<SecureKeyManagementService>();

            // Kern-app-services
            builder.Services.AddSingleton<ConnectivityService>();
            builder.Services.AddSingleton<LocalDatabaseService>();
            builder.Services.AddSingleton<ProductApiService>();
            builder.Services.AddSingleton<OrderApiService>();
            builder.Services.AddSingleton<AccountApiService>();
            builder.Services.AddSingleton<CartService>();
            builder.Services.AddSingleton<AuthService>();

            // ViewModels
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<RegisterViewModel>();
            builder.Services.AddSingleton<ProductsViewModel>();
            builder.Services.AddTransient<ProductDetailViewModel>();
            builder.Services.AddSingleton<CartViewModel>();
            builder.Services.AddSingleton<OrdersViewModel>();
            builder.Services.AddTransient<OrderDetailViewModel>();
            builder.Services.AddTransient<CreateOrderViewModel>();
            builder.Services.AddSingleton<AccountSettingsViewModel>();

            // Pagina's (AppShell vóór App registreren)
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<ProductsPage>();
            builder.Services.AddTransient<ProductDetailPage>();
            builder.Services.AddSingleton<CartPage>();
            builder.Services.AddSingleton<OrdersPage>();
            builder.Services.AddTransient<OrderDetailPage>();
            builder.Services.AddTransient<CreateOrderPage>();
            builder.Services.AddSingleton<AccountSettingsPage>();

            var app = builder.Build();

            // Initialiseer services bij opstarten
            Task.Run(async () =>
            {
                try
                {
                    // Initialiseer LocalDatabase als eerste
                    var localDb = app.Services.GetRequiredService<LocalDatabaseService>();
                    await localDb.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] LocalDatabaseService succesvol geïnitialiseerd.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] LocalDatabaseService.InitializeAsync mislukt: {ex.Message}");
                }

                try
                {
                    // Initialiseer ApiService (laadt opgeslagen JWT in de Authorization-header)
                    var apiService = app.Services.GetRequiredService<ApiService>();
                    await apiService.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] ApiService succesvol geïnitialiseerd.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] ApiService.InitializeAsync mislukt: {ex.Message}");
                }
            });

            return app;
        }

        /// <summary>
        /// Centrale bron van de platform-specifieke API-basis-URL.
        /// Internal zodat andere services (bv. AccountApiService) deze kunnen hergebruiken
        /// zonder de URL opnieuw te hardcoderen.
        /// </summary>
        internal static string GetPlatformApiBaseUrl()
        {
#if ANDROID
            return "https://10.0.2.2:7170/";
#elif IOS
            return "https://localhost:7170/";
#elif MACCATALYST
            return "https://localhost:7170/";
#elif WINDOWS
            return "https://localhost:7170/";
#else
            return "https://10.0.2.2:7170/";
#endif
        }
    }
}
using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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

            // ─── Resolve platform-specific API base URL ────────────────────────────
            var apiBaseUrl = GetPlatformApiBaseUrl();

            // ─── HttpClient with SSL bypass (dev only) ────────────────────────────────
            // Register ApiService as both HttpClient and Singleton for DI compatibility
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
#if DEBUG
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Trust self-signed certs from the ASP.NET Core dev server
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
#else
            ;
#endif

            // ─── Core app services ────────────────────────────────────────────────────
            builder.Services.AddSingleton<ConnectivityService>();
            builder.Services.AddSingleton<LocalDatabaseService>();
            builder.Services.AddSingleton<ProductApiService>();
            builder.Services.AddSingleton<OrderApiService>();
            builder.Services.AddSingleton<AccountApiService>();
            builder.Services.AddSingleton<CartService>();
            builder.Services.AddSingleton<AuthService>();

            // ─── ViewModels ───────────────────────────────────────────────────────────
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<RegisterViewModel>();
            builder.Services.AddSingleton<ProductsViewModel>();
            builder.Services.AddTransient<ProductDetailViewModel>();
            builder.Services.AddSingleton<CartViewModel>();
            builder.Services.AddSingleton<OrdersViewModel>();
            builder.Services.AddTransient<OrderDetailViewModel>();
            builder.Services.AddTransient<CreateOrderViewModel>();
            builder.Services.AddSingleton<AccountSettingsViewModel>();

            // ─── Pages (AppShell vóór App registreren) ────────────────────────────────
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

            // ─── Initialize services on startup ──────────────────────────────────────
            Task.Run(async () =>
            {
                try
                {
                    // Initialize LocalDatabase first
                    var localDb = app.Services.GetRequiredService<LocalDatabaseService>();
                    await localDb.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] LocalDatabaseService initialized successfully.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] LocalDatabaseService.InitializeAsync failed: {ex.Message}");
                }

                try
                {
                    // Initialize ApiService (loads stored JWT into Authorization header)
                    var apiService = app.Services.GetRequiredService<ApiService>();
                    await apiService.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] ApiService initialized successfully.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] ApiService.InitializeAsync failed: {ex.Message}");
                }
            });

            return app;
        }

        /// <summary>
        /// Bepaalt de platform-specifieke API base URL
        /// </summary>
        private static string GetPlatformApiBaseUrl()
        {
            var platformKey = GetPlatformKey();

            // Hardcoded URLs per platform
            return platformKey switch
            {
                "Android" => "https://10.0.2.2:7170/",
                "iOS" => "https://localhost:7170/",
                "MacCatalyst" => "https://localhost:7170/",
                "Windows" => "https://localhost:7170/",
                _ => "https://10.0.2.2:7170/"
            };
        }

        /// <summary>
        /// Bepaalt het platform-specifieke sleutelnaam voor de configuratie
        /// </summary>
        private static string GetPlatformKey()
        {
#if ANDROID
            return "Android";
#elif IOS
            return "iOS";
#elif MACCATALYST
            return "MacCatalyst";
#elif WINDOWS
            return "Windows";
#else
            return "Default";
#endif
        }
    }
}
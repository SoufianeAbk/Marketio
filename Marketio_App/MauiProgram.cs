using System;
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

            var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7170/";

            // ─── HttpClient with SSL bypass (dev only) ────────────────────────────────
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBase);
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
            builder.Services.AddSingleton<AuthService>();

            // ─── ViewModels ───────────────────────────────────────────────────────────
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<RegisterViewModel>();
            builder.Services.AddSingleton<ProductsViewModel>();
            builder.Services.AddSingleton<ProductDetailViewModel>();
            builder.Services.AddSingleton<OrdersViewModel>();
            builder.Services.AddSingleton<OrderDetailViewModel>();

            // ─── Pages (AppShell vóór App registreren) ────────────────────────────────
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<ProductsPage>();
            builder.Services.AddSingleton<ProductDetailPage>();
            builder.Services.AddSingleton<OrdersPage>();
            builder.Services.AddSingleton<OrderDetailPage>();

            var app = builder.Build();

            // ─── Initialize ApiService (loads stored JWT into Authorization header) ──
            var apiService = app.Services.GetRequiredService<ApiService>();
            Task.Run(async () =>
            {
                try
                {
                    await apiService.InitializeAsync();
                }
                catch (Exception ex)
                {
                    // Non-fatal: app can still run; user will just need to log in again
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] ApiService.InitializeAsync failed: {ex.Message}");
                }
            });

            return app;
        }
    }
}
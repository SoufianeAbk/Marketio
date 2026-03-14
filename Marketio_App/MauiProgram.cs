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

            var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://10.0.2.2:5001/";

            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBase);
            });

            // Core app services
            builder.Services.AddSingleton<ConnectivityService>();
            builder.Services.AddSingleton<LocalDatabaseService>();
            builder.Services.AddSingleton<ProductApiService>();
            builder.Services.AddSingleton<OrderApiService>();
            builder.Services.AddSingleton<AuthService>();

            // ViewModels met dependency injection
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<RegisterViewModel>();
            builder.Services.AddSingleton<ProductsViewModel>();
            builder.Services.AddSingleton<ProductDetailViewModel>();
            builder.Services.AddSingleton<OrdersViewModel>();
            builder.Services.AddSingleton<OrderDetailViewModel>();
            builder.Services.AddSingleton<OrderDetailPage>();

            // Pages (registry zodat constructor injection werkt vanuit DI)
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<ProductsPage>();
            builder.Services.AddSingleton<ProductDetailPage>();
            builder.Services.AddSingleton<OrdersPage>();
            builder.Services.AddSingleton<OrderDetailPage>();

            var app = builder.Build();

            try
            {
                var apiService = app.Services.GetRequiredService<ApiService>();
                apiService.InitializeAsync().GetAwaiter().GetResult();

                var localDb = app.Services.GetRequiredService<LocalDatabaseService>();
                localDb.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var loggerFactory = app.Services.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("MauiProgram");
                logger?.LogWarning(ex, "One or more startup initializers failed.");
            }

            return app;
        }
    }
}
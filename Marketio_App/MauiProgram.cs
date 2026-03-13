using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Marketio_App.Services;

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

            // Read API base URL from configuration if present, otherwise fallback to a sensible default for local dev.
            var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://10.0.2.2:5001/";

            // HttpClient typed client that ApiService will receive
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

            var app = builder.Build();

            // Initialize services that need startup work (synchronous wait is intentional for deterministic startup)
            try
            {
                // ApiService may read stored JWT and set auth header
                var apiService = app.Services.GetRequiredService<ApiService>();
                apiService.InitializeAsync().GetAwaiter().GetResult();

                // Local DB setup (creates file/tables)
                var localDb = app.Services.GetRequiredService<LocalDatabaseService>();
                localDb.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Avoid using static MauiProgram as generic type argument.
                var loggerFactory = app.Services.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("MauiProgram");
                logger?.LogWarning(ex, "One or more startup initializers failed.");
            }

            return app;
        }
    }
}
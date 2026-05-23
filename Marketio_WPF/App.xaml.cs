using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using Marketio_WPF.Data;
using Marketio_Shared.Models;
using Marketio_WPF.Services;
using Marketio_WPF.Services.Interfaces;
using Marketio_WPF.ViewModels;
using Marketio_WPF.Views;

namespace Marketio_WPF
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; } = null!;
        private ServiceCollection _services = null!;

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _services = new ServiceCollection();
                ConfigureServices(_services);
                ServiceProvider = _services.BuildServiceProvider();

                var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<App>();

                logger.LogInformation("Starting database migration and seeding...");
                await SeedDatabaseAsync();
                logger.LogInformation("Database migration and seeding completed successfully");

                var loginViewModel = ServiceProvider.GetRequiredService<LoginViewModel>();
                var loginView = new LoginView { DataContext = loginViewModel };

                logger.LogInformation("Showing login window");

                loginViewModel.LoginSucceeded += async (s, e) =>
                {
                    logger.LogInformation("Login succeeded, showing main window");
                    MainWindow = new MainWindow();
                    MainWindow.Show();
                };

                loginView.ShowDialog();

                if (MainWindow == null)
                {
                    logger.LogInformation("Login was cancelled, shutting down application");
                    Shutdown(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application startup error:\n{ex.Message}\n\nCheck the Output window for detailed logs.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<App>()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in User Secrets.");

            services.AddLogging(config =>
            {
                config.ClearProviders();
                config.AddConsole();
                config.AddDebug();
                config.SetMinimumLevel(LogLevel.Debug);
            });

            services.AddDbContext<MarketioDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly("Marketio_WPF"))
            );

            services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MarketioDbContext>();

            services.AddScoped<UserManager<AppUser>>();
            services.AddScoped<RoleManager<IdentityRole>>();

            services.AddScoped<DataSeeder>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<UserManagementService>();
            services.AddScoped<OrderService>();
            services.AddScoped<CustomerService>();
            services.AddScoped<ProductService>();

            services.AddScoped<MainViewModel>();
            services.AddScoped<ProductsViewModel>();
            services.AddScoped<OrdersViewModel>();
            services.AddScoped<CustomersViewModel>();
            services.AddScoped<AdminViewModel>();
            services.AddScoped<LoginViewModel>();
            services.AddScoped<RegisterViewModel>();
        }

        private async Task SeedDatabaseAsync()
        {
            using var scope = ServiceProvider.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<DataSeeder>();
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

            try
            {
                await seeder.SeedAsync();
                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database seeding");
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
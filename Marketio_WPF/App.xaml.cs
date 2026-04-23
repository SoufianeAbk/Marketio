using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Marketio_WPF.Data;
using Marketio_WPF.Models;
using Marketio_WPF.Services;
using Marketio_WPF.Services.Interfaces;
using Marketio_WPF.Views;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

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
                // Setup Dependency Injection
                _services = new ServiceCollection();
                ConfigureServices(_services);
                ServiceProvider = _services.BuildServiceProvider();

                // Run database migrations and seed data
                await SeedDatabaseAsync();

                // Show main window
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application startup error:\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Get connection string from app configuration
            var connectionString = GetConnectionString();

            // Register DbContext
            services.AddDbContext<MarketioDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly("Marketio_WPF"))
            );

            // Register Identity Core (no HTTP/SignIn manager for desktop WPF)
            services.AddIdentityCore<AppUser>(options =>
            {
                // Password requirements
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // User requirements
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MarketioDbContext>();

            // Add UserManager, RoleManager for DI
            services.AddScoped<UserManager<AppUser>>();
            services.AddScoped<RoleManager<IdentityRole>>();

            // Register application services
            services.AddScoped<DataSeeder>();
            services.AddScoped<IAuthService, AuthService>();

            // Register repositories (if using repository pattern)
            // services.AddScoped<IProductRepository, ProductRepository>();
            // services.AddScoped<IOrderRepository, OrderRepository>();

            // Register other services as needed
            // services.AddScoped<IProductService, ProductService>();
            // services.AddScoped<IOrderService, OrderService>();
        }

        private string GetConnectionString()
        {
            // In a real application, read from appsettings.json or environment variables
            // For now, using a local SQL Server connection
            return "Server=(localdb)\\mssqllocaldb;Database=MarketioDb;Integrated Security=true;";
        }

        private async Task SeedDatabaseAsync()
        {
            using var scope = ServiceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
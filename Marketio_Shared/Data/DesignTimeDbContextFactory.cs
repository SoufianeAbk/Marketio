using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Marketio_Shared.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketioDbContext>
    {
        public MarketioDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(MarketioDbContext).Assembly, optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in User Secrets.");

            var optionsBuilder = new DbContextOptionsBuilder<MarketioDbContext>();
            optionsBuilder.UseNpgsql(
                connectionString,
                b => b.MigrationsAssembly("Marketio_WPF"));

            return new MarketioDbContext(optionsBuilder.Options);
        }
    }
}
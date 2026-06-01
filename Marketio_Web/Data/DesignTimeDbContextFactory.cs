using Marketio_Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Marketio_Web.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketioDbContext>
    {
        public MarketioDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Stel de Supabase connection string in via User Secrets: " +
                    "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=...\"");

            var optionsBuilder = new DbContextOptionsBuilder<MarketioDbContext>();
            optionsBuilder.UseNpgsql(
                connectionString,
                b => b.MigrationsAssembly("Marketio_Web"));

            return new MarketioDbContext(optionsBuilder.Options);
        }
    }
}

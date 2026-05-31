using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Marketio_Shared.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketioDbContext>
    {
        // Fallback: LocalDB voor lokale migraties (zelfde als App.xaml.cs runtime-string).
        // Overschrijf via User Secrets: dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
        private const string FallbackConnection =
            "Server=(localdb)\\MSSQLLocalDB;Database=MarketioDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        public MarketioDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(MarketioDbContext).Assembly, optional: true)
                .Build();

            var connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? FallbackConnection;

            var optionsBuilder = new DbContextOptionsBuilder<MarketioDbContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly("Marketio_Shared"));

            return new MarketioDbContext(optionsBuilder.Options);
        }
    }
}

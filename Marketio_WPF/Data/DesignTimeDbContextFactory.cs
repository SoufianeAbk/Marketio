using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Marketio_WPF.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketioDbContext>
    {
        public MarketioDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MarketioDbContext>();

            // Use the same connection string as in App.xaml.cs
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=MarketioDb;Integrated Security=true;";

            optionsBuilder.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly("Marketio_WPF"));

            return new MarketioDbContext(optionsBuilder.Options);
        }
    }
}
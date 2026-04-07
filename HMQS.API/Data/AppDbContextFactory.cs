using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HMQS.API.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var connectionString = config.GetConnectionString("DefaultConnection");

            // Hardcode MariaDB 10.4.32 instead of AutoDetect
            // AutoDetect tries to connect just to check the version - that was causing the failure
            optionsBuilder.UseMySql(
                connectionString,
                new MariaDbServerVersion(new Version(10, 4, 32))
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
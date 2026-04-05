using Microsoft.EntityFrameworkCore;

namespace WebApp.Data
{
    public class WebAppDbContext(DbContextOptions<WebAppDbContext> options) : DbContext(options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}
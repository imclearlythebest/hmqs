using WebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace WebApp.Data
{
    public class WebAppDbContext(DbContextOptions<WebAppDbContext> options) : IdentityDbContext<WebAppUser>(options)
    {
        public DbSet<Scrobble> Scrobbles => Set<Scrobble>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}
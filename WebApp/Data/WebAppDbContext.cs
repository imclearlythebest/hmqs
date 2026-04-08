using WebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace WebApp.Data
{
    public class WebAppDbContext(DbContextOptions<WebAppDbContext> options) : IdentityDbContext<WebAppUser>(options)
    {
        public DbSet<Track> Tracks => Set<Track>();
        public DbSet<Artist> Artists => Set<Artist>();
        public DbSet<Collection> Albums => Set<Collection>();
        public DbSet<Genre> Genres => Set<Genre>();
        public DbSet<Scrobble> Scrobbles => Set<Scrobble>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}
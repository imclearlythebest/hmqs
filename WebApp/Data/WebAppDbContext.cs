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
        public DbSet<UserArtistStat> UserArtistStats => Set<UserArtistStat>();
        public DbSet<Playlist> Playlists => Set<Playlist>();
        public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserArtistStat>()
                .HasIndex(x => new { x.UserId, x.ArtistId })
                .IsUnique();

            builder.Entity<PlaylistTrack>()
                .HasOne(pt => pt.Playlist)
                .WithMany(p => p.PlaylistTracks)
                .HasForeignKey(pt => pt.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlaylistTrack>()
                .HasOne(pt => pt.Track)
                .WithMany()
                .HasForeignKey(pt => pt.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

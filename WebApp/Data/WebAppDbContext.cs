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
        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<Blend> Blends => Set<Blend>();
        public DbSet<BlendMember> BlendMembers => Set<BlendMember>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<UserArtistStat>()
                .HasIndex(x => new { x.UserId, x.ArtistId })
                .IsUnique();

            builder.Entity<Friendship>(entity =>
            {
                entity.HasOne(f => f.Requester)
                    .WithMany(u => u.SentFriendRequests)
                    .HasForeignKey(f => f.RequesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Receiver)
                    .WithMany(u => u.ReceivedFriendRequests)
                    .HasForeignKey(f => f.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BlendMember>(entity =>
            {
                entity.HasKey(bm => new { bm.BlendId, bm.UserId });

                entity.HasOne(bm => bm.Blend)
                    .WithMany(b => b.Members)
                    .HasForeignKey(bm => bm.BlendId);

                entity.HasOne(bm => bm.User)
                    .WithMany(u => u.BlendMemberships)
                    .HasForeignKey(bm => bm.UserId);
            });
        }
    }
}
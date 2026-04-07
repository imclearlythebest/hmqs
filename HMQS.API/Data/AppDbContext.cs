using Microsoft.EntityFrameworkCore;
using HMQS.API.Models;

namespace HMQS.API.Data
{
    // AppDbContext is the bridge between your C# code and the MySQL database
    // Every table you want EF Core to manage must be a DbSet here
    public class AppDbContext : DbContext
    {
        // Constructor - receives options from Program.cs (connection string, provider, etc.)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Each DbSet maps to one table in the database
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<PlayHistory> PlayHistories { get; set; }
        public DbSet<SpotifyImport> SpotifyImports { get; set; }

        // OnModelCreating is where you define rules EF Core cannot guess on its own
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- User rules ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique(); // No two users can share the same email

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique(); // No two users can share the same username

            // --- RefreshToken -> User relationship ---
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)           // Each token belongs to one user
                .WithMany(u => u.RefreshTokens)  // One user can have many tokens
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If user is deleted, delete their tokens too

            // --- Album -> Artist relationship ---
            modelBuilder.Entity<Album>()
                .HasOne(a => a.Artist)
                .WithMany(ar => ar.Albums)
                .HasForeignKey(a => a.ArtistId)
                .OnDelete(DeleteBehavior.Restrict); // Don't auto-delete albums if artist is deleted

            // --- Song -> User relationship ---
            modelBuilder.Entity<Song>()
                .HasOne(s => s.User)
                .WithMany(u => u.Songs)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If user is deleted, delete their songs

            // --- Song -> Album relationship ---
            modelBuilder.Entity<Song>()
                .HasOne(s => s.Album)
                .WithMany(a => a.Songs)
                .HasForeignKey(s => s.AlbumId)
                .OnDelete(DeleteBehavior.SetNull); // If album deleted, song stays but AlbumId becomes null

            // --- PlayHistory relationships ---
            modelBuilder.Entity<PlayHistory>()
                .HasOne(ph => ph.User)
                .WithMany(u => u.PlayHistories)
                .HasForeignKey(ph => ph.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayHistory>()
                .HasOne(ph => ph.Song)
                .WithMany(s => s.PlayHistories)
                .HasForeignKey(ph => ph.SongId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- SpotifyImport relationships ---
            modelBuilder.Entity<SpotifyImport>()
                .HasOne(si => si.User)
                .WithMany()
                .HasForeignKey(si => si.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpotifyImport>()
                .HasOne(si => si.MatchedSong)
                .WithMany()
                .HasForeignKey(si => si.MatchedSongId)
                .OnDelete(DeleteBehavior.SetNull); // If song deleted, import record stays
        }
    }
}
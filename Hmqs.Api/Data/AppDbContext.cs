using Hmqs.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hmqs.Api.Data;

public class AppDbContext : IdentityDbContext<Listener, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options)
    {
    }
    public DbSet<GlobalTrack> GlobalTracks { get; set; }
    public DbSet<LocalTrack> LocalTracks { get; set; }
    public DbSet<Scrobble> Scrobbles { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Album> Albums { get; set; }
    public DbSet<ListenerArtistPreference> ListenerArtistPreferences { get; set; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<LocalTrack>()
            .HasOne(l => l.Listener)
            .WithMany(l => l.LocalTracks)
            .HasForeignKey(l => l.ListenerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<LocalTrack>()
            .HasOne(l => l.GlobalTrack)
            .WithMany(g => g.LocalTracks)
            .HasForeignKey(l => l.GlobalTrackId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Scrobble>()
            .HasOne(s => s.Listener)
            .WithMany(l => l.Scrobbles)
            .HasForeignKey(s => s.ListenerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Scrobble>()
            .HasOne(s => s.LocalTrack)
            .WithMany(lt => lt.Scrobbles)
            .HasForeignKey(s => s.LocalTrackId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Playlist>()
            .HasOne(p => p.Owner)
            .WithMany(l => l.Playlists)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

         modelBuilder.Entity<PlaylistTrack>()
            .HasOne(pt => pt.Playlist)
            .WithMany(p => p.PlaylistTracks)
            .HasForeignKey(pt => pt.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<PlaylistTrack>()
            .HasOne(pt => pt.LocalTrack)
            .WithMany(lt => lt.PlaylistTracks)
            .HasForeignKey(pt => pt.LocalTrackId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<GlobalTrack>()
            .HasOne(g => g.Artist)
            .WithMany(a => a.GlobalTracks)
            .HasForeignKey(g => g.ArtistId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GlobalTrack>()
            .HasOne(g => g.Album)
            .WithMany(a => a.GlobalTracks)
            .HasForeignKey(g => g.AlbumId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Album>()
            .HasOne(a => a.Artist)
            .WithMany(ar => ar.Albums)
            .HasForeignKey(a => a.ArtistId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ListenerArtistPreference>()
            .HasKey(x => new { x.ListenerId, x.ArtistId });

        modelBuilder.Entity<ListenerArtistPreference>()
            .HasOne(x => x.Listener)
            .WithMany()
            .HasForeignKey(x => x.ListenerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ListenerArtistPreference>()
            .HasOne(x => x.Artist)
            .WithMany()
            .HasForeignKey(x => x.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {

    }
}
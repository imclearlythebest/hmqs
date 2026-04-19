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

        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {

    }
}
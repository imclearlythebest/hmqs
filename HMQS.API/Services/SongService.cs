using Microsoft.EntityFrameworkCore;
using HMQS.API.Data;
using HMQS.API.DTOs;
using HMQS.API.Models;

namespace HMQS.API.Services
{
    public class SongService
    {
        private readonly AppDbContext _db;

        public SongService(AppDbContext db)
        {
            _db = db;
        }

        // ─── GET ALL SONGS FOR A USER ─────────────────────────────────────────

        public async Task<List<SongResponseDto>> GetUserSongsAsync(int userId)
        {
            // Query songs for this user only
            // Include() tells EF Core to also load related Album and Artist data
            // This generates a SQL JOIN instead of multiple queries
            var songs = await _db.Songs
                .Where(s => s.UserId == userId)
                .Include(s => s.Album)
                    .ThenInclude(a => a!.Artist) // Also load the artist via the album
                .OrderByDescending(s => s.AddedAt) // Newest first
                .Select(s => new SongResponseDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    FilePath = s.FilePath,
                    Duration = s.Duration,
                    CoverArtUrl = s.CoverArtUrl,
                    AlbumTitle = s.Album != null ? s.Album.Title : null,
                    ArtistName = s.Album != null && s.Album.Artist != null
                        ? s.Album.Artist.Name
                        : null,
                    AddedAt = s.AddedAt
                })
                .ToListAsync();

            return songs;
        }

        // ─── GET A SINGLE SONG ────────────────────────────────────────────────

        public async Task<SongResponseDto?> GetSongByIdAsync(int songId, int userId)
        {
            var song = await _db.Songs
                .Where(s => s.Id == songId && s.UserId == userId)
                .Include(s => s.Album)
                    .ThenInclude(a => a!.Artist)
                .Select(s => new SongResponseDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    FilePath = s.FilePath,
                    Duration = s.Duration,
                    CoverArtUrl = s.CoverArtUrl,
                    AlbumTitle = s.Album != null ? s.Album.Title : null,
                    ArtistName = s.Album != null && s.Album.Artist != null
                        ? s.Album.Artist.Name
                        : null,
                    AddedAt = s.AddedAt
                })
                .FirstOrDefaultAsync();

            return song;
        }

        // ─── ADD A SONG ───────────────────────────────────────────────────────

        public async Task<SongResponseDto> AddSongAsync(int userId, AddSongDto dto)
        {
            var song = new Song
            {
                UserId = userId,
                Title = dto.Title,
                FilePath = dto.FilePath,
                AlbumId = dto.AlbumId,
                Duration = dto.Duration,
                CoverArtUrl = dto.CoverArtUrl,
                AddedAt = DateTime.UtcNow
            };

            _db.Songs.Add(song);
            await _db.SaveChangesAsync();

            // Return the full response DTO after saving
            return new SongResponseDto
            {
                Id = song.Id,
                Title = song.Title,
                FilePath = song.FilePath,
                Duration = song.Duration,
                CoverArtUrl = song.CoverArtUrl,
                AddedAt = song.AddedAt
            };
        }

        // ─── DELETE A SONG ────────────────────────────────────────────────────

        public async Task<bool> DeleteSongAsync(int songId, int userId)
        {
            // We check UserId too so a user cannot delete another user's song
            var song = await _db.Songs
                .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);

            if (song == null) return false;

            _db.Songs.Remove(song);
            await _db.SaveChangesAsync();
            return true;
        }

        // ─── GET SONG COUNT ───────────────────────────────────────────────────

        public async Task<int> GetSongCountAsync(int userId)
        {
            return await _db.Songs.CountAsync(s => s.UserId == userId);
        }
    }
}
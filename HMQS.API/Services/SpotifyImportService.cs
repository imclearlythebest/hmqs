using Microsoft.EntityFrameworkCore;
using HMQS.API.Data;
using HMQS.API.DTOs;
using HMQS.API.Models;

namespace HMQS.API.Services
{
    public class SpotifyImportService
    {
        private readonly AppDbContext _db;

        public SpotifyImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<SpotifyImportSummaryDto> ImportAsync(
            int userId,
            SpotifyImportRequestDto request)
        {
            var summary = new SpotifyImportSummaryDto
            {
                TotalTracks = request.TrackNames.Count
            };

            // Load the user's existing songs once
            // This avoids hitting the database for every single track
            var userSongs = await _db.Songs
                .Where(s => s.UserId == userId)
                .ToListAsync();

            foreach (var trackName in request.TrackNames)
            {
                if (string.IsNullOrWhiteSpace(trackName)) continue;

                // Try to find a matching song in the local library
                // We use a case-insensitive fuzzy match on the title
                // This is not perfect but works well for most cases
                var cleanTrack = trackName.Trim().ToLower();

                var matchedSong = userSongs.FirstOrDefault(s =>
                    s.Title.ToLower() == cleanTrack ||           // Exact match
                    s.Title.ToLower().Contains(cleanTrack) ||    // Local title contains Spotify name
                    cleanTrack.Contains(s.Title.ToLower())       // Spotify name contains local title
                );

                var status = matchedSong != null ? "Matched" : "NotFound";

                // Save this import record to the database
                var importRecord = new SpotifyImport
                {
                    UserId = userId,
                    SpotifyTrackId = trackName, // We store the name since we have no real Spotify ID
                    MatchedSongId = matchedSong?.Id,
                    ImportedAt = DateTime.UtcNow,
                    Status = status
                };

                _db.SpotifyImports.Add(importRecord);

                // Add to results list
                summary.Results.Add(new SpotifyImportResultDto
                {
                    SpotifyTrackName = trackName,
                    Status = status,
                    MatchedSongId = matchedSong?.Id,
                    MatchedSongTitle = matchedSong?.Title
                });

                if (status == "Matched")
                    summary.MatchedCount++;
                else
                    summary.NotFoundCount++;
            }

            await _db.SaveChangesAsync();

            return summary;
        }

        // Get all past imports for a user
        public async Task<List<SpotifyImportResultDto>> GetImportsAsync(int userId)
        {
            var imports = await _db.SpotifyImports
                .Where(si => si.UserId == userId)
                .Include(si => si.MatchedSong)
                .OrderByDescending(si => si.ImportedAt)
                .Select(si => new SpotifyImportResultDto
                {
                    SpotifyTrackName = si.SpotifyTrackId,
                    Status = si.Status,
                    MatchedSongId = si.MatchedSongId,
                    MatchedSongTitle = si.MatchedSong != null ? si.MatchedSong.Title : null
                })
                .ToListAsync();

            return imports;
        }

        // Count how many tracks have been imported
        public async Task<int> GetImportCountAsync(int userId)
        {
            return await _db.SpotifyImports
                .CountAsync(si => si.UserId == userId);
        }
    }
}
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.Dtos;

namespace WebApp.Controllers;

public class ScrobbleController(WebAppDbContext context, IHttpClientFactory httpClientFactory) : Controller
{
    private const string ItunesLookupBaseUrl = "https://itunes.apple.com/lookup?id=";
    private readonly WebAppDbContext _context = context;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ScrobbleDto model, CancellationToken cancellationToken)
    {
        if (model.ItunesTrackId <= 0)
        {
            return BadRequest(new { message = "itunesTrackId is required" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = string.IsNullOrWhiteSpace(userId)
            ? null
            : await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Unauthorized(new { message = "Login required for scrobbling." });
        }

        var unknownGenre = await GetOrCreateGenreAsync("Unknown", 0, cancellationToken);

        var artist = await EnsureArtistFromScrobbleAsync(model.ItunesArtistId, unknownGenre, cancellationToken);
        var collection = await EnsureCollectionFromScrobbleAsync(model.ItunesCollectionId, artist, unknownGenre, cancellationToken);
        var track = await EnsureTrackFromScrobbleAsync(model.ItunesTrackId, artist, collection, unknownGenre, cancellationToken);

        var scrobble = new Scrobble
        {
            UserId = user.Id,
            User = user,
            Track = track,
            ScrobbledAt = DateTime.UtcNow,
            Progress = model.Progress,
            DurationSeconds = model.DurationSeconds
        };

        _context.Scrobbles.Add(scrobble);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Scrobble submitted", userId, trackId = track.Id, model.Progress, model.DurationSeconds });
    }

    private async Task<Artist?> EnsureArtistFromScrobbleAsync(int? itunesArtistId, Genre unknownGenre, CancellationToken cancellationToken)
    {
        if (!itunesArtistId.HasValue || itunesArtistId.Value <= 0)
        {
            return null;
        }

        var artist = await GetOrCreateArtistPlaceholderAsync(itunesArtistId.Value, unknownGenre, cancellationToken);

        if (!artist.LastCrawledAtUtc.HasValue)
        {
            await CrawlArtistAsync(artist, unknownGenre, cancellationToken);
        }

        return artist;
    }

    private async Task<Collection?> EnsureCollectionFromScrobbleAsync(int? itunesCollectionId, Artist? knownArtist, Genre unknownGenre, CancellationToken cancellationToken)
    {
        if (!itunesCollectionId.HasValue || itunesCollectionId.Value <= 0)
        {
            return null;
        }

        var collection = await GetOrCreateCollectionPlaceholderAsync(itunesCollectionId.Value, knownArtist, unknownGenre, cancellationToken);

        if (knownArtist != null && collection.Artist == null)
        {
            AssignCollectionArtist(collection, knownArtist);
        }

        if (!collection.LastCrawledAtUtc.HasValue)
        {
            await CrawlCollectionAsync(collection, unknownGenre, cancellationToken);
        }

        return collection;
    }

    private async Task<Track> EnsureTrackFromScrobbleAsync(int itunesTrackId, Artist? knownArtist, Collection? knownCollection, Genre unknownGenre, CancellationToken cancellationToken)
    {
        var trackedTrack = _context.Tracks.Local.FirstOrDefault(t => t.ItunesTrackId == itunesTrackId);
        var track = trackedTrack ?? await _context.Tracks
            .Include(t => t.Artist)
            .Include(t => t.Genre)
            .Include(t => t.Collection)
            .FirstOrDefaultAsync(t => t.ItunesTrackId == itunesTrackId, cancellationToken);

        if (track == null)
        {
            track = new Track
            {
                ItunesTrackId = itunesTrackId,
                FileName = string.Empty,
                TrackName = "Unknown Track",
                Year = 0,
                PreviewUrl = string.Empty,
                ArtworkUrl = string.Empty,
                GenreName = unknownGenre.GenreName,
                Genre = unknownGenre,
            };
            _context.Tracks.Add(track);
        }

        if (knownArtist != null && track.Artist == null)
        {
            AssignTrackArtist(track, knownArtist);
        }

        if (knownCollection != null && track.Collection == null)
        {
            AssignTrackCollection(track, knownCollection);
        }

        if (track.Genre == null)
        {
            track.Genre = unknownGenre;
            track.GenreName = unknownGenre.GenreName;
        }

        if (!track.LastCrawledAtUtc.HasValue)
        {
            await CrawlTrackAsync(track, unknownGenre, cancellationToken);
        }

        return track;
    }

    private async Task CrawlArtistAsync(Artist artist, Genre unknownGenre, CancellationToken cancellationToken)
    {
        var lookup = await LookupByIdAsync(artist.ItunesArtistId, cancellationToken);
        if (!lookup.Success)
        {
            return;
        }

        var result = lookup.Results.FirstOrDefault(r => r.ArtistId == artist.ItunesArtistId);
        if (result != null)
        {
            if (!string.IsNullOrWhiteSpace(result.ArtistName))
            {
                artist.ArtistName = result.ArtistName.Trim();
            }

            var genre = await GetOrCreateGenreAsync(result.PrimaryGenreName, result.PrimaryGenreId, cancellationToken);
            artist.PrimaryGenre = genre;
            artist.PrimaryGenreName = genre.GenreName;
        }
        else if (artist.PrimaryGenre == null)
        {
            artist.PrimaryGenre = unknownGenre;
            artist.PrimaryGenreName = unknownGenre.GenreName;
        }

        artist.LastCrawledAtUtc = DateTime.UtcNow;
    }

    private async Task CrawlCollectionAsync(Collection collection, Genre unknownGenre, CancellationToken cancellationToken)
    {
        var lookup = await LookupByIdAsync(collection.ItunesCollectionId, cancellationToken);
        if (!lookup.Success)
        {
            return;
        }

        var result = lookup.Results.FirstOrDefault(r => r.CollectionId == collection.ItunesCollectionId);
        if (result != null)
        {
            if (result.ArtistId.HasValue && result.ArtistId.Value > 0)
            {
                var artist = await GetOrCreateArtistPlaceholderAsync(result.ArtistId.Value, unknownGenre, cancellationToken);
                if (collection.Artist == null)
                {
                    AssignCollectionArtist(collection, artist);
                }

                if (!artist.LastCrawledAtUtc.HasValue)
                {
                    await CrawlArtistAsync(artist, unknownGenre, cancellationToken);
                }
            }

            var genre = await GetOrCreateGenreAsync(result.PrimaryGenreName, result.PrimaryGenreId, cancellationToken);
            collection.Genre = genre;
            collection.GenreName = genre.GenreName;
        }
        else if (collection.Genre == null)
        {
            collection.Genre = unknownGenre;
            collection.GenreName = unknownGenre.GenreName;
        }

        collection.LastCrawledAtUtc = DateTime.UtcNow;
    }

    private async Task CrawlTrackAsync(Track track, Genre unknownGenre, CancellationToken cancellationToken)
    {
        var lookup = await LookupByIdAsync(track.ItunesTrackId, cancellationToken);
        if (!lookup.Success)
        {
            return;
        }

        var result = lookup.Results.FirstOrDefault(r => r.TrackId == track.ItunesTrackId);
        if (result != null)
        {
            if (!string.IsNullOrWhiteSpace(result.TrackName))
            {
                track.TrackName = result.TrackName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(result.PreviewUrl))
            {
                track.PreviewUrl = result.PreviewUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(result.ArtworkUrl100))
            {
                track.ArtworkUrl = result.ArtworkUrl100.Trim();
            }

            if (!string.IsNullOrWhiteSpace(result.ReleaseDate) &&
                DateTime.TryParse(result.ReleaseDate, out var releaseDate))
            {
                track.Year = releaseDate.Year;
            }

            if (result.ArtistId.HasValue && result.ArtistId.Value > 0)
            {
                var artist = await GetOrCreateArtistPlaceholderAsync(result.ArtistId.Value, unknownGenre, cancellationToken);
                if (track.Artist == null)
                {
                    AssignTrackArtist(track, artist);
                }

                if (!artist.LastCrawledAtUtc.HasValue)
                {
                    await CrawlArtistAsync(artist, unknownGenre, cancellationToken);
                }
            }

            if (result.CollectionId.HasValue && result.CollectionId.Value > 0)
            {
                var collection = await GetOrCreateCollectionPlaceholderAsync(result.CollectionId.Value, null, unknownGenre, cancellationToken);
                if (track.Collection == null)
                {
                    AssignTrackCollection(track, collection);
                }

                if (!collection.LastCrawledAtUtc.HasValue)
                {
                    await CrawlCollectionAsync(collection, unknownGenre, cancellationToken);
                }
            }

            var genre = await GetOrCreateGenreAsync(result.PrimaryGenreName, result.PrimaryGenreId, cancellationToken);
            track.Genre = genre;
            track.GenreName = genre.GenreName;
        }
        else if (track.Genre == null)
        {
            track.Genre = unknownGenre;
            track.GenreName = unknownGenre.GenreName;
        }

        track.LastCrawledAtUtc = DateTime.UtcNow;
    }

    private async Task<Artist> GetOrCreateArtistPlaceholderAsync(int itunesArtistId, Genre unknownGenre, CancellationToken cancellationToken)
    {
        var tracked = _context.Artists.Local.FirstOrDefault(a => a.ItunesArtistId == itunesArtistId);
        if (tracked != null)
        {
            if (tracked.PrimaryGenre == null)
            {
                tracked.PrimaryGenre = unknownGenre;
                tracked.PrimaryGenreName = unknownGenre.GenreName;
            }

            return tracked;
        }

        var existing = await _context.Artists
            .Include(a => a.PrimaryGenre)
            .FirstOrDefaultAsync(a => a.ItunesArtistId == itunesArtistId, cancellationToken);

        if (existing != null)
        {
            if (existing.PrimaryGenre == null)
            {
                existing.PrimaryGenre = unknownGenre;
                existing.PrimaryGenreName = unknownGenre.GenreName;
            }

            return existing;
        }

        var artist = new Artist
        {
            ItunesArtistId = itunesArtistId,
            ArtistName = "Unknown Artist",
            PrimaryGenre = unknownGenre,
            PrimaryGenreName = unknownGenre.GenreName,
            LastCrawledAtUtc = null,
        };

        _context.Artists.Add(artist);
        return artist;
    }

    private async Task<Collection> GetOrCreateCollectionPlaceholderAsync(int itunesCollectionId, Artist? knownArtist, Genre unknownGenre, CancellationToken cancellationToken)
    {
        var tracked = _context.Albums.Local.FirstOrDefault(c => c.ItunesCollectionId == itunesCollectionId);
        if (tracked != null)
        {
            if (tracked.Genre == null)
            {
                tracked.Genre = unknownGenre;
                tracked.GenreName = unknownGenre.GenreName;
            }

            if (knownArtist != null && tracked.Artist == null)
            {
                AssignCollectionArtist(tracked, knownArtist);
            }

            return tracked;
        }

        var existing = await _context.Albums
            .Include(c => c.Artist)
            .Include(c => c.Genre)
            .FirstOrDefaultAsync(c => c.ItunesCollectionId == itunesCollectionId, cancellationToken);

        if (existing != null)
        {
            if (existing.Genre == null)
            {
                existing.Genre = unknownGenre;
                existing.GenreName = unknownGenre.GenreName;
            }

            if (knownArtist != null && existing.Artist == null)
            {
                AssignCollectionArtist(existing, knownArtist);
            }

            return existing;
        }

        var collection = new Collection
        {
            ItunesCollectionId = itunesCollectionId,
            Genre = unknownGenre,
            GenreName = unknownGenre.GenreName,
            LastCrawledAtUtc = null,
        };

        if (knownArtist != null)
        {
            AssignCollectionArtist(collection, knownArtist);
        }

        _context.Albums.Add(collection);
        return collection;
    }

    private static void AssignTrackArtist(Track track, Artist artist)
    {
        track.Artist = artist;
        track.ArtistId = artist.Id > 0 ? artist.Id : null;
    }

    private static void AssignTrackCollection(Track track, Collection collection)
    {
        track.Collection = collection;
        track.CollectionId = collection.Id > 0 ? collection.Id : null;
    }

    private static void AssignCollectionArtist(Collection collection, Artist artist)
    {
        collection.Artist = artist;
        collection.ArtistId = artist.Id > 0 ? artist.Id : null;
    }

    private async Task<Genre> GetOrCreateGenreAsync(string? genreName, int? itunesGenreId, CancellationToken cancellationToken)
    {
        var normalizedName = string.IsNullOrWhiteSpace(genreName) ? "Unknown" : genreName.Trim();
        var tracked = _context.Genres.Local.FirstOrDefault(g => g.GenreName == normalizedName);
        if (tracked != null)
        {
            if (tracked.ItunesGenreId <= 0 && itunesGenreId.HasValue && itunesGenreId.Value > 0)
            {
                tracked.ItunesGenreId = itunesGenreId.Value;
            }

            return tracked;
        }

        var genre = await _context.Genres.FirstOrDefaultAsync(g => g.GenreName == normalizedName, cancellationToken);
        if (genre != null)
        {
            if (genre.ItunesGenreId <= 0 && itunesGenreId.HasValue && itunesGenreId.Value > 0)
            {
                genre.ItunesGenreId = itunesGenreId.Value;
            }

            return genre;
        }

        genre = new Genre
        {
            GenreName = normalizedName,
            ItunesGenreId = itunesGenreId.HasValue && itunesGenreId.Value > 0 ? itunesGenreId.Value : 0,
        };

        _context.Genres.Add(genre);
        return genre;
    }

    private async Task<LookupEnvelope> LookupByIdAsync(int itunesId, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync($"{ItunesLookupBaseUrl}{itunesId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new LookupEnvelope(false, []);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ItunesLookupResponse>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            },
            cancellationToken);

        return new LookupEnvelope(true, payload?.Results ?? []);
    }

    private sealed record LookupEnvelope(bool Success, List<ItunesLookupResult> Results);

    private sealed class ItunesLookupResponse
    {
        public List<ItunesLookupResult> Results { get; init; } = [];
    }

    private sealed class ItunesLookupResult
    {
        public int? ArtistId { get; init; }
        public int? CollectionId { get; init; }
        public int? TrackId { get; init; }
        public int? PrimaryGenreId { get; init; }

        public string? ArtistName { get; init; }
        public string? TrackName { get; init; }
        public string? PrimaryGenreName { get; init; }
        public string? PreviewUrl { get; init; }
        public string? ArtworkUrl100 { get; init; }
        public string? ReleaseDate { get; init; }
    }

    [Authorize]
    [HttpGet]
    public IActionResult History()
    {
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Scrobbles
            .Include(s => s.Track)
            .ThenInclude(t => t.Artist)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(s => s.UserId == userId);
        }

        var history = query
            .OrderByDescending(s => s.ScrobbledAt)
            .Take(100)
            .ToList();

        return View(history);
    }
}
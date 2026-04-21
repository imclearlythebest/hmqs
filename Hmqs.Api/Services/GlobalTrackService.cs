using Hmqs.Api.Data;
using Hmqs.Api.Dtos;
using Hmqs.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Hmqs.Api.Services;

public class GlobalTrackService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;

    public GlobalTrackService(AppDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<GlobalTrackMatchDto>> SearchMatchesAsync(Guid ownerId, Guid trackId, int limit = 5, CancellationToken cancellationToken = default)
    {
        var track = await _context.LocalTracks
            .Where(t => t.Id == trackId && t.ListenerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (track is null)
        {
            throw new InvalidOperationException("Local track not found or access denied.");
        }

        var term = BuildSearchTerm(track);
        if (string.IsNullOrWhiteSpace(term))
        {
            return Array.Empty<GlobalTrackMatchDto>();
        }

        var safeLimit = Math.Clamp(limit, 1, 20);
        var response = await _httpClient.GetFromJsonAsync<ItunesSearchResponse>($"search?media=music&entity=song&limit={safeLimit}&term={Uri.EscapeDataString(term)}", cancellationToken);

        return response?.Results.Select(MapSearchResult).ToList() ?? Enumerable.Empty<GlobalTrackMatchDto>();
    }

    public async Task<LocalTrackResponseDto> SelectMatchAsync(Guid ownerId, Guid trackId, GlobalTrackMatchSelectionDto model, CancellationToken cancellationToken = default)
    {
        var track = await _context.LocalTracks
            .Where(t => t.Id == trackId && t.ListenerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (track is null)
        {
            throw new InvalidOperationException("Local track not found or access denied.");
        }

        var trackResult = await GetTrackLookupAsync(model.ExternalTrackId, cancellationToken);
        if (trackResult is null)
        {
            throw new InvalidOperationException("Could not find matching iTunes track.");
        }

        if (trackResult.TrackId is null || trackResult.TrackId <= 0)
        {
            throw new InvalidOperationException("iTunes lookup did not return a valid track id.");
        }

        if (trackResult.ArtistId is null || trackResult.ArtistId <= 0)
        {
            throw new InvalidOperationException("iTunes lookup did not return a valid artist id.");
        }

        if (trackResult.CollectionId is null || trackResult.CollectionId <= 0)
        {
            throw new InvalidOperationException("iTunes lookup did not return a valid album id.");
        }

        var artistResult = await GetArtistLookupAsync(trackResult.ArtistId.Value, cancellationToken);
        var albumResult = await GetAlbumLookupAsync(trackResult.CollectionId.Value, cancellationToken);

        if (artistResult is null)
        {
            throw new InvalidOperationException("Could not resolve artist details from iTunes.");
        }

        if (albumResult is null)
        {
            throw new InvalidOperationException("Could not resolve album details from iTunes.");
        }

        var artist = await GetOrCreateArtistAsync(
            artistResult.ArtistId ?? trackResult.ArtistId.Value,
            artistResult.ArtistName ?? trackResult.ArtistName ?? string.Empty,
            artistResult.PrimaryGenreName ?? trackResult.PrimaryGenreName,
            cancellationToken);

        var album = await GetOrCreateAlbumAsync(
            albumResult.CollectionId ?? trackResult.CollectionId.Value,
            albumResult.CollectionName ?? trackResult.CollectionName,
            albumResult.ReleaseYear ?? trackResult.ReleaseYear,
            albumResult.PrimaryGenreName ?? trackResult.PrimaryGenreName,
            artist.Id,
            albumResult.ArtworkUrl100 ?? trackResult.ArtworkUrl100,
            albumResult.CollectionPrice ?? trackResult.CollectionPrice,
            cancellationToken);

        var globalTrack = await GetOrCreateGlobalTrackAsync(
            trackResult.TrackId.Value,
            trackResult.TrackName ?? string.Empty,
            trackResult.PrimaryGenreName,
            trackResult.ReleaseYear,
            trackResult.ArtworkUrl100,
            artist.Id,
            album.Id,
            cancellationToken);

        track.GlobalTrackId = globalTrack.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Map(track);
    }

    private async Task<Artist> GetOrCreateArtistAsync(int externalId, string name, string? genre, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(externalId);

        var artist = await _context.Artists
            .Where(a => a.ExternalId == externalId)
            .SingleOrDefaultAsync(cancellationToken);

        if (artist is null)
        {
            artist = new Artist
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = name,
                PrimaryGenre = genre
            };

            _context.Artists.Add(artist);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (artist.PrimaryGenre is null && genre is not null)
        {
            artist.PrimaryGenre = genre;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return artist;
    }

    private async Task<ItunesLookupResult?> GetTrackLookupAsync(int externalTrackId, CancellationToken cancellationToken)
    {
        if (externalTrackId <= 0)
        {
            return null;
        }

        var lookup = await _httpClient.GetFromJsonAsync<ItunesLookupResponse>($"lookup?id={externalTrackId}", cancellationToken);
        return lookup?.Results?.SingleOrDefault(r => string.Equals(r.WrapperType, "track", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Kind, "song", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ItunesLookupResult?> GetArtistLookupAsync(int externalArtistId, CancellationToken cancellationToken)
    {
        if (externalArtistId <= 0)
        {
            return null;
        }

        var lookup = await _httpClient.GetFromJsonAsync<ItunesLookupResponse>($"lookup?id={externalArtistId}", cancellationToken);
        return lookup?.Results?.SingleOrDefault(r => string.Equals(r.WrapperType, "artist", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ItunesLookupResult?> GetAlbumLookupAsync(int externalCollectionId, CancellationToken cancellationToken)
    {
        if (externalCollectionId <= 0)
        {
            return null;
        }

        var lookup = await _httpClient.GetFromJsonAsync<ItunesLookupResponse>($"lookup?id={externalCollectionId}", cancellationToken);
        return lookup?.Results?.SingleOrDefault(r => string.Equals(r.WrapperType, "collection", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Kind, "album", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Album> GetOrCreateAlbumAsync(int externalId, string? title, int? year, string? genre, Guid artistId, string? coverArt, decimal? price, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(externalId);

        var album = await _context.Albums
            .Where(a => a.ExternalId == externalId)
            .SingleOrDefaultAsync(cancellationToken);

        if (album is null)
        {
            album = new Album
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Title = title ?? string.Empty,
                Year = year,
                PrimaryGenre = genre,
                CoverArt = coverArt,
                Price = price ?? 0m,
                ArtistId = artistId
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var updated = false;
            if (string.IsNullOrWhiteSpace(album.Title) && !string.IsNullOrWhiteSpace(title))
            {
                album.Title = title!;
                updated = true;
            }

            if (album.Year is null && year is not null)
            {
                album.Year = year;
                updated = true;
            }

            if (album.PrimaryGenre is null && genre is not null)
            {
                album.PrimaryGenre = genre;
                updated = true;
            }

            if (album.ArtistId == Guid.Empty)
            {
                album.ArtistId = artistId;
                updated = true;
            }

            if (album.CoverArt is null && coverArt is not null)
            {
                album.CoverArt = coverArt;
                updated = true;
            }

            if (album.Price <= 0m && price.HasValue)
            {
                album.Price = price.Value;
                updated = true;
            }

            if (updated)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return album;
    }

    private async Task<GlobalTrack> GetOrCreateGlobalTrackAsync(int externalId, string title, string? genre, int? year, string? artworkUrl, Guid? artistId, Guid? albumId, CancellationToken cancellationToken)
    {
        var globalTrack = await _context.GlobalTracks
            .Where(g => g.ExternalId == externalId)
            .SingleOrDefaultAsync(cancellationToken);

        if (globalTrack is null)
        {
            globalTrack = new GlobalTrack
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                TrackTitle = title,
                Genre = genre,
                Year = year,
                ArtworkUrl = artworkUrl,
                ArtistId = artistId,
                AlbumId = albumId
            };

            _context.GlobalTracks.Add(globalTrack);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var updated = false;
            if (string.IsNullOrWhiteSpace(globalTrack.TrackTitle) && !string.IsNullOrWhiteSpace(title))
            {
                globalTrack.TrackTitle = title;
                updated = true;
            }

            if (globalTrack.ArtistId is null && artistId is not null)
            {
                globalTrack.ArtistId = artistId;
                updated = true;
            }

            if (globalTrack.AlbumId is null && albumId is not null)
            {
                globalTrack.AlbumId = albumId;
                updated = true;
            }

            if (globalTrack.Year is null && year is not null)
            {
                globalTrack.Year = year;
                updated = true;
            }

            if (globalTrack.ArtworkUrl is null && artworkUrl is not null)
            {
                globalTrack.ArtworkUrl = artworkUrl;
                updated = true;
            }

            if (updated)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return globalTrack;
    }

    private static GlobalTrackMatchDto MapSearchResult(ItunesSearchResult result)
    {
        return new GlobalTrackMatchDto
        {
            ExternalTrackId = result.TrackId,
            ArtistExternalId = result.ArtistId,
            AlbumExternalId = result.CollectionId,
            TrackTitle = result.TrackName,
            ArtistName = result.ArtistName,
            AlbumTitle = result.CollectionName ?? string.Empty,
            Genre = result.PrimaryGenreName,
            Year = result.ReleaseYear,
            ArtworkUrl = result.ArtworkUrl100
        };
    }

    private static string BuildSearchTerm(LocalTrack track)
    {
        if (!string.IsNullOrWhiteSpace(track.TrackTitle) && !string.IsNullOrWhiteSpace(track.Artist) && !string.IsNullOrWhiteSpace(track.Album))
        {
            var parts = new[] { track.TrackTitle, track.Artist, track.Album }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim());

            return string.Join(" ", parts);
        }

        if (!string.IsNullOrWhiteSpace(track.FileName))
        {
            var fileName = Path.GetFileNameWithoutExtension(track.FileName).Replace('_', ' ');
            return fileName.Trim();
        }

        return string.Empty;
    }

    private sealed record ItunesSearchResponse(
        [property: JsonPropertyName("resultCount")] int ResultCount,
        [property: JsonPropertyName("results")] List<ItunesSearchResult> Results);

    private sealed record ItunesSearchResult(
        [property: JsonPropertyName("trackId")] int TrackId,
        [property: JsonPropertyName("trackName")] string TrackName,
        [property: JsonPropertyName("artistName")] string ArtistName,
        [property: JsonPropertyName("collectionName")] string? CollectionName,
        [property: JsonPropertyName("collectionId")] int CollectionId,
        [property: JsonPropertyName("artistId")] int ArtistId,
        [property: JsonPropertyName("primaryGenreName")] string? PrimaryGenreName,
        [property: JsonPropertyName("releaseDate")] string? ReleaseDate,
        [property: JsonPropertyName("artworkUrl100")] string? ArtworkUrl100,
        [property: JsonPropertyName("collectionPrice")] decimal? CollectionPrice)
    {
        public int? ReleaseYear => DateTime.TryParse(ReleaseDate, out var parsed) ? parsed.Year : null;
    }

    private sealed record ItunesLookupResponse(
        [property: JsonPropertyName("resultCount")] int ResultCount,
        [property: JsonPropertyName("results")] List<ItunesLookupResult> Results);

    private sealed record ItunesLookupResult(
        [property: JsonPropertyName("wrapperType")] string? WrapperType,
        [property: JsonPropertyName("kind")] string? Kind,
        [property: JsonPropertyName("trackId")] int? TrackId,
        [property: JsonPropertyName("trackName")] string? TrackName,
        [property: JsonPropertyName("artistName")] string? ArtistName,
        [property: JsonPropertyName("collectionName")] string? CollectionName,
        [property: JsonPropertyName("collectionId")] int? CollectionId,
        [property: JsonPropertyName("artistId")] int? ArtistId,
        [property: JsonPropertyName("primaryGenreName")] string? PrimaryGenreName,
        [property: JsonPropertyName("releaseDate")] string? ReleaseDate,
        [property: JsonPropertyName("artworkUrl100")] string? ArtworkUrl100,
        [property: JsonPropertyName("collectionPrice")] decimal? CollectionPrice)
    {
        public int? ReleaseYear => DateTime.TryParse(ReleaseDate, out var parsed) ? parsed.Year : null;
    }

    private static LocalTrackResponseDto Map(LocalTrack track)
    {
        return new LocalTrackResponseDto
        {
            Id = track.Id,
            FileName = track.FileName,
            TrackTitle = track.TrackTitle,
            Artist = track.Artist,
            Album = track.Album,
            Genre = track.Genre,
            Year = track.Year,
            ArtworkUrl = track.ArtworkUrl,
            GlobalTrackId = track.GlobalTrackId
        };
    }
}

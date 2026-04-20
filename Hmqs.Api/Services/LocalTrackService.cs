using Hmqs.Api.Data;
using Hmqs.Api.Dtos;
using Hmqs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hmqs.Api.Services;

public class LocalTrackService
{
    private readonly AppDbContext _context;

    public LocalTrackService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LocalTrackResponseDto> CreateTrackAsync(Guid ownerId, LocalTrackCreateDto model, CancellationToken cancellationToken = default)
    {
        var track = new LocalTrack
        {
            Id = Guid.NewGuid(),
            ListenerId = ownerId,
            FileName = model.FileName,
            TrackTitle = model.TrackTitle,
            Artist = model.Artist,
            Album = model.Album,
            Genre = model.Genre,
            Year = model.Year,
            ArtworkUrl = model.ArtworkUrl
        };

        _context.LocalTracks.Add(track);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(track);
    }

    public async Task<LocalTrackResponseDto> UpdateTrackAsync(Guid ownerId, Guid trackId, LocalTrackUpdateDto model, CancellationToken cancellationToken = default)
    {
        var track = await _context.LocalTracks
            .Where(t => t.Id == trackId && t.ListenerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (track is null)
        {
            throw new InvalidOperationException("Local track not found or access denied.");
        }

        if (model.FileName is not null)
        {
            track.FileName = model.FileName;
        }

        if (model.TrackTitle is not null)
        {
            track.TrackTitle = model.TrackTitle;
        }

        if (model.Artist is not null)
        {
            track.Artist = model.Artist;
        }

        if (model.Album is not null)
        {
            track.Album = model.Album;
        }

        if (model.Genre is not null)
        {
            track.Genre = model.Genre;
        }

        if (model.Year is not null)
        {
            track.Year = model.Year;
        }

        if (model.ArtworkUrl is not null)
        {
            track.ArtworkUrl = model.ArtworkUrl;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Map(track);
    }

    public async Task DeleteTrackAsync(Guid ownerId, Guid trackId, CancellationToken cancellationToken = default)
    {
        var track = await _context.LocalTracks
            .Where(t => t.Id == trackId && t.ListenerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (track is null)
        {
            throw new InvalidOperationException("Local track not found or access denied.");
        }

        _context.LocalTracks.Remove(track);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<LocalTrackResponseDto>> GetUserTracksAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.LocalTracks
            .Where(t => t.ListenerId == ownerId)
            .OrderBy(t => t.TrackTitle)
            .Select(t => new LocalTrackResponseDto
            {
                Id = t.Id,
                FileName = t.FileName,
                TrackTitle = t.TrackTitle,
                Artist = t.Artist,
                Album = t.Album,
                Genre = t.Genre,
                Year = t.Year,
                ArtworkUrl = t.ArtworkUrl
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LocalTrackResponseDto?> GetTrackAsync(Guid ownerId, Guid trackId, CancellationToken cancellationToken = default)
    {
        return await _context.LocalTracks
            .Where(t => t.Id == trackId && t.ListenerId == ownerId)
            .Select(t => new LocalTrackResponseDto
            {
                Id = t.Id,
                FileName = t.FileName,
                TrackTitle = t.TrackTitle,
                Artist = t.Artist,
                Album = t.Album,
                Genre = t.Genre,
                Year = t.Year,
                ArtworkUrl = t.ArtworkUrl
            })
            .SingleOrDefaultAsync(cancellationToken);
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
            ArtworkUrl = track.ArtworkUrl
        };
    }
}

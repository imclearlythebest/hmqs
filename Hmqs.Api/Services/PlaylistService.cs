using Hmqs.Api.Data;
using Hmqs.Api.Dtos;
using Hmqs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hmqs.Api.Services;

public class PlaylistService
{
    private readonly AppDbContext _context;

    public PlaylistService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PlaylistResponseDto> CreatePlaylistAsync(Guid ownerId, PlaylistCreateDto model, CancellationToken cancellationToken = default)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            Description = model.Description,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Playlists.Add(playlist);
        await _context.SaveChangesAsync(cancellationToken);

        return new PlaylistResponseDto
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt
        };
    }

    public async Task<PlaylistResponseDto> UpdatePlaylistAsync(Guid ownerId, Guid playlistId, PlaylistUpdateDto model, CancellationToken cancellationToken = default)
    {
        var playlist = await _context.Playlists
            .Where(p => p.Id == playlistId && p.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (playlist is null)
        {
            throw new InvalidOperationException("Playlist not found or access denied.");
        }

        if (model.Name is not null)
        {
            playlist.Name = model.Name;
        }

        if (model.Description is not null)
        {
            playlist.Description = model.Description;
        }

        playlist.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new PlaylistResponseDto
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt
        };
    }

    public async Task DeletePlaylistAsync(Guid ownerId, Guid playlistId, CancellationToken cancellationToken = default)
    {
        var playlist = await _context.Playlists
            .Where(p => p.Id == playlistId && p.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (playlist is null)
        {
            throw new InvalidOperationException("Playlist not found or access denied.");
        }

        _context.Playlists.Remove(playlist);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlaylistTrackResponseDto> AddTrackAsync(Guid ownerId, Guid playlistId, PlaylistTrackCreateDto model, CancellationToken cancellationToken = default)
    {
        var playlist = await _context.Playlists
            .Where(p => p.Id == playlistId && p.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (playlist is null)
        {
            throw new InvalidOperationException("Playlist not found or access denied.");
        }

        var position = await _context.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlistId)
            .MaxAsync(pt => (int?)pt.Position, cancellationToken) ?? -1;

        var entry = new PlaylistTrack
        {
            PlaylistId = playlistId,
            LocalTrackId = model.LocalTrackId,
            Position = position + 1,
            AddedAt = DateTime.UtcNow
        };

        _context.PlaylistTracks.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return new PlaylistTrackResponseDto
        {
            Id = entry.Id,
            LocalTrackId = entry.LocalTrackId,
            Position = entry.Position,
            AddedAt = entry.AddedAt
        };
    }

    public async Task<PlaylistTrackResponseDto> MoveTrackAsync(Guid ownerId, Guid playlistId, int trackId, int position, CancellationToken cancellationToken = default)
    {
        var playlist = await _context.Playlists
            .Where(p => p.Id == playlistId && p.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (playlist is null)
        {
            throw new InvalidOperationException("Playlist not found or access denied.");
        }

        var tracks = await _context.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlistId)
            .OrderBy(pt => pt.Position)
            .ToListAsync(cancellationToken);

        var track = tracks.SingleOrDefault(pt => pt.Id == trackId);
        if (track is null)
        {
            throw new InvalidOperationException("Playlist track not found.");
        }

        tracks.Remove(track);
        var newPosition = Math.Clamp(position, 0, tracks.Count);
        tracks.Insert(newPosition, track);

        for (var index = 0; index < tracks.Count; index++)
        {
            tracks[index].Position = index;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PlaylistTrackResponseDto
        {
            Id = track.Id,
            LocalTrackId = track.LocalTrackId,
            Position = track.Position,
            AddedAt = track.AddedAt
        };
    }

    public async Task RemoveTrackAsync(Guid ownerId, Guid playlistId, int trackId, CancellationToken cancellationToken = default)
    {
        var playlist = await _context.Playlists
            .Where(p => p.Id == playlistId && p.OwnerId == ownerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (playlist is null)
        {
            throw new InvalidOperationException("Playlist not found or access denied.");
        }

        var track = await _context.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlistId && pt.Id == trackId)
            .SingleOrDefaultAsync(cancellationToken);

        if (track is null)
        {
            throw new InvalidOperationException("Playlist track not found.");
        }

        _context.PlaylistTracks.Remove(track);
        await _context.SaveChangesAsync(cancellationToken);

        var remaining = await _context.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlistId)
            .OrderBy(pt => pt.Position)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].Position = index;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<PlaylistResponseDto>> GetUserPlaylistsAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Playlists
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PlaylistResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Tracks = p.PlaylistTracks
                    .OrderBy(pt => pt.Position)
                    .Select(pt => new PlaylistTrackResponseDto
                    {
                        Id = pt.Id,
                        LocalTrackId = pt.LocalTrackId,
                        Position = pt.Position,
                        AddedAt = pt.AddedAt
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PlaylistResponseDto?> GetPlaylistAsync(Guid ownerId, Guid playlistId, CancellationToken cancellationToken = default)
    {
        return await _context.Playlists
            .Where(p => p.Id == playlistId && p.OwnerId == ownerId)
            .Select(p => new PlaylistResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Tracks = p.PlaylistTracks
                    .OrderBy(pt => pt.Position)
                    .Select(pt => new PlaylistTrackResponseDto
                    {
                        Id = pt.Id,
                        LocalTrackId = pt.LocalTrackId,
                        Position = pt.Position,
                        AddedAt = pt.AddedAt
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}

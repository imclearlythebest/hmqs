using Hmqs.Api.Data;
using Hmqs.Api.Dtos;
using Hmqs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hmqs.Api.Services;

public class ScrobbleService
{
    private readonly AppDbContext _context;

    public ScrobbleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ScrobbleResponseDto> CreateScrobbleAsync(Guid listenerId, ScrobbleDto model, CancellationToken cancellationToken = default)
    {
        var scrobble = new Scrobble
        {
            LocalTrackId = model.LocalTrackId,
            ListenerId = listenerId,
            ScrobbleTime = model.ScrobbleTime,
            TrackDuration = model.TrackDuration,
            ListenedDuration = model.ListenedDuration
        };

        _context.Scrobbles.Add(scrobble);
        await _context.SaveChangesAsync(cancellationToken);

        return new ScrobbleResponseDto
        {
            Id = scrobble.Id,
            LocalTrackId = scrobble.LocalTrackId,
            ScrobbleTime = scrobble.ScrobbleTime,
            TrackDuration = scrobble.TrackDuration,
            ListenedDuration = scrobble.ListenedDuration
        };
    }

    public async Task<IEnumerable<ScrobbleResponseDto>> GetScrobblesAsync(Guid listenerId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.Scrobbles
            .Where(s => s.ListenerId == listenerId)
            .OrderByDescending(s => s.ScrobbleTime)
            .Take(limit)
            .Select(s => new ScrobbleResponseDto
            {
                Id = s.Id,
                LocalTrackId = s.LocalTrackId,
                ScrobbleTime = s.ScrobbleTime,
                TrackDuration = s.TrackDuration,
                ListenedDuration = s.ListenedDuration
            })
            .ToListAsync(cancellationToken);
    }
}

using Hmqs.Api.Data;
using Hmqs.Api.Dtos;
using Hmqs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hmqs.Api.Services;

public class EthicalCalculatorService
{
    private readonly AppDbContext _context;

    public EthicalCalculatorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EthicalCalculatorResultDto> GetCalculatorAsync(Guid listenerId, decimal monthlyBudget, CancellationToken cancellationToken = default)
    {
        var normalizedBudget = NormalizeMonthlyBudget(monthlyBudget);

        var listenerArtistListening = await LoadListenerArtistListeningAsync(listenerId, cancellationToken);
        if (listenerArtistListening.Count == 0)
        {
            return new EthicalCalculatorResultDto
            {
                MonthlyBudget = normalizedBudget,
                Artists = []
            };
        }

        var artistIds = listenerArtistListening.Select(x => x.ArtistId).ToList();
        var globalArtistListening = await LoadGlobalArtistListeningAsync(cancellationToken);
        var deprioritizedByEthicalByArtist = await LoadDeprioritizedByEthicalCountsAsync(listenerId, artistIds, cancellationToken);
        var artistNamesById = await _context.Artists
            .AsNoTracking()
            .Where(a => artistIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var allocations = CalculateArtistAllocations(
            listenerArtistListening,
            globalArtistListening,
            deprioritizedByEthicalByArtist,
            artistNamesById,
            normalizedBudget);

        return new EthicalCalculatorResultDto
        {
            MonthlyBudget = normalizedBudget,
            Artists = allocations
        };
    }

    public async Task<EthicalDummyPurchaseResultDto> ApplyDummyPurchaseAsync(Guid listenerId, Guid purchasedArtistId, decimal monthlyBudget, CancellationToken cancellationToken = default)
    {
        var normalizedBudget = NormalizeMonthlyBudget(monthlyBudget);
        var listenerArtistListening = await LoadListenerArtistListeningAsync(listenerId, cancellationToken);
        var byArtist = listenerArtistListening.ToDictionary(x => x.ArtistId, x => x.Seconds);

        if (!byArtist.ContainsKey(purchasedArtistId))
        {
            var skippedCalculator = await GetCalculatorAsync(listenerId, normalizedBudget, cancellationToken);
            return new EthicalDummyPurchaseResultDto
            {
                Message = "Dummy purchase skipped because this artist has no listening history yet.",
                AffectedArtistsCount = 0,
                Calculator = skippedCalculator
            };
        }

        var purchasedSeconds = byArtist.GetValueOrDefault(purchasedArtistId, 0);
        var affectedArtistIds = byArtist
            .Where(x => x.Key != purchasedArtistId && x.Value > purchasedSeconds)
            .Select(x => x.Key)
            .ToList();

        var trackedPreferences = await _context.ListenerArtistPreferences
            .Where(x => x.ListenerId == listenerId && (affectedArtistIds.Contains(x.ArtistId) || x.ArtistId == purchasedArtistId))
            .ToListAsync(cancellationToken);

        var preferencesByArtist = trackedPreferences.ToDictionary(x => x.ArtistId, x => x);

        if (preferencesByArtist.TryGetValue(purchasedArtistId, out var purchasedPreference) && purchasedPreference.DeprioritizedByEthicalCount > 0)
        {
            purchasedPreference.DeprioritizedByEthicalCount = 0;
        }

        foreach (var artistId in affectedArtistIds)
        {
            if (!preferencesByArtist.TryGetValue(artistId, out var preference))
            {
                preference = new ListenerArtistPreference
                {
                    ListenerId = listenerId,
                    ArtistId = artistId,
                    DeprioritizedByEthicalCount = 0
                };
                _context.ListenerArtistPreferences.Add(preference);
                preferencesByArtist[artistId] = preference;
            }

            preference.DeprioritizedByEthicalCount += 1;
        }

        if (affectedArtistIds.Count > 0 || purchasedPreference is not null)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var calculator = await GetCalculatorAsync(listenerId, normalizedBudget, cancellationToken);
        return new EthicalDummyPurchaseResultDto
        {
            Message = $"Dummy purchase applied. Incremented deprioritized counts for {affectedArtistIds.Count} artist(s).",
            AffectedArtistsCount = affectedArtistIds.Count,
            Calculator = calculator
        };
    }

    private async Task<List<ArtistListeningRow>> LoadListenerArtistListeningAsync(Guid listenerId, CancellationToken cancellationToken)
    {
        return await _context.Scrobbles
            .AsNoTracking()
            .Where(s => s.ListenerId == listenerId && s.ListenedDuration > 0)
            .Join(
                _context.LocalTracks.AsNoTracking().Where(t => t.GlobalTrackId.HasValue),
                scrobble => scrobble.LocalTrackId,
                track => track.Id,
                (scrobble, track) => new { scrobble.ListenedDuration, track.GlobalTrackId })
            .Join(
                _context.GlobalTracks.AsNoTracking().Where(g => g.ArtistId.HasValue),
                row => row.GlobalTrackId!.Value,
                globalTrack => globalTrack.Id,
                (row, globalTrack) => new { ArtistId = globalTrack.ArtistId!.Value, row.ListenedDuration })
            .GroupBy(row => row.ArtistId)
            .Select(group => new ArtistListeningRow(group.Key, group.Sum(x => x.ListenedDuration)))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ArtistListeningRow>> LoadGlobalArtistListeningAsync(CancellationToken cancellationToken)
    {
        return await _context.Scrobbles
            .AsNoTracking()
            .Where(s => s.ListenedDuration > 0)
            .Join(
                _context.LocalTracks.AsNoTracking().Where(t => t.GlobalTrackId.HasValue),
                scrobble => scrobble.LocalTrackId,
                track => track.Id,
                (scrobble, track) => new { scrobble.ListenedDuration, track.GlobalTrackId })
            .Join(
                _context.GlobalTracks.AsNoTracking().Where(g => g.ArtistId.HasValue),
                row => row.GlobalTrackId!.Value,
                globalTrack => globalTrack.Id,
                (row, globalTrack) => new { ArtistId = globalTrack.ArtistId!.Value, row.ListenedDuration })
            .GroupBy(row => row.ArtistId)
            .Select(group => new ArtistListeningRow(group.Key, group.Sum(x => x.ListenedDuration)))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> LoadDeprioritizedByEthicalCountsAsync(Guid listenerId, IEnumerable<Guid> artistIds, CancellationToken cancellationToken)
    {
        return await _context.ListenerArtistPreferences
            .AsNoTracking()
            .Where(x => x.ListenerId == listenerId && artistIds.Contains(x.ArtistId))
            .Select(x => new { x.ArtistId, x.DeprioritizedByEthicalCount })
            .ToDictionaryAsync(x => x.ArtistId, x => x.DeprioritizedByEthicalCount, cancellationToken);
    }

    private static List<EthicalArtistAllocationDto> CalculateArtistAllocations(
        IReadOnlyList<ArtistListeningRow> listenerArtistListening,
        IReadOnlyList<ArtistListeningRow> globalArtistListening,
        IReadOnlyDictionary<Guid, int> deprioritizedByEthicalByArtist,
        IReadOnlyDictionary<Guid, string> artistNamesById,
        decimal monthlyBudget)
    {
        var weightedListeningSum = listenerArtistListening.Sum(x => Math.Sqrt(Math.Max(0, x.Seconds)));
        var maxGlobalSeconds = Math.Max(1, globalArtistListening.Select(x => x.Seconds).DefaultIfEmpty(0).Max());
        var maxGlobalLog = Math.Log(1 + maxGlobalSeconds);

        var globalListeningByArtist = globalArtistListening.ToDictionary(x => x.ArtistId, x => x.Seconds);

        const decimal alpha = 0.75m;
        const decimal beta = 0.25m;

        var scored = new List<(EthicalArtistAllocationDto dto, decimal rawScore, decimal rawListeningShare)>();
        foreach (var row in listenerArtistListening)
        {
            var globalListenedSeconds = globalListeningByArtist.GetValueOrDefault(row.ArtistId, 0);
            var deprioritizedByEthicalCount = deprioritizedByEthicalByArtist.GetValueOrDefault(row.ArtistId, 0);

            var listeningWeight = Math.Sqrt(Math.Max(0, row.Seconds));
            var listeningShare = weightedListeningSum > 0
                ? (decimal)(listeningWeight / weightedListeningSum)
                : 0m;

            var deprioritizationSignalShare = deprioritizedByEthicalCount > 0
                ? deprioritizedByEthicalCount / (deprioritizedByEthicalCount + 3m)
                : 0m;

            var popularity = maxGlobalLog > 0
                ? (decimal)(Math.Log(1 + globalListenedSeconds) / maxGlobalLog)
                : 0m;

            var inversePopularityScore = 0.2m + (0.8m * (1m - popularity));
            var listeningComponent = alpha * listeningShare * inversePopularityScore;
            var ignoredComponent = beta * deprioritizationSignalShare * (1m + inversePopularityScore);
            var rawScore = listeningComponent + ignoredComponent;

            var dto = new EthicalArtistAllocationDto
            {
                ArtistId = row.ArtistId,
                ArtistName = artistNamesById.GetValueOrDefault(row.ArtistId, "Unknown Artist"),
                ListenerListenedSeconds = row.Seconds,
                GlobalListenedSeconds = globalListenedSeconds,
                DeprioritizedByEthicalCount = deprioritizedByEthicalCount,
                InversePopularityScore = Math.Round(inversePopularityScore, 4),
                ListeningShare = Math.Round(listeningShare, 4),
                DeprioritizationSignalShare = Math.Round(deprioritizationSignalShare, 4),
                EthicalScore = Math.Round(rawScore, 6)
            };

            scored.Add((dto, rawScore, listeningShare));
        }

        var totalEthicalScore = scored.Sum(x => x.rawScore);
        return scored
            .Select(x =>
            {
                var fallbackShare = x.rawListeningShare;
                var effectiveShare = totalEthicalScore > 0
                    ? x.rawScore / totalEthicalScore
                    : fallbackShare;

                return new EthicalArtistAllocationDto
                {
                    ArtistId = x.dto.ArtistId,
                    ArtistName = x.dto.ArtistName,
                    ListenerListenedSeconds = x.dto.ListenerListenedSeconds,
                    GlobalListenedSeconds = x.dto.GlobalListenedSeconds,
                    DeprioritizedByEthicalCount = x.dto.DeprioritizedByEthicalCount,
                    InversePopularityScore = x.dto.InversePopularityScore,
                    ListeningShare = x.dto.ListeningShare,
                    DeprioritizationSignalShare = x.dto.DeprioritizationSignalShare,
                    EthicalScore = x.dto.EthicalScore,
                    SuggestedBudget = Math.Round(monthlyBudget * effectiveShare, 2)
                };
            })
            .OrderByDescending(x => x.EthicalScore)
            .ToList();
    }

    private static decimal NormalizeMonthlyBudget(decimal monthlyBudget)
    {
        return monthlyBudget > 0 ? monthlyBudget : 500m;
    }

    private sealed record ArtistListeningRow(Guid ArtistId, int Seconds);
}
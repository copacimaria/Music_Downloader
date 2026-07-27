using System.Collections.Concurrent;
using Musiq.Models;

namespace Musiq.Services;

/// <summary>
/// Implementare în memorie. Istoricul se pierde la restartul aplicației;
/// pentru persistență se înlocuiește doar această clasă.
/// </summary>
public class InMemoryDownloadJobStore : IDownloadJobStore
{
    private readonly ConcurrentDictionary<Guid, DownloadJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellations = new();

    public void Add(DownloadJob job) => _jobs[job.Id] = job;

    public DownloadJob? Get(Guid id) => _jobs.TryGetValue(id, out var job) ? job : null;

    public IReadOnlyList<DownloadJob> GetRecent(int take = 20) =>
        _jobs.Values
             .OrderByDescending(job => job.CreatedUtc)
             .Take(take)
             .ToList();

    public void RegisterCancellation(Guid id, CancellationTokenSource cancellationTokenSource) =>
        _cancellations[id] = cancellationTokenSource;

    public void ReleaseCancellation(Guid id) => _cancellations.TryRemove(id, out _);

    public bool TryCancel(Guid id)
    {
        if (!_cancellations.TryGetValue(id, out var cancellationTokenSource))
        {
            return false;
        }

        try
        {
            cancellationTokenSource.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}

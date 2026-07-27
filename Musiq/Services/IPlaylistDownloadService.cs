using Musiq.Models;

namespace Musiq.Services;

/// <summary>Serviciul de orchestrare apelat din controller.</summary>
public interface IPlaylistDownloadService
{
    string DefaultOutputFolder { get; }

    /// <summary>Validează playlistul, creează sarcina și o pune în coadă.</summary>
    Task<Guid> QueueAsync(StartDownloadViewModel model, CancellationToken cancellationToken);

    DownloadJob? GetJob(Guid id);

    IReadOnlyList<DownloadJob> GetRecentJobs();

    bool Cancel(Guid id);
}

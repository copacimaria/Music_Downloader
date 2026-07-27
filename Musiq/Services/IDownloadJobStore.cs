using Musiq.Models;

namespace Musiq.Services;

/// <summary>Depozitul sarcinilor de descărcare și al token-urilor lor de anulare.</summary>
public interface IDownloadJobStore
{
    void Add(DownloadJob job);

    DownloadJob? Get(Guid id);

    IReadOnlyList<DownloadJob> GetRecent(int take = 20);

    void RegisterCancellation(Guid id, CancellationTokenSource cancellationTokenSource);

    void ReleaseCancellation(Guid id);

    bool TryCancel(Guid id);
}

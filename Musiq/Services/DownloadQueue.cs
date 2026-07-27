using System.Threading.Channels;

namespace Musiq.Services;

/// <summary>Coada de sarcini consumată de <see cref="DownloadWorker"/>.</summary>
public interface IDownloadQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}

/// <summary>Coadă în memorie, cu un singur consumator (descărcările rulează secvențial).</summary>
public class DownloadQueue : IDownloadQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(jobId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

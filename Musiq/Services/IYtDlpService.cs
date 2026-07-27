using Musiq.Models;

namespace Musiq.Services;

/// <summary>Abstracție peste procesul extern yt-dlp.</summary>
public interface IYtDlpService
{
    /// <summary>Citește titlul și numărul de melodii ale playlistului, fără a descărca nimic.</summary>
    Task<PlaylistInfo> GetPlaylistInfoAsync(string playlistUrl, CancellationToken cancellationToken);

    /// <summary>Descarcă întregul playlist, raportând progresul pentru fiecare melodie.</summary>
    Task DownloadPlaylistAsync(
        DownloadJob job,
        IProgress<ProgressUpdate> progress,
        Action<string> onLog,
        CancellationToken cancellationToken);
}

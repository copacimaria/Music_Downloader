using Microsoft.Extensions.Options;
using Musiq.Models;

namespace Musiq.Services;

/// <summary>
/// Leagă citirea metadatelor, crearea sarcinii și punerea ei în coadă.
/// Controllerul nu cunoaște nici yt-dlp, nici coada.
/// </summary>
public class PlaylistDownloadService : IPlaylistDownloadService
{
    private readonly IYtDlpService _ytDlp;
    private readonly IDownloadJobStore _store;
    private readonly IDownloadQueue _queue;
    private readonly YtDlpOptions _options;

    public PlaylistDownloadService(
        IYtDlpService ytDlp,
        IDownloadJobStore store,
        IDownloadQueue queue,
        IOptions<YtDlpOptions> options)
    {
        _ytDlp = ytDlp;
        _store = store;
        _queue = queue;
        _options = options.Value;
    }

    public string DefaultOutputFolder => _options.DefaultOutputFolder;

    public async Task<Guid> QueueAsync(StartDownloadViewModel model, CancellationToken cancellationToken)
    {
        EnsureFolderIsUsable(model.OutputFolder);

        var info = await _ytDlp.GetPlaylistInfoAsync(model.PlaylistUrl, cancellationToken);

        if (info.ItemCount == 0)
        {
            throw new InvalidOperationException(
                "Playlistul nu conține melodii accesibile. Verifică dacă este public sau nelistat.");
        }

        var outputFolder = model.UsePlaylistSubfolder
            ? Path.Combine(model.OutputFolder, SanitizeFolderName(info.Title))
            : model.OutputFolder;

        var job = new DownloadJob
        {
            PlaylistUrl = model.PlaylistUrl,
            PlaylistTitle = info.Title,
            TotalItems = info.ItemCount,
            OutputFolder = outputFolder,
            Format = model.Format,
            PrefixWithIndex = model.PrefixWithIndex
        };

        _store.Add(job);
        await _queue.EnqueueAsync(job.Id, cancellationToken);

        return job.Id;
    }

    public DownloadJob? GetJob(Guid id) => _store.Get(id);

    public IReadOnlyList<DownloadJob> GetRecentJobs() => _store.GetRecent();

    public bool Cancel(Guid id) => _store.TryCancel(id);

    private static void EnsureFolderIsUsable(string folder)
    {
        if (!Path.IsPathFullyQualified(folder))
        {
            throw new InvalidOperationException("Folderul destinație trebuie să fie o cale absolută (ex. D:\\Muzică).");
        }

        var root = Path.GetPathRoot(folder);

        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            throw new InvalidOperationException($"Unitatea „{root}” nu există.");
        }
    }

    /// <summary>Înlocuiește caracterele nepermise în numele de folder, păstrând diacriticele.</summary>
    private static string SanitizeFolderName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray()).Trim();

        return cleaned.Length == 0 ? "Playlist" : cleaned;
    }
}

using Musiq.Models;

namespace Musiq.Services;

/// <summary>
/// Serviciu de fundal care consumă coada și execută descărcările una câte una.
/// </summary>
public class DownloadWorker : BackgroundService
{
    private readonly IDownloadQueue _queue;
    private readonly IDownloadJobStore _store;
    private readonly IYtDlpService _ytDlp;
    private readonly ILogger<DownloadWorker> _logger;

    public DownloadWorker(
        IDownloadQueue queue,
        IDownloadJobStore store,
        IYtDlpService ytDlp,
        ILogger<DownloadWorker> logger)
    {
        _queue = queue;
        _store = store;
        _ytDlp = ytDlp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _queue.DequeueAllAsync(stoppingToken))
        {
            var job = _store.Get(jobId);

            if (job is null)
            {
                _logger.LogWarning("Sarcina {JobId} nu a fost găsită în depozit.", jobId);
                continue;
            }

            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(DownloadJob job, CancellationToken stoppingToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _store.RegisterCancellation(job.Id, cancellation);

        try
        {
            job.Status = JobStatus.Running;
            job.AppendLog($"Pornit: {job.PlaylistTitle} ({job.TotalItems} melodii) → {job.OutputFolder}");

            var progress = new Progress<ProgressUpdate>(update => ApplyProgress(job, update));

            await _ytDlp.DownloadPlaylistAsync(job, progress, job.AppendLog, cancellation.Token);

            job.CompletedItems = job.TotalItems;
            job.CurrentItemPercent = 0;
            job.CurrentItemTitle = string.Empty;
            job.Status = JobStatus.Completed;
            job.AppendLog("Descărcare finalizată.");
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.AppendLog("Descărcare anulată de utilizator.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Descărcarea playlistului {JobId} a eșuat.", job.Id);
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.AppendLog($"Eroare: {ex.Message}");
        }
        finally
        {
            job.FinishedUtc = DateTime.UtcNow;
            _store.ReleaseCancellation(job.Id);
        }
    }

    /// <summary>
    /// Când yt-dlp trece la melodia următoare, cea anterioară se consideră încheiată.
    /// </summary>
    private static void ApplyProgress(DownloadJob job, ProgressUpdate update)
    {
        if (update.Index > job.CurrentIndex && job.CurrentIndex > 0)
        {
            job.CompletedItems = job.CurrentIndex;
        }

        job.CurrentIndex = update.Index;
        job.CurrentItemTitle = update.Title;
        job.CurrentItemPercent = update.Percent;
    }
}

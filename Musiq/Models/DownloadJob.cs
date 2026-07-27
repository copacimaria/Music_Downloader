namespace Musiq.Models;

/// <summary>
/// Reprezintă o sarcină de descărcare a unui playlist, împreună cu progresul curent.
/// Instanța este mutabilă și partajată între worker (scrie) și controller (citește).
/// </summary>
public class DownloadJob
{
    private const int MaxLogLines = 300;
    private readonly List<string> _log = new();
    private readonly object _sync = new();

    public Guid Id { get; init; } = Guid.NewGuid();

    public string PlaylistUrl { get; init; } = string.Empty;

    public string PlaylistTitle { get; set; } = string.Empty;

    public string OutputFolder { get; init; } = string.Empty;

    public AudioFormat Format { get; init; } = AudioFormat.Mp3;

    /// <summary>Dacă numele fișierelor începe cu numărul de ordine din playlist.</summary>
    public bool PrefixWithIndex { get; init; } = true;

    /// <summary>Șablonul de nume de fișier trimis lui yt-dlp prin --output.</summary>
    public string OutputTemplate => PrefixWithIndex
        ? "%(playlist_index)02d - %(title)s.%(ext)s"
        : "%(title)s.%(ext)s";

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public int TotalItems { get; set; }

    public int CompletedItems { get; set; }

    public int CurrentIndex { get; set; }

    public string CurrentItemTitle { get; set; } = string.Empty;

    public double CurrentItemPercent { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public DateTime? FinishedUtc { get; set; }

    /// <summary>Progresul total, în procente, incluzând fracțiunea melodiei curente.</summary>
    public int OverallPercent
    {
        get
        {
            if (TotalItems <= 0)
            {
                return 0;
            }

            var done = CompletedItems + (CurrentItemPercent / 100d);
            return (int)Math.Round(Math.Clamp(done, 0, TotalItems) / TotalItems * 100);
        }
    }

    public bool IsFinished =>
        Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled;

    public TimeSpan Duration => (FinishedUtc ?? DateTime.UtcNow) - CreatedUtc;

    public void AppendLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (_sync)
        {
            _log.Add(line);

            if (_log.Count > MaxLogLines)
            {
                _log.RemoveRange(0, _log.Count - MaxLogLines);
            }
        }
    }

    public IReadOnlyList<string> GetLog(int lastLines = 60)
    {
        lock (_sync)
        {
            return _log.Count <= lastLines
                ? _log.ToArray()
                : _log.Skip(_log.Count - lastLines).ToArray();
        }
    }
}

/// <summary>Metadatele minime ale unui playlist, citite înainte de descărcare.</summary>
public record PlaylistInfo(string Title, int ItemCount);

/// <summary>O actualizare de progres raportată de yt-dlp pentru melodia curentă.</summary>
public record ProgressUpdate(int Index, double Percent, string Title);

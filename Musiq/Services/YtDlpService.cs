using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Musiq.Models;

namespace Musiq.Services;

/// <summary>
/// Rulează yt-dlp ca proces extern și traduce ieșirea acestuia în actualizări de progres.
/// </summary>
public class YtDlpService : IYtDlpService
{
    /// <summary>Prefix folosit pentru a distinge liniile de progres de restul jurnalului.</summary>
    private const string ProgressMarker = "@@P@@";

    private readonly YtDlpOptions _options;
    private readonly IToolLocator _toolLocator;
    private readonly ILogger<YtDlpService> _logger;

    public YtDlpService(
        IOptions<YtDlpOptions> options,
        IToolLocator toolLocator,
        ILogger<YtDlpService> logger)
    {
        _options = options.Value;
        _toolLocator = toolLocator;
        _logger = logger;
    }

    public async Task<PlaylistInfo> GetPlaylistInfoAsync(string playlistUrl, CancellationToken cancellationToken)
    {
        var args = new List<string>
        {
            "--flat-playlist",
            "--dump-single-json",
            "--no-warnings",
            playlistUrl
        };

        var json = new StringBuilder();
        var errors = new StringBuilder();

        var exitCode = await RunAsync(
            args,
            line => json.AppendLine(line),
            line => errors.AppendLine(line),
            cancellationToken);

        if (exitCode != 0 || json.Length == 0)
        {
            var detail = errors.Length > 0 ? errors.ToString().Trim() : $"cod de ieșire {exitCode}";
            throw new InvalidOperationException($"Nu s-au putut citi datele playlistului ({detail}).");
        }

        return ParsePlaylistInfo(json.ToString());
    }

    public async Task DownloadPlaylistAsync(
        DownloadJob job,
        IProgress<ProgressUpdate> progress,
        Action<string> onLog,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(job.OutputFolder);

        var args = BuildDownloadArguments(job);

        var exitCode = await RunAsync(
            args,
            line => HandleOutputLine(line, progress, onLog),
            onLog,
            cancellationToken);

        // Codul 1 apare și când --ignore-errors a sărit peste melodii indisponibile,
        // deci nu îl tratăm ca eșec total al playlistului.
        if (exitCode > 1)
        {
            throw new InvalidOperationException($"yt-dlp s-a încheiat cu codul {exitCode}.");
        }
    }

    private List<string> BuildDownloadArguments(DownloadJob job)
    {
        var template = job.OutputTemplate;

        var args = new List<string>
        {
            "--newline",
            "--ignore-errors",
            "--no-warnings",
            "--no-overwrites",
            "--extract-audio",
            "--audio-format", job.Format.ToString().ToLowerInvariant(),
            "--audio-quality", "0",
            "--embed-metadata",
            "--embed-thumbnail",
            "--sleep-requests", _options.SleepRequestsSeconds.ToString(CultureInfo.InvariantCulture),
            "--paths", job.OutputFolder,
            "--output", template,
            "--progress-template",
            $"download:{ProgressMarker}|%(info.playlist_index)s|%(progress._percent_str)s|%(info.title)s"
        };

        var ffmpegDirectory = _toolLocator.ResolveFfmpegDirectory();

        if (ffmpegDirectory is not null)
        {
            args.Add("--ffmpeg-location");
            args.Add(ffmpegDirectory);
        }

        args.Add(job.PlaylistUrl);
        return args;
    }

    private static PlaylistInfo ParsePlaylistInfo(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var title = root.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString() ?? "Playlist"
            : "Playlist";

        var count = 0;

        if (root.TryGetProperty("playlist_count", out var countElement) &&
            countElement.TryGetInt32(out var parsedCount))
        {
            count = parsedCount;
        }
        else if (root.TryGetProperty("entries", out var entries) &&
                 entries.ValueKind == JsonValueKind.Array)
        {
            count = entries.GetArrayLength();
        }

        return new PlaylistInfo(title, count);
    }

    private static void HandleOutputLine(string line, IProgress<ProgressUpdate> progress, Action<string> onLog)
    {
        if (!line.StartsWith(ProgressMarker, StringComparison.Ordinal))
        {
            onLog(line);
            return;
        }

        var parts = line.Split('|');

        if (parts.Length < 4)
        {
            return;
        }

        _ = int.TryParse(parts[1].Trim(), out var index);

        var percentText = parts[2].Trim().TrimEnd('%');
        _ = double.TryParse(
            percentText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var percent);

        progress.Report(new ProgressUpdate(index, percent, parts[3].Trim()));
    }

    private async Task<int> RunAsync(
        IEnumerable<string> args,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken)
    {
        // Aruncă ToolNotFoundException cu un mesaj explicit dacă yt-dlp lipsește.
        var executable = _toolLocator.ResolveYtDlpPath();

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable) ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onError(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Nu s-a putut porni procesul „{executable}”.");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Pornirea procesului „{executable}” a eșuat: {ex.Message}", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using var registration = cancellationToken.Register(() => KillProcess(process));

        // Aşteptăm cu CancellationToken.None: oprirea reală o face KillProcess.
        await process.WaitForExitAsync(CancellationToken.None);

        cancellationToken.ThrowIfCancellationRequested();

        return process.ExitCode;
    }

    private void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Oprirea procesului yt-dlp a eșuat.");
        }
    }
}

using Microsoft.Extensions.Options;
using Musiq.Models;

namespace Musiq.Services;

/// <summary>Aruncată când o unealtă externă nu poate fi găsită pe disc.</summary>
public class ToolNotFoundException : Exception
{
    public ToolNotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Rezolvă căile către uneltele externe. Căile relative din configurație se
/// interpretează față de rădăcina aplicației, nu față de directorul de lucru
/// al procesului — altfel aplicația publicată nu le-ar mai găsi.
/// </summary>
public interface IToolLocator
{
    /// <summary>Calea absolută către yt-dlp. Aruncă dacă nu există.</summary>
    string ResolveYtDlpPath();

    /// <summary>Folderul absolut cu ffmpeg, sau <c>null</c> dacă nu e configurat/găsit.</summary>
    string? ResolveFfmpegDirectory();

    /// <summary>Verificare fără excepții, pentru avertismentul din interfață.</summary>
    ToolAvailability CheckAvailability();
}

public class ToolLocator : IToolLocator
{
    private static readonly string[] FfmpegExecutables = ["ffmpeg.exe", "ffmpeg"];

    private readonly YtDlpOptions _options;
    private readonly string _contentRoot;
    private readonly ILogger<ToolLocator> _logger;

    public ToolLocator(
        IOptions<YtDlpOptions> options,
        IHostEnvironment environment,
        ILogger<ToolLocator> logger)
    {
        _options = options.Value;
        _contentRoot = environment.ContentRootPath;
        _logger = logger;
    }

    public string ResolveYtDlpPath()
    {
        var configured = _options.YtDlpPath;

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new ToolNotFoundException("Setarea YtDlp:YtDlpPath este necompletată.");
        }

        var resolved = TryResolveFile(configured);

        if (resolved is not null)
        {
            return resolved;
        }

        throw new ToolNotFoundException(BuildYtDlpNotFoundMessage(configured));
    }

    public string? ResolveFfmpegDirectory()
    {
        var configured = _options.FfmpegPath;

        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        var absolute = ToAbsolute(configured);

        // Configurația poate indica fie folderul, fie direct executabilul.
        if (File.Exists(absolute))
        {
            return Path.GetDirectoryName(absolute);
        }

        if (Directory.Exists(absolute) && FfmpegExecutables.Any(exe => File.Exists(Path.Combine(absolute, exe))))
        {
            return absolute;
        }

        _logger.LogWarning(
            "ffmpeg nu a fost găsit la „{Path}”. Extragerea audio va eșua.", absolute);

        return null;
    }

    public ToolAvailability CheckAvailability()
    {
        var ytDlpFound = TryResolveFile(_options.YtDlpPath) is not null;
        var ffmpegFound = ResolveFfmpegDirectory() is not null;

        if (ytDlpFound && ffmpegFound)
        {
            return ToolAvailability.Ok();
        }

        var missing = new List<string>();

        if (!ytDlpFound)
        {
            missing.Add("yt-dlp.exe");
        }

        if (!ffmpegFound)
        {
            missing.Add("ffmpeg.exe");
        }

        var folder = ToAbsolute(Path.GetDirectoryName(_options.YtDlpPath) is { Length: > 0 } dir
            ? dir
            : ".");

        var message =
            $"Lipsesc uneltele externe: {string.Join(", ", missing)}. " +
            $"Copiază-le în „{folder}” (detalii în CITESTE-MA.md din acel folder).";

        return new ToolAvailability(ytDlpFound, ffmpegFound, message);
    }

    /// <summary>
    /// Încearcă, în ordine: calea absolută, calea relativă la rădăcina aplicației,
    /// calea relativă la directorul de lucru, apoi folderele din PATH.
    /// </summary>
    private string? TryResolveFile(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        if (Path.IsPathRooted(configured))
        {
            return File.Exists(configured) ? configured : null;
        }

        var fromContentRoot = Path.GetFullPath(configured, _contentRoot);

        if (File.Exists(fromContentRoot))
        {
            return fromContentRoot;
        }

        var fromWorkingDirectory = Path.GetFullPath(configured);

        if (File.Exists(fromWorkingDirectory))
        {
            return fromWorkingDirectory;
        }

        // Doar un nume de fișier simplu poate veni din PATH.
        return Path.GetFileName(configured) == configured
            ? SearchInPathVariable(configured)
            : null;
    }

    private static string? SearchInPathVariable(string fileName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        foreach (var folder in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(folder.Trim(), fileName);

                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // Intrare invalidă în PATH — o ignorăm.
            }
        }

        return null;
    }

    private string ToAbsolute(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(path, _contentRoot);

    private string BuildYtDlpNotFoundMessage(string configured)
    {
        var absolute = ToAbsolute(configured);

        return $"yt-dlp nu a fost găsit. Calea verificată: „{absolute}”. " +
               "Descarcă yt-dlp.exe de la https://github.com/yt-dlp/yt-dlp/releases/latest " +
               "și copiază-l acolo, sau modifică setarea YtDlp:YtDlpPath din appsettings.json.";
    }
}

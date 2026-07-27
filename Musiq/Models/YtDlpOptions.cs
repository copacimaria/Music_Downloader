namespace Musiq.Models;

/// <summary>Configurarea uneltelor externe, din secțiunea "YtDlp" din appsettings.json.</summary>
public class YtDlpOptions
{
    public const string SectionName = "YtDlp";

    /// <summary>Calea către yt-dlp.exe (relativă la folderul aplicației sau absolută).</summary>
    public string YtDlpPath { get; set; } = "yt-dlp.exe";

    /// <summary>Folderul care conține ffmpeg.exe. Necesar pentru extragerea audio.</summary>
    public string? FfmpegPath { get; set; }

    /// <summary>Folderul propus implicit în formular.</summary>
    public string DefaultOutputFolder { get; set; } = string.Empty;

    /// <summary>Pauza, în secunde, între cereri către YouTube (reduce riscul de throttling).</summary>
    public int SleepRequestsSeconds { get; set; } = 1;
}

namespace Musiq.Models;

/// <summary>
/// Rezultatul verificării uneltelor externe, afișat ca avertisment în formular
/// înainte ca utilizatorul să încerce o descărcare.
/// </summary>
public record ToolAvailability(bool YtDlpFound, bool FfmpegFound, string? Message)
{
    public bool AllFound => YtDlpFound && FfmpegFound;

    public static ToolAvailability Ok() => new(true, true, null);
}

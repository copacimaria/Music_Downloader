using System.ComponentModel.DataAnnotations;

namespace Musiq.Models;

/// <summary>Datele introduse de utilizator pentru a porni o descărcare.</summary>
public class StartDownloadViewModel
{
    [Required(ErrorMessage = "Introdu URL-ul playlistului.")]
    [Url(ErrorMessage = "URL invalid.")]
    [Display(Name = "URL playlist YouTube")]
    public string PlaylistUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alege folderul de destinație.")]
    [Display(Name = "Folder destinație")]
    public string OutputFolder { get; set; } = string.Empty;

    [Display(Name = "Format audio")]
    public AudioFormat Format { get; set; } = AudioFormat.Mp3;

    [Display(Name = "Salvează într-un subfolder cu numele playlistului")]
    public bool UsePlaylistSubfolder { get; set; } = true;

    [Display(Name = "Include numărul de ordine în numele fișierului")]
    public bool PrefixWithIndex { get; set; } = true;
}

/// <summary>Modelul paginii de start, cu formularul și istoricul descărcărilor.</summary>
public class DownloadIndexViewModel
{
    public StartDownloadViewModel Form { get; set; } = new();

    public IReadOnlyList<DownloadJob> RecentJobs { get; set; } = Array.Empty<DownloadJob>();
}

namespace Musiq.Models;

/// <summary>Traduce stările în texte afișabile, ca să nu ținem șiruri în view-uri.</summary>
public static class JobStatusExtensions
{
    public static string ToRomanian(this JobStatus status) => status switch
    {
        JobStatus.Pending => "În așteptare",
        JobStatus.Running => "În lucru",
        JobStatus.Completed => "Finalizat",
        JobStatus.Failed => "Eșuat",
        JobStatus.Cancelled => "Anulat",
        _ => status.ToString()
    };
}

namespace Musiq.Models;

/// <summary>Formatul audio în care se convertesc melodiile descărcate.</summary>
public enum AudioFormat
{
    Mp3 = 0,
    M4a = 1,
    Opus = 2,
    Flac = 3
}

/// <summary>Starea unei sarcini de descărcare.</summary>
public enum JobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

namespace OpenTrainDrive.Models;

/// <summary>
/// Modell fuer die Fehleransicht mit Request-Informationen.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Request-ID fuer die Fehleranzeige.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// True, wenn eine Request-ID vorhanden ist.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

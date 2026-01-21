namespace OpenTrainDrive.Models;

/// <summary>
/// DTO fuer die Anwendungseinstellungen.
/// </summary>
public record SettingsDto(
    string? ProjectName,
    string? Language,
    bool AutoSave,
    int AutoSaveInterval,
    bool AutoOpen,
    bool UsersEnabled,
    string? Theme,
    string? Density,
    bool ShowTooltips,
    bool ShowClock,
    bool ShowStatusbar,
    string? System,
    string? Host,
    int Port,
    int Baud,
    bool AutoConnect,
    int Heartbeat,
    double MaxSpeed,
    double AccelFactor,
    double BrakeFactor,
    bool StopOnSignal,
    bool EmergencyStop,
    int GridSize,
    bool Snap,
    bool ShowLabels,
    bool ShowIds,
    bool ConfirmDelete,
    string? LogLevel,
    int KeepDays,
    string? LogPath
);

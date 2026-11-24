namespace BandFounder.Api.Options;

public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>
    /// Enables backup export and restore endpoints. Enable this only on an instance
    /// whose network access is restricted to trusted administrators.
    /// </summary>
    public bool TrustedMaintenanceEnabled { get; set; }
}

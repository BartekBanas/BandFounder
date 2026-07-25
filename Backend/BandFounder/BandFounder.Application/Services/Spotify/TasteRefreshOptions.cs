namespace BandFounder.Application.Services.Spotify;

public class TasteRefreshOptions
{
    public const string SectionName = "Spotify:TasteRefresh";

    public bool Enabled { get; set; } = true;

    /// <summary>Minimum days since last sync before a user can be due.</summary>
    public int MinDays { get; set; } = 7;

    /// <summary>Maximum days since last sync (inclusive); jitter spreads users across MinDays..MaxDays.</summary>
    public int MaxDays { get; set; } = 14;

    public int BatchSize { get; set; } = 10;

    public int TickHours { get; set; } = 1;
}

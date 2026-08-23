namespace BandFounder.Application.Services.Email;

public sealed class MessageEmailNotificationOptions
{
    public const string SectionName = "MessageEmailNotifications";
    public const int DefaultDelayMinutes = 1440;

    public static readonly IReadOnlySet<int> AllowedDelayMinutes = new HashSet<int>
    {
        5,
        60,
        DefaultDelayMinutes
    };

    public int PollIntervalSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 3;
    public int MaxSnippetLength { get; set; } = 160;
    public int StaleClaimMinutes { get; set; } = 15;
}

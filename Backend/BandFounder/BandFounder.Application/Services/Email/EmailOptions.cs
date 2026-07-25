namespace BandFounder.Application.Services.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "noreply@bandfounder.com";
    public string FrontendBaseUrl { get; set; } = "http://127.0.0.1:3000";
    public int PasswordResetTokenTtlMinutes { get; set; } = 15;
}

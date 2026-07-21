namespace BandFounder.Application.Services.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "noreply@bandfounder.com";
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    public int PasswordResetTokenTtlMinutes { get; set; } = 15;
}

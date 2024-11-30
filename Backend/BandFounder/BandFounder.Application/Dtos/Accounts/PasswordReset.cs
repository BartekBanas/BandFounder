namespace BandFounder.Application.Dtos.Accounts;

public class RequestPasswordResetDto
{
    public required string ResetPasswordPageUrl { get; set; }
}

public class PasswordResetDto
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}
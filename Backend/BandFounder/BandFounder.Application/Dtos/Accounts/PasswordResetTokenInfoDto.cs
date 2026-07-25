namespace BandFounder.Application.Dtos.Accounts;

public class PasswordResetTokenInfoDto
{
    public required Guid AccountId { get; set; }

    public required string Username { get; set; }

    public required string Email { get; set; }

    public required bool HasProfilePicture { get; set; }

    public required DateTime MemberSince { get; set; }

    public required DateTime ExpiresAt { get; set; }
}

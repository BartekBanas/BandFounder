namespace BandFounder.Application.Dtos.Accounts;

public class RegisterAccountDto
{
    public required string Name { get; init; }

    public required string Email { get; init; }

    public required string Password { get; init; }
}
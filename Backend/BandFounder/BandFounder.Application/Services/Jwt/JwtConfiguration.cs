namespace BandFounder.Application.Services.Jwt;

public class JwtConfiguration
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string PublicKey { get; init; }
    public required string PublicKeyInfo { get; init; }
    public required string SecretKey { get; init; }
    public int Expires { get; init; }
}
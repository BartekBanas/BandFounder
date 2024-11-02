namespace BandFounder.Infrastructure.Errors;

public class SpotifyAccountAlreadyConnectedException : InfrastructureErrorException
{
    private const string DefaultMessage = "This account is already connected to a Spotify account.";

    public SpotifyAccountAlreadyConnectedException() : base(DefaultMessage)
    {
    }

    public SpotifyAccountAlreadyConnectedException(string? message) : base(message ?? DefaultMessage)
    {
    }

    public SpotifyAccountAlreadyConnectedException(string? message, Exception? innerException) : 
        base(message ?? DefaultMessage, innerException)
    {
    }
}
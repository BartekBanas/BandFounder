using System.Security.Cryptography;
using System.Text;

namespace BandFounder.Application.Services;

public static class SecureTokenHelper
{
    public static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    public static string DeriveRawToken(Guid tokenId, string signingKey)
    {
        var tokenIdBytes = tokenId.ToByteArray();
        var purpose = Encoding.UTF8.GetBytes("BandFounder.EmailVerification.v1");
        var input = new byte[purpose.Length + tokenIdBytes.Length];
        Buffer.BlockCopy(purpose, 0, input, 0, purpose.Length);
        Buffer.BlockCopy(tokenIdBytes, 0, input, purpose.Length, tokenIdBytes.Length);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var authenticator = hmac.ComputeHash(input);
        var tokenBytes = new byte[tokenIdBytes.Length + authenticator.Length];
        Buffer.BlockCopy(tokenIdBytes, 0, tokenBytes, 0, tokenIdBytes.Length);
        Buffer.BlockCopy(authenticator, 0, tokenBytes, tokenIdBytes.Length, authenticator.Length);

        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

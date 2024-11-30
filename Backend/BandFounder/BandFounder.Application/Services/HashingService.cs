using BandFounder.Domain.Entities;

namespace BandFounder.Application.Services;

public interface IHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(Account account, string password);
    bool VerifyPhrase(string phrase, string hashedPhrase);
}

public class HashingService : IHashingService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(Account account, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
    }

    public bool VerifyPhrase(string phrase, string hashedPhrase)
    {
        return BCrypt.Net.BCrypt.Verify(phrase, hashedPhrase);
    }
}
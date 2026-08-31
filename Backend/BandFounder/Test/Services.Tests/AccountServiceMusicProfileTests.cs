using BandFounder.Application.Services;
using BandFounder.Application.Services.Email;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class AccountServiceMusicProfileTests
{
    [Test]
    public async Task AddArtist_ShouldInvalidateTheAccountMusicProfile()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Name = "test",
            Email = "test@example.com",
            PasswordHash = "hash",
            DateCreated = DateTime.UtcNow
        };
        var accountRepository = Substitute.For<IRepository<Account>>();
        var artistRepository = Substitute.For<IRepository<Artist>>();
        var authenticationService = Substitute.For<IAuthenticationService>();
        var musicProfileProvider = Substitute.For<IMusicProfileProvider>();
        accountRepository.GetOneRequiredAsync(
            Arg.Any<object>(),
            Arg.Any<string>(),
            Arg.Any<string[]>()).Returns(account);
        artistRepository.GetOneAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<Artist, bool>>>(),
                Arg.Any<string[]>())
            .Returns((Artist?)null);
        authenticationService.GetUserId().Returns(accountId);
        var service = new AccountService(
            accountRepository,
            artistRepository,
            Substitute.For<IRepository<MusicianRole>>(),
            Substitute.For<IRepository<SpotifyTokens>>(),
            Substitute.For<IRepository<PasswordResetToken>>(),
            Substitute.For<IPasswordResetTokenStore>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IChatroomService>(),
            Substitute.For<IValidator<Account>>(),
            authenticationService,
            Substitute.For<IHashingService>(),
            Substitute.For<IJwtService>(),
            Substitute.For<IEmailSender>(),
            Options.Create(new EmailOptions()),
            Substitute.For<ILogger<AccountService>>(),
            musicProfileProvider);

        await service.AddArtist(accountId, "Artist 1");

        musicProfileProvider.Received(1).Invalidate(accountId);
    }
}

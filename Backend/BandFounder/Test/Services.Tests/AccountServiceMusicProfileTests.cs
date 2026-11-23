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
        var account = CreateAccount(accountId);
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
        var service = CreateService(
            accountRepository,
            artistRepository,
            authenticationService,
            Substitute.For<IChatroomService>(),
            musicProfileProvider);

        await service.AddArtist(accountId, "Artist 1");

        musicProfileProvider.Received(1).Invalidate(accountId);
    }

    [Test]
    public async Task DeleteAccountAsync_ShouldLeaveChatroomsDeleteAndInvalidate()
    {
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId);
        var accountRepository = Substitute.For<IRepository<Account>>();
        var chatroomService = Substitute.For<IChatroomService>();
        var musicProfileProvider = Substitute.For<IMusicProfileProvider>();
        accountRepository.GetOneRequiredAsync(
            Arg.Any<object>(),
            Arg.Any<string>(),
            Arg.Any<string[]>()).Returns(account);
        var service = CreateService(
            accountRepository,
            Substitute.For<IRepository<Artist>>(),
            Substitute.For<IAuthenticationService>(),
            chatroomService,
            musicProfileProvider);

        await service.DeleteAccountAsync(accountId);

        await chatroomService.Received(1).LeaveAllChatrooms(account);
        await accountRepository.Received(1).DeleteOneAsync(accountId);
        await accountRepository.Received(1).SaveChangesAsync();
        musicProfileProvider.Received(1).Invalidate(accountId);
    }

    private static Account CreateAccount(Guid accountId)
    {
        return new Account
        {
            Id = accountId,
            Name = "test",
            Email = "test@example.com",
            PasswordHash = "hash",
            DateCreated = DateTime.UtcNow
        };
    }

    private static AccountService CreateService(
        IRepository<Account> accountRepository,
        IRepository<Artist> artistRepository,
        IAuthenticationService authenticationService,
        IChatroomService chatroomService,
        IMusicProfileProvider musicProfileProvider)
    {
        return new AccountService(
            accountRepository,
            artistRepository,
            Substitute.For<IRepository<MusicianRole>>(),
            Substitute.For<IRepository<SpotifyTokens>>(),
            Substitute.For<IRepository<PasswordResetToken>>(),
            Substitute.For<IPasswordResetTokenStore>(),
            Substitute.For<IUnitOfWork>(),
            chatroomService,
            Substitute.For<IEmailVerificationService>(),
            Substitute.For<IValidator<Account>>(),
            authenticationService,
            Substitute.For<IHashingService>(),
            Substitute.For<IJwtService>(),
            Substitute.For<IEmailSender>(),
            Options.Create(new EmailOptions()),
            Substitute.For<ILogger<AccountService>>(),
            musicProfileProvider);
    }
}

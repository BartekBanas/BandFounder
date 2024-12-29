using System.Linq.Expressions;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Error;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class ChatroomServiceTests
{
    private ChatroomService _chatroomService;
    private IRepository<Chatroom> _chatRoomRepositoryMock;
    private IRepository<Account> _accountRepositoryMock;
    private IAuthenticationService _authenticationServiceMock;
    private IAuthorizationService _authorizationServiceMock;

    [SetUp]
    public void Setup()
    {
        _chatRoomRepositoryMock = Substitute.For<IRepository<Chatroom>>();
        _accountRepositoryMock = Substitute.For<IRepository<Account>>();
        _authenticationServiceMock = Substitute.For<IAuthenticationService>();
        _authorizationServiceMock = Substitute.For<IAuthorizationService>();

        _chatroomService = new ChatroomService(
            _chatRoomRepositoryMock,
            _accountRepositoryMock,
            _authenticationServiceMock,
            _authorizationServiceMock
        );
    }

    [Test]
    public async Task CreateChatroom_ShouldCreateGeneralChatroom_WhenValidRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatroomDto = new ChatroomCreateDto
        {
            ChatRoomType = ChatRoomType.General,
            Name = "General Chatroom"
        };

        var account = new Account
        {
            Id = userId,
            Name = null,
            PasswordHash = null,
            Email = null
        };

        // Act
        var result = await _chatroomService.CreateChatroom(account, chatroomDto);

        // Assert
        Assert.That(result.Name, Is.EqualTo(chatroomDto.Name));
        await _chatRoomRepositoryMock.Received(1).CreateAsync(Arg.Is<Chatroom>(c =>
            c.Name == chatroomDto.Name && c.ChatRoomType == ChatRoomType.General && c.Owner.Id == userId));
        await _chatRoomRepositoryMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public void CreateChatroom_ShouldThrowBadRequestError_WhenNameIsEmptyForGeneralChatroom()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatroomDto = new ChatroomCreateDto
        {
            ChatRoomType = ChatRoomType.General,
            Name = string.Empty
        };
        
        var account = new Account
        {
            Id = userId,
            Name = null,
            PasswordHash = null,
            Email = null
        };

        // Act & Assert
        Assert.ThrowsAsync<BadRequestError>(async () => await _chatroomService.CreateChatroom(account, chatroomDto));
    }
    
    [Test]
    public async Task GetUserChatrooms_ShouldReturnUserChatrooms()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = userId,
            Chatrooms = new List<Chatroom>
            {
                new Chatroom { Id = Guid.NewGuid(), Name = "Chatroom 1" },
                new Chatroom { Id = Guid.NewGuid(), Name = "Chatroom 2" }
            },
            Name = null,
            PasswordHash = null,
            Email = null
        };

        _chatRoomRepositoryMock.GetAsync(Arg.Any<Expression<Func<Chatroom, bool>>>(),
                Arg.Any<Func<IQueryable<Chatroom>, IOrderedQueryable<Chatroom>>>(), nameof(Chatroom.Members))
            .Returns(Task.FromResult<IEnumerable<Chatroom>>(account.Chatrooms));

        // Act
        var result = await _chatroomService.GetUsersChatrooms(account);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Any(c => c.Name == "Chatroom 1"), Is.True);
        Assert.That(result.Any(c => c.Name == "Chatroom 2"), Is.True);
    }

    [Test]
    public void InviteToChatroom_ShouldThrowBadRequestError_WhenInvitingSelf()
    {
        // Arrange
        var chatroomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _authenticationServiceMock.GetUserId().Returns(userId);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestError>(async () =>
            await _chatroomService.InviteToChatroom(chatroomId, userId));
    }
}
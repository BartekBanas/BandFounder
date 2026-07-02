using System.Linq.Expressions;
using System.Security.Claims;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Exceptions;
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
        Assert.ThrowsAsync<BadRequestException>(async () => await _chatroomService.CreateChatroom(account, chatroomDto));
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
        Assert.ThrowsAsync<BadRequestException>(async () =>
            await _chatroomService.InviteToChatroom(chatroomId, userId));
    }

    [Test]
    public async Task InviteToChatroom_ShouldAddMember_WhenValidRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var chatroom = CreateGroupChatroom(userId);
        var invitedAccount = CreateAccount(invitedUserId);

        MockAuthorizationSuccess(userId);
        _chatRoomRepositoryMock.GetOneRequiredAsync(
                Arg.Any<Expression<Func<Chatroom, bool>>>(), nameof(Chatroom.Members))
            .Returns(chatroom);
        _accountRepositoryMock.GetOneRequiredAsync(invitedUserId).Returns(invitedAccount);

        // Act
        await _chatroomService.InviteToChatroom(chatroom.Id, invitedUserId);

        // Assert
        Assert.That(chatroom.Members.Any(member => member.Id == invitedUserId), Is.True);
        await _chatRoomRepositoryMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public void InviteToChatroom_ShouldThrowBadRequestError_WhenChatroomIsDirect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var chatroom = CreateGroupChatroom(userId);
        chatroom.ChatRoomType = ChatRoomType.Direct;

        MockAuthorizationSuccess(userId);
        _chatRoomRepositoryMock.GetOneRequiredAsync(
                Arg.Any<Expression<Func<Chatroom, bool>>>(), nameof(Chatroom.Members))
            .Returns(chatroom);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(async () =>
            await _chatroomService.InviteToChatroom(chatroom.Id, invitedUserId));
    }

    [Test]
    public void InviteToChatroom_ShouldThrowBadRequestError_WhenUserIsAlreadyMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var chatroom = CreateGroupChatroom(userId);
        chatroom.Members.Add(CreateAccount(invitedUserId));

        MockAuthorizationSuccess(userId);
        _chatRoomRepositoryMock.GetOneRequiredAsync(
                Arg.Any<Expression<Func<Chatroom, bool>>>(), nameof(Chatroom.Members))
            .Returns(chatroom);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(async () =>
            await _chatroomService.InviteToChatroom(chatroom.Id, invitedUserId));
    }

    [Test]
    public async Task LeaveChatroom_ShouldDeleteChatroom_WhenLastMemberLeaves()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatroom = CreateGroupChatroom(userId);

        MockAuthorizationSuccess(userId);
        _chatRoomRepositoryMock.GetOneRequiredAsync(
                Arg.Any<Expression<Func<Chatroom, bool>>>(), nameof(Chatroom.Members))
            .Returns(chatroom);

        // Act
        await _chatroomService.LeaveChatroom(chatroom.Id);

        // Assert
        await _chatRoomRepositoryMock.Received(1).DeleteOneAsync(chatroom.Id);
        await _chatRoomRepositoryMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task LeaveChatroom_ShouldTransferOwnership_WhenOwnerLeaves()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherMemberId = Guid.NewGuid();
        var chatroom = CreateGroupChatroom(ownerId);
        chatroom.Members.Add(CreateAccount(otherMemberId));

        MockAuthorizationSuccess(ownerId);
        _chatRoomRepositoryMock.GetOneRequiredAsync(
                Arg.Any<Expression<Func<Chatroom, bool>>>(), nameof(Chatroom.Members))
            .Returns(chatroom);

        // Act
        await _chatroomService.LeaveChatroom(chatroom.Id);

        // Assert
        Assert.That(chatroom.OwnerId, Is.EqualTo(otherMemberId));
        Assert.That(chatroom.Members.Any(member => member.Id == ownerId), Is.False);
        await _chatRoomRepositoryMock.DidNotReceive().DeleteOneAsync(chatroom.Id);
        await _chatRoomRepositoryMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task DeleteChatroom_ShouldDeleteGeneralChatroom_WhenIssuerIsOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var chatroom = CreateGroupChatroom(ownerId);
        chatroom.Members.Add(CreateAccount(Guid.NewGuid()));

        MockAuthorizationSuccess(ownerId);
        _chatRoomRepositoryMock.GetOneRequiredAsync(Arg.Any<Expression<Func<Chatroom, bool>>>(),
                nameof(Chatroom.Members), nameof(Chatroom.Messages))
            .Returns(chatroom);

        // Act
        await _chatroomService.DeleteChatroom(chatroom.Id);

        // Assert
        await _chatRoomRepositoryMock.Received(1).DeleteOneAsync(chatroom.Id);
        await _chatRoomRepositoryMock.Received(1).SaveChangesAsync();
    }

    private static Account CreateAccount(Guid accountId)
    {
        return new Account
        {
            Id = accountId,
            Name = null,
            PasswordHash = null,
            Email = null
        };
    }

    private static Chatroom CreateGroupChatroom(Guid ownerId)
    {
        var owner = CreateAccount(ownerId);

        return new Chatroom
        {
            Id = Guid.NewGuid(),
            Name = "Group Chatroom",
            ChatRoomType = ChatRoomType.General,
            OwnerId = ownerId,
            Owner = owner,
            Members = [owner]
        };
    }

    private void MockAuthorizationSuccess(Guid userId)
    {
        _authenticationServiceMock.GetUserId().Returns(userId);
        _authorizationServiceMock.AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());
    }
}
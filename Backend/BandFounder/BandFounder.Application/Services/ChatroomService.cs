using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Exceptions;
using BandFounder.Application.Services.Authorization;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services;

public interface IChatroomService
{
    Task<ChatroomDto> CreateChatroom(Account issuer, ChatroomCreateDto request);
    Task<ChatroomDto> GetChatroom(Guid chatroomId);
    Task<IEnumerable<ChatroomDto>> GetUsersChatrooms(Account issuer, ChatroomFilters? filters = null);
    Task<UnreadSummaryDto> GetUnreadSummary(Account issuer);
    Task MarkChatroomAsRead(Guid chatroomId);
    Task DeleteChatroom(Guid chatroomId);
    Task InviteToChatroom(Guid chatroomId, Guid invitedUserId);
    Task LeaveChatroom(Guid chatroomId);
    Task LeaveAllChatrooms(Account account);
}

public class ChatroomService : IChatroomService
{
    private readonly IRepository<Chatroom> _chatRoomRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<ChatroomReadState> _readStateRepository;
    private readonly IRepository<Message> _messageRepository;

    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;

    public ChatroomService(
        IRepository<Chatroom> chatRoomRepository,
        IRepository<Account> accountRepository,
        IRepository<ChatroomReadState> readStateRepository,
        IRepository<Message> messageRepository,
        IAuthenticationService authenticationService,
        IAuthorizationService authorizationService)
    {
        _chatRoomRepository = chatRoomRepository;
        _accountRepository = accountRepository;
        _readStateRepository = readStateRepository;
        _messageRepository = messageRepository;
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
    }

    public async Task<ChatroomDto> CreateChatroom(Account issuer, ChatroomCreateDto request)
    {
        var newChatRoom = await CreateChatroomEntity(issuer, request);
        await _chatRoomRepository.CreateAsync(newChatRoom);

        await EnsureReadStatesAsync(newChatRoom.Members.Select(member => member.Id), newChatRoom.Id, DateTime.UtcNow);
        await _chatRoomRepository.SaveChangesAsync();

        return newChatRoom.ToDto();
    }

    public async Task<ChatroomDto> GetChatroom(Guid chatroomId)
    {
        var userClaims = _authenticationService.GetUserClaims();

        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(
            chatRoom => chatRoom.Id == chatroomId, nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatroom, AuthorizationPolicies.IsMemberOf);

        return chatroom.ToDto();
    }

    public async Task<IEnumerable<ChatroomDto>> GetUsersChatrooms(Account issuer, ChatroomFilters? filters = null)
    {
        var usersChatrooms = (await _chatRoomRepository.GetAsync(
            chatRoom => chatRoom.Members.Contains(issuer),
            includeProperties: [nameof(Chatroom.Members), nameof(Chatroom.Messages)])).ToList();

        if (filters is { ChatRoomType: not null })
        {
            usersChatrooms = usersChatrooms.Where(chatroom => chatroom.ChatRoomType == filters.ChatRoomType).ToList();
        }

        if (filters is { WithUser: not null })
        {
            var selectedUser = await _accountRepository.GetOneRequiredAsync(filters.WithUser);
            usersChatrooms = usersChatrooms.Where(chatroom => chatroom.Members.Contains(selectedUser)).ToList();
        }

        var unreadCounts = await ComputeUnreadCountsAsync(issuer.Id, usersChatrooms.Select(c => c.Id).ToList());

        return usersChatrooms
            .Select(chatroom => chatroom.ToDto(unreadCounts.GetValueOrDefault(chatroom.Id)))
            .OrderByDescending(chatroom => chatroom.LastMessageSentDate ?? DateTime.MinValue);
    }

    public async Task<UnreadSummaryDto> GetUnreadSummary(Account issuer)
    {
        var chatroomIds = (await _chatRoomRepository.GetAsync(
            chatRoom => chatRoom.Members.Contains(issuer)))
            .Select(chatroom => chatroom.Id)
            .ToList();

        var unreadCounts = await ComputeUnreadCountsAsync(issuer.Id, chatroomIds);

        var rooms = unreadCounts
            .Where(pair => pair.Value > 0)
            .Select(pair => new ChatroomUnreadDto
            {
                ChatRoomId = pair.Key,
                UnreadCount = pair.Value
            })
            .ToList();

        return new UnreadSummaryDto
        {
            TotalUnread = rooms.Sum(room => room.UnreadCount),
            Rooms = rooms
        };
    }

    public async Task MarkChatroomAsRead(Guid chatroomId)
    {
        var userId = _authenticationService.GetUserId();
        var userClaims = _authenticationService.GetUserClaims();

        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(
            chatRoom => chatRoom.Id == chatroomId,
            nameof(Chatroom.Members),
            nameof(Chatroom.Messages));

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatroom, AuthorizationPolicies.IsMemberOf);

        var now = DateTime.UtcNow;
        var latestSentDate = chatroom.Messages.Count > 0
            ? chatroom.Messages.Max(message => message.SentDate)
            : (DateTime?)null;
        var lastReadAt = latestSentDate.HasValue && latestSentDate.Value > now
            ? latestSentDate.Value
            : now;

        var existing = await _readStateRepository.GetOneAsync(
            readState => readState.AccountId == userId && readState.ChatRoomId == chatroomId);

        if (existing is null)
        {
            await _readStateRepository.CreateAsync(new ChatroomReadState
            {
                AccountId = userId,
                ChatRoomId = chatroomId,
                LastReadAt = lastReadAt
            });
        }
        else
        {
            existing.LastReadAt = lastReadAt;
        }

        await _readStateRepository.SaveChangesAsync();
    }

    public async Task DeleteChatroom(Guid chatroomId)
    {
        var userClaims = _authenticationService.GetUserClaims();

        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == chatroomId,
            nameof(Chatroom.Members), nameof(Chatroom.Messages));

        switch (chatroom.ChatRoomType)
        {
            case ChatRoomType.General:
                await _authorizationService.AuthorizeRequiredAsync(userClaims, chatroom, AuthorizationPolicies.IsOwnerOf);
                break;

            case ChatRoomType.Direct:
            {
                await _authorizationService.AuthorizeRequiredAsync(userClaims, chatroom, AuthorizationPolicies.IsMemberOf);

                if (chatroom.Members.Count > 1)
                {
                    throw new ForbiddenException("You cannot delete private conversations");
                }

                break;
            }
        }

        await _chatRoomRepository.DeleteOneAsync(chatroom.Id);
        await _chatRoomRepository.SaveChangesAsync();
    }

    public async Task InviteToChatroom(Guid chatroomId, Guid invitedUserId)
    {
        var userClaims = _authenticationService.GetUserClaims();
        if (invitedUserId == _authenticationService.GetUserId())
        {
            throw new BadRequestException("You cannot invite yourself to a chatroom");
        }

        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == chatroomId,
            nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatroom, AuthorizationPolicies.IsMemberOf);

        if (chatroom.ChatRoomType is ChatRoomType.Direct)
        {
            throw new BadRequestException("You cannot invite anyone to a direct chatroom");
        }

        if (chatroom.Members.Any(account => account.Id == invitedUserId))
        {
            throw new BadRequestException("Selected user is already a member of this chatroom");
        }

        var invitedAccount = await _accountRepository.GetOneRequiredAsync(invitedUserId);

        chatroom.Members.Add(invitedAccount);
        await EnsureReadStatesAsync([invitedUserId], chatroomId, DateTime.UtcNow);

        await _chatRoomRepository.SaveChangesAsync();
    }

    public async Task LeaveChatroom(Guid chatroomId)
    {
        var userId = _authenticationService.GetUserId();
        var userClaims = _authenticationService.GetUserClaims();
        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(
            filter: chatRoom => chatRoom.Id == chatroomId, includeProperties: nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatroom, AuthorizationPolicies.IsMemberOf);

        if (chatroom.Members.Count == 1)
        {
            await _chatRoomRepository.DeleteOneAsync(chatroom.Id);
        }
        else
        {
            if (chatroom.OwnerId == userId)
            {
                var luckyUser = chatroom.Members.First(account => account.Id != userId);
                chatroom.OwnerId = luckyUser.Id;
            }

            var memberToRemove = chatroom.Members.FirstOrDefault(account => account.Id == userId);
            if (memberToRemove != null)
            {
                chatroom.Members.Remove(memberToRemove);
            }

            var readState = await _readStateRepository.GetOneAsync(
                state => state.AccountId == userId && state.ChatRoomId == chatroomId);
            if (readState is not null)
            {
                await _readStateRepository.DeleteOneAsync(userId, chatroomId);
            }
        }

        await _chatRoomRepository.SaveChangesAsync();
    }

    public async Task LeaveAllChatrooms(Account account)
    {
        var chatrooms = account.Chatrooms.ToList();
        foreach (var chatroom in chatrooms)
        {
            await LeaveChatroom(chatroom.Id);
        }
    }

    private async Task EnsureReadStatesAsync(IEnumerable<Guid> accountIds, Guid chatRoomId, DateTime lastReadAt)
    {
        foreach (var accountId in accountIds.Distinct())
        {
            var existing = await _readStateRepository.GetOneAsync(
                state => state.AccountId == accountId && state.ChatRoomId == chatRoomId);
            if (existing is not null)
            {
                continue;
            }

            await _readStateRepository.CreateAsync(new ChatroomReadState
            {
                AccountId = accountId,
                ChatRoomId = chatRoomId,
                LastReadAt = lastReadAt
            });
        }
    }

    private async Task<Dictionary<Guid, int>> ComputeUnreadCountsAsync(Guid accountId, List<Guid> chatroomIds)
    {
        var result = chatroomIds.ToDictionary(id => id, _ => 0);
        if (chatroomIds.Count == 0)
        {
            return result;
        }

        var readStates = (await _readStateRepository.GetAsync(
                state => state.AccountId == accountId && chatroomIds.Contains(state.ChatRoomId)))
            .ToDictionary(state => state.ChatRoomId);

        var messages = await _messageRepository.GetAsync(
            message => chatroomIds.Contains(message.ChatRoomId) && message.SenderId != accountId);

        foreach (var message in messages)
        {
            readStates.TryGetValue(message.ChatRoomId, out var readState);
            var lastReadAt = readState?.LastReadAt;
            if (lastReadAt is null || message.SentDate > lastReadAt)
            {
                result[message.ChatRoomId]++;
            }
        }

        return result;
    }

    private async Task<Chatroom> CreateChatroomEntity(Account issuer, ChatroomCreateDto chatroomCreateDto)
    {
        return chatroomCreateDto.ChatRoomType switch
        {
            ChatRoomType.General => await CreateGeneralChatroom(issuer, chatroomCreateDto),
            ChatRoomType.Direct => await CreateDirectChatroom(issuer, chatroomCreateDto),
            _ => throw new BadRequestException("Invalid chatroom type")
        };
    }

    private async Task<Chatroom> CreateGeneralChatroom(Account issuer, ChatroomCreateDto chatroomCreateDto)
    {
        if (string.IsNullOrEmpty(chatroomCreateDto.Name))
        {
            throw new BadRequestException("Chatroom name is required");
        }

        var chatroom = new Chatroom
        {
            ChatRoomType = chatroomCreateDto.ChatRoomType,
            Name = chatroomCreateDto.Name,
            Owner = issuer,
            Members = [issuer]
        };

        return chatroom;
    }

    private async Task<Chatroom> CreateDirectChatroom(Account issuer, ChatroomCreateDto chatroomCreateDto)
    {
        Account recipient;

        try
        {
            recipient = await _accountRepository.GetOneRequiredAsync(chatroomCreateDto.InvitedAccountId!);
        }
        catch (Exception)
        {
            throw new BadRequestException("Invited account is invalid");
        }

        var existingChatrooms = await _chatRoomRepository.GetOneAsync(
            chatRoom => chatRoom.ChatRoomType == ChatRoomType.Direct &
                        chatRoom.Members.Contains(issuer) & chatRoom.Members.Contains(recipient),
            includeProperties: nameof(Chatroom.Members));

        if (existingChatrooms is not null)
        {
            throw new ConflictException("You already have an existing conversation with this user");
        }

        var chatroom = new Chatroom
        {
            ChatRoomType = chatroomCreateDto.ChatRoomType,
            Name = "Private chatroom",
            Owner = issuer,
            Members = [issuer, recipient]
        };

        return chatroom;
    }
}

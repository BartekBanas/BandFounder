using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Error;
using BandFounder.Application.Services.Authorization;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services;

public interface IChatroomService
{
    Task<ChatroomDto> CreateChatroom(Account issuer, ChatroomCreateDto request);
    Task<ChatroomDto> GetChatroom(Guid chatroomId);
    Task<IEnumerable<ChatroomDto>> GetUsersChatrooms(Account issuer, ChatroomFilters? filters = null);
    Task DeleteChatroom(Guid chatroomId);
    Task InviteToChatroom(Guid chatroomId, Guid invitedUserId);
    Task LeaveChatroom(Guid chatroomId);
}

public class ChatroomService : IChatroomService
{
    private readonly IRepository<Chatroom> _chatRoomRepository;
    private readonly IRepository<Account> _accountRepository;
    
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;

    public ChatroomService(IRepository<Chatroom> chatRoomRepository, IRepository<Account> accountRepository,
        IAuthenticationService authenticationService, IAuthorizationService authorizationService)
    {
        _chatRoomRepository = chatRoomRepository;
        _accountRepository = accountRepository;
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
    }

    public async Task<ChatroomDto> CreateChatroom(Account issuer, ChatroomCreateDto request)
    {
        var newChatRoom = await CreateChatroomEntity(issuer, request);
        await _chatRoomRepository.CreateAsync(newChatRoom);

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
        var usersChatrooms = await _chatRoomRepository.GetAsync(
            chatRoom => chatRoom.Members.Contains(issuer), includeProperties: nameof(Chatroom.Members));
        
        if (filters is { ChatRoomType: not null })
        {
            usersChatrooms = usersChatrooms.Where(chatroom => chatroom.ChatRoomType == filters.ChatRoomType);
        }
        
        if (filters is { WithUser: not null })
        {
            var selectedUser = await _accountRepository.GetOneRequiredAsync(filters.WithUser);
            usersChatrooms = usersChatrooms.Where(chatroom => chatroom.Members.Contains(selectedUser));
        }
        
        return usersChatrooms.ToDto();
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
                    throw new ForbiddenError("You cannot delete private conversations");
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
            throw new BadRequestError("You cannot invite yourself to a chatroom");
        }
        
        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == chatroomId,
            nameof(Chatroom.Members));
        
        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatroom, AuthorizationPolicies.IsMemberOf);

        if (chatroom.ChatRoomType is ChatRoomType.Direct)
        {
            throw new BadRequestError("You cannot invite anyone to a direct chatroom");
        }

        if (chatroom.Members.Any(account => account.Id == invitedUserId))
        {
            throw new BadRequestError("Selected user is already a member of this chatroom");
        }

        var invitedAccount = await _accountRepository.GetOneRequiredAsync(invitedUserId);

        chatroom.Members.Add(invitedAccount);

        await _chatRoomRepository.SaveChangesAsync();
    }

    public async Task LeaveChatroom(Guid chatroomId)
    {
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
            chatroom.Members.Remove(
                chatroom.Members.First(account => account.Id == _authenticationService.GetUserId()));
        }

        await _chatRoomRepository.SaveChangesAsync();
    }

    private async Task<Chatroom> CreateChatroomEntity(Account issuer, ChatroomCreateDto chatroomCreateDto)
    {
        return chatroomCreateDto.ChatRoomType switch
        {
            ChatRoomType.General => await CreateGeneralChatroom(issuer, chatroomCreateDto),
            ChatRoomType.Direct => await CreateDirectChatroom(issuer, chatroomCreateDto),
            _ => throw new BadRequestError("Invalid chatroom type")
        };
    }

    private async Task<Chatroom> CreateGeneralChatroom(Account issuer, ChatroomCreateDto chatroomCreateDto)
    {
        if (string.IsNullOrEmpty(chatroomCreateDto.Name))
        {
            throw new BadRequestError("Chatroom name is required");
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
        catch (Exception e)
        {
            throw new BadRequestError("Invited account is invalid");
        }

        var existingChatrooms = await _chatRoomRepository.GetOneAsync(
            chatRoom => chatRoom.ChatRoomType == ChatRoomType.Direct &
                        chatRoom.Members.Contains(issuer) & chatRoom.Members.Contains(recipient),
            includeProperties: nameof(Chatroom.Members));

        if (existingChatrooms is not null)
        {
            throw new ConflictError("You already have an existing conversation with this user");
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
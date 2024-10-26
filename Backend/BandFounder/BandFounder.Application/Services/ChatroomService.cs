using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Services.Authorization;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Errors.Api;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services;

public interface IChatroomService
{
    Task<ChatRoomDto> CreateChatroom(ChatroomCreateDto request);
    Task<ChatRoomDto> GetChatroom(Guid chatroomId);
    Task<IEnumerable<ChatRoomDto>> GetUserChatrooms();
    Task DeleteChatroom(Guid chatroomId);
    Task InviteToChatroom(ChatroomInvitationDto request);
    Task LeaveChatroom(Guid chatroomId);
}

public class ChatroomService : IChatroomService
{
    private readonly IRepository<Chatroom> _chatRoomRepository;
    
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAccountService _accountService;

    public ChatroomService(IRepository<Chatroom> chatRoomRepository, IAuthenticationService authenticationService,
        IAuthorizationService authorizationService, IAccountService accountService)
    {
        _chatRoomRepository = chatRoomRepository;
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
        _accountService = accountService;
    }

    public async Task<ChatRoomDto> CreateChatroom(ChatroomCreateDto request)
    {
        var userId = _authenticationService.GetUserId();

        await _accountService.GetAccountAsync(userId);

        var newChatRoom = await CreateChatroomEntity(userId, request);

        await _chatRoomRepository.CreateAsync(newChatRoom);

        await _chatRoomRepository.SaveChangesAsync();

        return newChatRoom.ToDto();
    }

    public async Task<ChatRoomDto> GetChatroom(Guid chatroomId)
    {
        var user = _authenticationService.GetUserClaims();

        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(
            chatRoom => chatRoom.Id == chatroomId, nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(user, chatroom, AuthorizationPolicies.IsMemberOf);

        return chatroom.ToDto();
    }

    public async Task<IEnumerable<ChatRoomDto>> GetUserChatrooms()
    {
        var userId = _authenticationService.GetUserId();
        
        var account = await _accountService.GetDetailedAccount(userId);
        
        var usersChatrooms = account.Chatrooms;
        
        return usersChatrooms.ToDto();
    }

    public async Task DeleteChatroom(Guid chatroomId)
    {
        var user = _authenticationService.GetUserClaims();

        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == chatroomId,
            nameof(Chatroom.Members), nameof(Chatroom.Messages));

        switch (chatroom.ChatRoomType)
        {
            case ChatRoomType.General:
                await _authorizationService.AuthorizeRequiredAsync(user, chatroom, AuthorizationPolicies.IsOwnerOf);
                break;

            case ChatRoomType.Direct:
            {
                await _authorizationService.AuthorizeRequiredAsync(user, chatroom, AuthorizationPolicies.IsMemberOf);

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
    
    public async Task InviteToChatroom(ChatroomInvitationDto request)
    {
        var userClaims = _authenticationService.GetUserClaims();
        if (request.AccountId == _authenticationService.GetUserId())
        {
            throw new BadRequestError("You cannot invite yourself to a chatroom");
        }
        
        var chatRoom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == request.ChatroomId,
            nameof(Chatroom.Members));
        
        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatRoom, AuthorizationPolicies.IsMemberOf);

        if (chatRoom.ChatRoomType is ChatRoomType.Direct)
        {
            throw new BadRequestError("You cannot invite anyone to a direct messaging chatroom");
        }

        CheckUserMembershipInChatroom(chatRoom, request.AccountId);

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatRoom, AuthorizationPolicies.IsOwnerOf);

        var invitedAccount = await _accountService.GetDetailedAccount(request.AccountId);

        chatRoom.Members.Add(invitedAccount);

        await _chatRoomRepository.SaveChangesAsync();
    }

    private static void CheckUserMembershipInChatroom(Chatroom chatRoom, Guid accountId)
    {
        if (chatRoom.Members.Any(account => account.Id == accountId))
        {
            throw new BadRequestError("Selected user is already a member of this chatroom");
        }
    }

    public async Task LeaveChatroom(Guid chatroomId)
    {
        var user = _authenticationService.GetUserClaims();
        var chatroom = await _chatRoomRepository.GetOneRequiredAsync(
            filter: chatRoom => chatRoom.Id == chatroomId, includeProperties: nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(user, chatroom, AuthorizationPolicies.IsMemberOf);

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

    private async Task<Chatroom> CreateChatroomEntity(Guid issuerId, ChatroomCreateDto chatroomCreateDto)
    {
        var issuer = await _accountService.GetDetailedAccount(issuerId);

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
            recipient = await _accountService.GetDetailedAccount((Guid)chatroomCreateDto.InvitedAccountId!);
        }
        catch (Exception e)
        {
            throw new BadRequestError("Invited account is invalid");
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
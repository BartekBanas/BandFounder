using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Messages;
using BandFounder.Application.Services.Authorization;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services;

public interface IMessageService
{
    Task SendMessage(SendMessageDto dto);
    Task<IEnumerable<MessageDto>> GetChatroomMessages(Guid chatRoomId);
    Task<IEnumerable<MessageDto>> GetChatroomPagedMessages(GetPagedMessagesDto request);
}

public class MessageService : IMessageService
{
    private readonly IRepository<Chatroom> _chatRoomRepository;
    private readonly IRepository<Message> _messageRepository;

    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;

    public MessageService(IRepository<Chatroom> chatRoomRepository, IRepository<Message> messageRepository,
        IAuthenticationService authenticationService, IAuthorizationService authorizationService)
    {
        _chatRoomRepository = chatRoomRepository;
        _messageRepository = messageRepository;
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
    }

    public async Task SendMessage(SendMessageDto dto)
    {
        var userClaims = _authenticationService.GetUserClaims();
        var userId = _authenticationService.GetUserId();

        var chatRoom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == dto.ChatRoomId, 
            nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatRoom, AuthorizationPolicies.IsMemberOf);

        var newMessage = new Message
        {
            ChatRoomId = chatRoom.Id,
            SenderId = userId,
            Content = dto.Content,
            SentDate = DateTime.UtcNow
        };

        chatRoom.Messages.Add(newMessage);

        await _messageRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<MessageDto>> GetChatroomMessages(Guid chatRoomId)
    {
        var userClaims = _authenticationService.GetUserClaims();

        var chatRoom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == chatRoomId, 
            nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatRoom, AuthorizationPolicies.IsMemberOf);
        
        var messages = await _messageRepository
            .GetAsync(
                message => message.ChatRoomId == chatRoomId,
                query => query.OrderBy(message => message.SentDate) // Sent Date ascending
            );

        return messages.ToDto();
    }
    
    public async Task<IEnumerable<MessageDto>> GetChatroomPagedMessages(GetPagedMessagesDto request)
    {
        var userClaims = _authenticationService.GetUserClaims();

        var chatRoom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == request.ChatRoomId, 
            nameof(Chatroom.Members));

        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatRoom, AuthorizationPolicies.IsMemberOf);

        var messages = await _messageRepository
            .GetAsync(
                message => message.ChatRoomId == request.ChatRoomId,
                query => query.OrderBy(message => message.SentDate)
            );
        
        var paginatedMessages = messages
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        return paginatedMessages.ToDto();
    }
}
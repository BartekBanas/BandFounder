using BandFounder.Application.Dtos.Messages;
using BandFounder.Application.Services.Authorization;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace BandFounder.Application.Services;

public interface IMessageService
{
    Task<Message> SendMessage(SendMessageDto dto);
    Task<IEnumerable<Message>> GetChatroomMessages(GetMessagesRequest request);
}

public class MessageService : IMessageService
{
    private readonly IRepository<Chatroom> _chatRoomRepository;
    private readonly IRepository<Message> _messageRepository;

    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMessageEmailNotificationService _messageEmailNotificationService;
    private readonly ILogger<MessageService> _logger;

    public MessageService(IRepository<Chatroom> chatRoomRepository, IRepository<Message> messageRepository,
        IAuthenticationService authenticationService, IAuthorizationService authorizationService,
        IMessageEmailNotificationService messageEmailNotificationService,
        ILogger<MessageService> logger)
    {
        _chatRoomRepository = chatRoomRepository;
        _messageRepository = messageRepository;
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
        _messageEmailNotificationService = messageEmailNotificationService;
        _logger = logger;
    }

    public async Task<Message> SendMessage(SendMessageDto dto)
    {
        var userClaims = _authenticationService.GetUserClaims();
        var userId = _authenticationService.GetUserId();

        var chatRoom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == dto.ChatRoomId, 
            nameof(Chatroom.Members),
            $"{nameof(Chatroom.Members)}.{nameof(Account.NotificationPreferences)}");

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

        try
        {
            await _messageEmailNotificationService.QueueAsync(chatRoom, newMessage, userId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to queue email notification for message in chatroom {ChatRoomId}",
                chatRoom.Id);
        }

        return newMessage;
    }

    public async Task<IEnumerable<Message>> GetChatroomMessages(GetMessagesRequest request)
    {
        var userClaims = _authenticationService.GetUserClaims();
        
        var chatRoom = await _chatRoomRepository.GetOneRequiredAsync(chatRoom => chatRoom.Id == request.ChatRoomId,
            includeProperties: nameof(Chatroom.Members));
        
        await _authorizationService.AuthorizeRequiredAsync(userClaims, chatRoom, AuthorizationPolicies.IsMemberOf);
        
        var messages = await _messageRepository
            .GetAsync(
                message => message.ChatRoomId == request.ChatRoomId,
                query => query.OrderByDescending(message => message.SentDate)
            );
        
        if (request is { PageNumber: not null, PageSize: not null })
        {
            messages = messages
                .Skip((request.PageNumber.Value - 1) * request.PageSize.Value)
                .Take(request.PageSize.Value);
        }
        
        return messages;
    }
}

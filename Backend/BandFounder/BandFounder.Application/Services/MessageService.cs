using BandFounder.Application.Dtos.Messages;
using BandFounder.Application.Services.Authorization;
using BandFounder.Application.Services.Email;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

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
    private readonly IRepository<ChatroomReadState> _readStateRepository;
    private readonly IEmailNotificationOutboxRepository _outboxRepository;

    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MessageEmailNotificationOptions _notificationOptions;

    public MessageService(IRepository<Chatroom> chatRoomRepository, IRepository<Message> messageRepository,
        IRepository<ChatroomReadState> readStateRepository,
        IEmailNotificationOutboxRepository outboxRepository,
        IAuthenticationService authenticationService, IAuthorizationService authorizationService,
        IUnitOfWork unitOfWork,
        IOptions<MessageEmailNotificationOptions> notificationOptions)
    {
        _chatRoomRepository = chatRoomRepository;
        _messageRepository = messageRepository;
        _readStateRepository = readStateRepository;
        _outboxRepository = outboxRepository;
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
        _unitOfWork = unitOfWork;
        _notificationOptions = notificationOptions.Value;
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

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            chatRoom.Messages.Add(newMessage);
            await _messageRepository.SaveChangesAsync();
            await EnsureNotificationIntentAsync(chatRoom, newMessage, userId);
            await _outboxRepository.SaveChangesAsync();
        });

        return newMessage;
    }

    private async Task EnsureNotificationIntentAsync(Chatroom chatRoom, Message message, Guid senderId)
    {
        var senderName = chatRoom.Members
            .FirstOrDefault(member => member.Id == senderId)?.Name ?? "Someone";
        var now = DateTime.UtcNow;
        var snippet = Truncate(message.Content);

        foreach (var recipient in chatRoom.Members.Where(member =>
                     member.Id != senderId &&
                     member.NotificationPreferences.EmailOnNewMessage))
        {
            await _outboxRepository.AcquireQueueLockAsync(recipient.Id, chatRoom.Id);

            var unreadCount = await GetUnreadMessageCountAsync(recipient.Id, chatRoom.Id);
            var existing = (await _outboxRepository.GetAsync(outbox =>
                outbox.RecipientAccountId == recipient.Id &&
                outbox.ChatRoomId == chatRoom.Id &&
                (outbox.Status == EmailNotificationStatus.Pending ||
                 outbox.Status == EmailNotificationStatus.Processing))).ToList();
            var pending = existing.FirstOrDefault(
                outbox => outbox.Status == EmailNotificationStatus.Pending);

            if (pending is not null &&
                await _outboxRepository.TryUpdatePendingAsync(
                    pending.Id,
                    now.AddMinutes(GetDelayMinutes(recipient.NotificationPreferences.EmailUnreadDelayMinutes)),
                    senderName,
                    chatRoom.Name,
                    snippet,
                    unreadCount))
            {
                continue;
            }

            await _outboxRepository.CreateAsync(new EmailNotificationOutbox
            {
                Id = Guid.NewGuid(),
                RecipientAccountId = recipient.Id,
                ChatRoomId = chatRoom.Id,
                Status = EmailNotificationStatus.Pending,
                NotBeforeUtc = now.AddMinutes(
                    GetDelayMinutes(recipient.NotificationPreferences.EmailUnreadDelayMinutes)),
                CreatedAt = now,
                LatestSenderName = senderName,
                ChatroomName = chatRoom.Name,
                Snippet = snippet,
                UnreadCountHint = unreadCount
            });
        }
    }

    private async Task<int> GetUnreadMessageCountAsync(Guid recipientAccountId, Guid chatRoomId)
    {
        var readState = await _readStateRepository.GetOneAsync(
            state => state.AccountId == recipientAccountId &&
                     state.ChatRoomId == chatRoomId);

        if (readState?.LastReadAt is DateTime lastReadAt)
        {
            return await _messageRepository.CountAsync(message =>
                message.ChatRoomId == chatRoomId &&
                message.SenderId != recipientAccountId &&
                message.SentDate > lastReadAt);
        }

        return await _messageRepository.CountAsync(message =>
            message.ChatRoomId == chatRoomId &&
            message.SenderId != recipientAccountId);
    }

    private int GetDelayMinutes(int configuredDelayMinutes)
    {
        return MessageEmailNotificationOptions.AllowedDelayMinutes.Contains(configuredDelayMinutes)
            ? configuredDelayMinutes
            : MessageEmailNotificationOptions.DefaultDelayMinutes;
    }

    private string Truncate(string content)
    {
        var normalized = content.Trim();
        return normalized.Length <= _notificationOptions.MaxSnippetLength
            ? normalized
            : $"{normalized[.._notificationOptions.MaxSnippetLength]}…";
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

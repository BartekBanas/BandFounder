namespace BandFounder.Application.Dtos.Messages;

public class SendMessageDto(Guid chatRoomId, string content)
{
    public Guid ChatRoomId { get; } = chatRoomId;
    public string Content { get; } = content;
}
namespace BandFounder.Application.Dtos.Messages;

public class SendMessageDto(string content)
{
    public Guid ChatRoomId { get; }
    public string Content { get; } = content;
}
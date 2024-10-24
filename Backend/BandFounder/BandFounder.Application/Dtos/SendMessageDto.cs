namespace BandFounder.Application.Dtos;

public class SendMessageDto(string content)
{
    public Guid ChatRoomId { get; }
    public string Content { get; } = content;
}
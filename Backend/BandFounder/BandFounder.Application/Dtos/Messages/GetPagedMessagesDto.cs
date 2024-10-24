namespace BandFounder.Application.Dtos.Messages;

public class GetPagedMessagesDto
{
    public Guid ChatRoomId { get; }
    public int PageSize { get; }
    public int PageNumber { get; }
}
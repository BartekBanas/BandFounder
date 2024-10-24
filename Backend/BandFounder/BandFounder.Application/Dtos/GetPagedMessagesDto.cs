namespace BandFounder.Application.Dtos;

public class GetPagedMessagesDto
{
    public Guid ChatRoomId { get; }
    public int PageSize { get; }
    public int PageNumber { get; }
}
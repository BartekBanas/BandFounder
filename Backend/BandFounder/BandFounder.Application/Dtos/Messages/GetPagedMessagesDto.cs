namespace BandFounder.Application.Dtos.Messages;

public class GetPagedMessagesDto(Guid chatRoomId, int pageSize, int pageNumber)
{
    public Guid ChatRoomId { get; } = chatRoomId;
    public int PageSize { get; } = pageSize;
    public int PageNumber { get; } = pageNumber;
}
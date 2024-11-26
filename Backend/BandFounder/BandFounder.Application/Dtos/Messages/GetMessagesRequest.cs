namespace BandFounder.Application.Dtos.Messages;

public class GetMessagesRequest
{
    public Guid ChatRoomId { get; set; }
    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }
}
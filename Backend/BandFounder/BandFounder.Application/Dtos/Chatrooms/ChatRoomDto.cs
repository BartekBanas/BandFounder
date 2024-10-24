namespace BandFounder.Application.Dtos.Chatrooms;

public class ChatRoomDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required List<Guid> MembersIds { get; init; }
}
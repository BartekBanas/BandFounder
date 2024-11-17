using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Chatrooms;

public class ChatroomDto
{
    public required Guid Id { get; init; }
    public ChatRoomType Type { get; set; }
    public string Name { get; init; }
    public required List<Guid> MembersIds { get; init; }
}
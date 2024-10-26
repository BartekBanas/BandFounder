using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Chatrooms;

public class ChatroomCreateDto
{
    public required ChatRoomType ChatRoomType { get; init; }
    public string? Name { get; init; }
    public Guid? InvitedAccountId { get; init; }
}
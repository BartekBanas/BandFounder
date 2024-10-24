using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Chatrooms;

public class ChatroomCreateDto
{
    public ChatRoomType ChatRoomType { get; }
    public required string Name { get; set; }
    public Guid InvitedAccountId { get; set; }
}
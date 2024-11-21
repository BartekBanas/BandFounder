using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Chatrooms;

public class ChatroomFilters
{
    public ChatRoomType? ChatRoomType { get; set; }
    public Guid? WithUser { get; set; }
}
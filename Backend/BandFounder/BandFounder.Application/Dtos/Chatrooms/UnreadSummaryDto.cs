namespace BandFounder.Application.Dtos.Chatrooms;

public class UnreadSummaryDto
{
    public int TotalUnread { get; init; }
    public required List<ChatroomUnreadDto> Rooms { get; init; }
}

public class ChatroomUnreadDto
{
    public Guid ChatRoomId { get; init; }
    public int UnreadCount { get; init; }
}

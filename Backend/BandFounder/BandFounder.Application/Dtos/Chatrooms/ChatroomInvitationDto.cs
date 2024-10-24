namespace BandFounder.Application.Dtos.Chatrooms;

public class ChatroomInvitationDto
{
    public required Guid ChatroomId { get; init; }
    public required Guid AccountId { get; init; }
}
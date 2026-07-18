using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Domain.Entities;

namespace Api.IntegrationTests;

[TestFixture]
public class ChatroomUnreadTests : IntegrationTestBase
{
    [Test]
    public async Task UnreadSummary_ExcludesOwnMessages_AndClearsAfterMarkRead()
    {
        var ownerToken = await RegisterAsync("unreadowner", "unreadowner@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "Unread Room"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var memberToken = await RegisterAsync("unreadmember", "unreadmember@example.com");
        AuthenticateAs(memberToken);
        var member = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));

        AuthenticateAs(ownerToken);
        Assert.That(
            (await Client.PostAsync($"/api/chatrooms/{chatroom.Id}/invite/{member.Id}", null)).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        await SendMessageAsync(chatroom.Id, "From owner 1");
        await SendMessageAsync(chatroom.Id, "From owner 2");

        AuthenticateAs(memberToken);
        var summaryResponse = await Client.GetAsync("/api/chatrooms/unread-summary");
        Assert.That(summaryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var summary = await ReadJsonAsync<UnreadSummaryDto>(summaryResponse);

        Assert.That(summary.TotalUnread, Is.EqualTo(2));
        Assert.That(summary.Rooms, Has.Count.EqualTo(1));
        Assert.That(summary.Rooms[0].ChatRoomId, Is.EqualTo(chatroom.Id));
        Assert.That(summary.Rooms[0].UnreadCount, Is.EqualTo(2));

        var listResponse = await Client.GetAsync("/api/chatrooms");
        var rooms = await ReadJsonAsync<List<ChatroomDto>>(listResponse);
        var listed = rooms.Single(room => room.Id == chatroom.Id);
        Assert.That(listed.UnreadCount, Is.EqualTo(2));
        Assert.That(summary.TotalUnread, Is.EqualTo(rooms.Sum(room => room.UnreadCount)));

        await SendMessageAsync(chatroom.Id, "From member");
        summary = await ReadJsonAsync<UnreadSummaryDto>(await Client.GetAsync("/api/chatrooms/unread-summary"));
        Assert.That(summary.TotalUnread, Is.EqualTo(2), "Own messages must not increase unread count");

        var markReadResponse = await Client.PutAsync($"/api/chatrooms/{chatroom.Id}/read", null);
        Assert.That(markReadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        summary = await ReadJsonAsync<UnreadSummaryDto>(await Client.GetAsync("/api/chatrooms/unread-summary"));
        Assert.That(summary.TotalUnread, Is.EqualTo(0));
        Assert.That(summary.Rooms, Is.Empty);

        AuthenticateAs(ownerToken);
        summary = await ReadJsonAsync<UnreadSummaryDto>(await Client.GetAsync("/api/chatrooms/unread-summary"));
        Assert.That(summary.TotalUnread, Is.EqualTo(1));
        Assert.That(summary.Rooms.Single().UnreadCount, Is.EqualTo(1));
    }

    [Test]
    public async Task MarkRead_AsNonMember_ReturnsForbidden()
    {
        var ownerToken = await RegisterAsync("markowner", "markowner@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "Private Unread Room"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var outsiderToken = await RegisterAsync("markoutsider", "markoutsider@example.com");
        AuthenticateAs(outsiderToken);

        var markReadResponse = await Client.PutAsync($"/api/chatrooms/{chatroom.Id}/read", null);
        Assert.That(markReadResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private async Task SendMessageAsync(Guid chatroomId, string content)
    {
        var sendContent = new StringContent(
            JsonSerializer.Serialize(content),
            Encoding.UTF8,
            "application/json");
        var sendResponse = await Client.PostAsync($"/api/chatrooms/{chatroomId}/messages", sendContent);
        Assert.That(sendResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}

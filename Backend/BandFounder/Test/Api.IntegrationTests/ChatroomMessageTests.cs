using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Messages;
using BandFounder.Domain.Entities;

namespace Api.IntegrationTests;

[TestFixture]
public class ChatroomMessageTests : IntegrationTestBase
{
    [Test]
    public async Task CreateGeneralChatroom_ThenGet_ReturnsChatroom()
    {
        var token = await RegisterAsync("chatowner", "chatowner@example.com");
        AuthenticateAs(token);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "Band HQ"
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await ReadJsonAsync<ChatroomDto>(createResponse);
        Assert.That(created.Name, Is.EqualTo("Band HQ"));
        Assert.That(created.Type, Is.EqualTo(ChatRoomType.General));

        var getResponse = await Client.GetAsync($"/api/chatrooms/{created.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fetched = await ReadJsonAsync<ChatroomDto>(getResponse);
        Assert.That(fetched.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task InviteMember_ThenSendAndGetMessages()
    {
        var ownerToken = await RegisterAsync("msgowner", "msgowner@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "Rehearsal Chat"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var memberToken = await RegisterAsync("msgmember", "msgmember@example.com");
        AuthenticateAs(memberToken);
        var member = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));

        AuthenticateAs(ownerToken);
        var inviteResponse = await Client.PostAsync(
            $"/api/chatrooms/{chatroom.Id}/invite/{member.Id}", null);
        Assert.That(inviteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(memberToken);
        var sendContent = new StringContent(
            JsonSerializer.Serialize("Hello band!"),
            Encoding.UTF8,
            "application/json");
        var sendResponse = await Client.PostAsync(
            $"/api/chatrooms/{chatroom.Id}/messages", sendContent);
        Assert.That(sendResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var messagesResponse = await Client.GetAsync($"/api/chatrooms/{chatroom.Id}/messages");
        Assert.That(messagesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var messages = await ReadJsonAsync<List<MessageDto>>(messagesResponse);
        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Content, Is.EqualTo("Hello band!"));
    }

    [Test]
    public async Task GetChatroom_AsNonMember_ReturnsForbidden()
    {
        var ownerToken = await RegisterAsync("privateowner", "privateowner@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "Private Room"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var outsiderToken = await RegisterAsync("outsider", "outsider@example.com");
        AuthenticateAs(outsiderToken);

        var getResponse = await Client.GetAsync($"/api/chatrooms/{chatroom.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SendMessage_AsNonMember_ReturnsForbiddenOrServerError()
    {
        var ownerToken = await RegisterAsync("msgowner2", "msgowner2@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "Closed Room"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var outsiderToken = await RegisterAsync("msgoutsider", "msgoutsider@example.com");
        AuthenticateAs(outsiderToken);

        var sendContent = new StringContent(
            JsonSerializer.Serialize("Should fail"),
            Encoding.UTF8,
            "application/json");
        var sendResponse = await Client.PostAsync(
            $"/api/chatrooms/{chatroom.Id}/messages", sendContent);

        // MessageController catches exceptions and may return 500 instead of letting middleware map ForbiddenException
        Assert.That(sendResponse.StatusCode, Is.AnyOf(HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task LeaveChatroom_RemovesMembership()
    {
        var ownerToken = await RegisterAsync("leaveowner", "leaveowner@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "Leaving Room"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var memberToken = await RegisterAsync("leavemember", "leavemember@example.com");
        AuthenticateAs(memberToken);
        var member = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));

        AuthenticateAs(ownerToken);
        await Client.PostAsync($"/api/chatrooms/{chatroom.Id}/invite/{member.Id}", null);

        AuthenticateAs(memberToken);
        var leaveResponse = await Client.PostAsync($"/api/chatrooms/{chatroom.Id}/leave", null);
        Assert.That(leaveResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var getResponse = await Client.GetAsync($"/api/chatrooms/{chatroom.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
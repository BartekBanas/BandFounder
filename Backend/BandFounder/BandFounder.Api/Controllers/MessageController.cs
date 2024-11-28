using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BandFounder.Api.WebSockets;
using BandFounder.Application.Dtos.Messages;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/chatrooms/{chatRoomId:guid}/messages")]
public class MessageController : Controller
{
    private readonly IMessageService _messageService;
    private readonly IAccountService _accountService;
    private readonly WebSocketConnectionManager _webSocketConnectionManager;
    private readonly JsonSerializerOptions _serializationOptions;

    public MessageController(
        IMessageService messageService,
        IAccountService accountService,
        WebSocketConnectionManager webSocketConnectionManager)
    {
        _messageService = messageService;
        _accountService = accountService;
        _webSocketConnectionManager = webSocketConnectionManager;
        _serializationOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve
        };
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromRoute] Guid chatRoomId, [FromBody] string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return BadRequest("Message content cannot be empty");
        }

        try
        {
            // Send the message via the service
            await _messageService.SendMessage(new SendMessageDto(chatRoomId, message));

            // Create the message payload
            var senderAccount = await _accountService.GetAccountAsync();
            var messagePayload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new { senderId = senderAccount.Id, content = message }, _serializationOptions));
            
            // Broadcast the message to WebSocket clients in the chat room
            foreach (var socket in _webSocketConnectionManager.GetConnections(chatRoomId))
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(messagePayload), 
                        WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            // Log the exception (logging mechanism not shown here)
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetChatroomMessages([FromRoute] Guid chatRoomId,
        [FromQuery] int? pageSize, [FromQuery] int? pageNumber)
    {
        object messagesDto;

        if (pageSize != null && pageNumber != null)
        {
            var request = new GetPagedMessagesDto(chatRoomId, (int)pageSize, (int)pageNumber);
            messagesDto = await _messageService.GetChatroomPagedMessages(request);
        }
        else
        {
            messagesDto = await _messageService.GetChatroomMessages(chatRoomId);
        }
        
        return Ok(messagesDto);
    }
}
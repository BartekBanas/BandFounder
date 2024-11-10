using BandFounder.Application.Dtos.Messages;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/chatrooms/{chatRoomId:guid}/messages")]
public class MessageController : Controller
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromRoute]Guid chatRoomId, string message)
    {
        await _messageService.SendMessage(new SendMessageDto(chatRoomId, message));

        return Ok();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetChatroomMessages([FromRoute] Guid chatRoomId,
        [FromQuery]int? pageSize, [FromQuery]int? pageNumber)
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
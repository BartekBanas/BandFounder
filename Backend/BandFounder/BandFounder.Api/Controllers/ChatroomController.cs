using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/chatroom")]
public class ChatroomController : Controller
{
    private readonly IChatroomService _chatroomService;

    public ChatroomController(IChatroomService chatroomService)
    {
        _chatroomService = chatroomService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUserChatrooms()
    {
        var chatRoomDtos = await _chatroomService.GetUserChatrooms();

        return Ok(chatRoomDtos);
    }

    [Authorize]
    [HttpGet("{chatroomId:guid}")]
    public async Task<IActionResult> GetChatroom(Guid chatroomId)
    {
        var chatRoomDto = await _chatroomService.GetChatroom(chatroomId);

        return Ok(chatRoomDto);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateChatroom(ChatroomCreateDto dto)
    {
        var createdChatroomDto = await _chatroomService.CreateChatroom(dto);

        return Created($"api/chatroom/{createdChatroomDto.Id}", createdChatroomDto);
    }

    [Authorize]
    [HttpPost("{chatroomId:guid}/invite/{invitedUserId:guid}")]
    public async Task<IActionResult> InviteToChatroom(Guid chatroomId, Guid invitedUserId)
    {
        await _chatroomService.InviteToChatroom(chatroomId, invitedUserId);

        return Ok();
    }

    [Authorize]
    [HttpPost("{chatroomId:guid}/leave")]
    public async Task<IActionResult> LeaveChatroom([FromRoute] Guid chatroomId)
    {
        await _chatroomService.LeaveChatroom(chatroomId);

        return Ok();
    }
}
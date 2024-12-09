using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/chatrooms")]
public class ChatroomController : Controller
{
    private readonly IChatroomService _chatroomService;
    private readonly IAccountService _accountService;

    public ChatroomController(IChatroomService chatroomService, IAccountService accountService)
    {
        _chatroomService = chatroomService;
        _accountService = accountService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUserChatrooms([FromQuery] ChatroomFilters filters)
    {
        var issuer = await _accountService.GetDetailedAccount(includeProperties: nameof(Account.Chatrooms));
        var chatRoomDtos = await _chatroomService.GetUsersChatrooms(issuer, filters);

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
        var issuer = await _accountService.GetAccountAsync();
        var createdChatroomDto = await _chatroomService.CreateChatroom(issuer, dto);

        return Created($"api/chatrooms/{createdChatroomDto.Id}", createdChatroomDto);
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
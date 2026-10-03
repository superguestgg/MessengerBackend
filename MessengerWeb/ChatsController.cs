using System.ComponentModel.DataAnnotations;
using Mediator;
using Messenger.Chats.Application;
using Messenger.Chats.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatsController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChatResult>>> List()
    {
        var result = await _mediator.Send(new GetChatsQuery(User.GetAccountId()));

        return Ok(result);
    }

    [HttpGet("{chatId:guid}")]
    public async Task<ActionResult<ChatResult>> Get(Guid chatId)
    {
        var result = await _mediator.Send(new GetChatQuery(
            User.GetAccountId(),
            chatId));

        return Ok(result);
    }

    // Returns the existing chat if the two accounts already have one.
    [HttpPost("direct")]
    public async Task<ActionResult<CreateChatResult>> CreateDirect(
        CreateDirectChatRequest request)
    {
        var result = await _mediator.Send(new CreateDirectChatCommand(
            User.GetAccountId(),
            request.UserId));

        return Ok(result);
    }

    [HttpPost("group")]
    public async Task<ActionResult<CreateChatResult>> CreateGroup(
        CreateGroupChatRequest request)
    {
        var result = await _mediator.Send(new CreateGroupChatCommand(
            User.GetAccountId(),
            request.Title,
            request.MemberIds ?? []));

        return Ok(result);
    }

    [HttpPost("{chatId:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid chatId,
        AddChatMemberRequest request)
    {
        await _mediator.Send(new AddChatMemberCommand(
            User.GetAccountId(),
            chatId,
            request.UserId));

        return NoContent();
    }

    // Removing yourself leaves the chat.
    [HttpDelete("{chatId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid chatId,
        Guid userId)
    {
        await _mediator.Send(new RemoveChatMemberCommand(
            User.GetAccountId(),
            chatId,
            userId));

        return NoContent();
    }

    [HttpPut("{chatId:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(
        Guid chatId,
        Guid userId,
        ChangeChatMemberRoleRequest request)
    {
        await _mediator.Send(new ChangeChatMemberRoleCommand(
            User.GetAccountId(),
            chatId,
            userId,
            request.Role!.Value));

        return NoContent();
    }

    [HttpGet("{chatId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<MessageResult>>> GetMessages(
        Guid chatId,
        [FromQuery] long? after,
        [FromQuery] long? before,
        [FromQuery, Range(1, MaxPageSize)] int limit = DefaultPageSize)
    {
        var result = await _mediator.Send(new GetMessagesQuery(
            User.GetAccountId(),
            chatId,
            after,
            before,
            limit));

        return Ok(result);
    }

    // Long polling: answers as soon as a matching message newer than "after" appears,
    // or with an empty list when the timeout runs out. Pass nextAfterSeq as "after" next time.
    [HttpGet("{chatId:guid}/messages/wait")]
    public async Task<ActionResult<WaitForMessagesResult>> WaitForMessages(
        Guid chatId,
        [FromQuery, Required, Range(0, long.MaxValue)] long? after,
        [FromQuery] Guid? from,
        [FromQuery, Range(1, long.MaxValue)] long? replyTo,
        [FromQuery, Range(0, MaxWaitSeconds)] int timeout = DefaultWaitSeconds)
    {
        var result = await _mediator.Send(
            new WaitForMessagesQuery(
                User.GetAccountId(),
                chatId,
                after!.Value,
                from,
                replyTo,
                TimeSpan.FromSeconds(timeout)),
            HttpContext.RequestAborted);

        return Ok(result);
    }

    [HttpPost("{chatId:guid}/messages")]
    public async Task<ActionResult<SendMessageResult>> SendMessage(
        Guid chatId,
        SendMessageRequest request)
    {
        var result = await _mediator.Send(new SendMessageCommand(
            User.GetAccountId(),
            chatId,
            request.Text,
            request.ReplyToSeq));

        return Ok(result);
    }

    private const int DefaultPageSize = 50;

    private const int MaxPageSize = 100;

    private const int DefaultWaitSeconds = 30;

    // Kept below the usual 60 s proxy timeout; WaitForMessagesHandler.MaxTimeout matches it.
    private const int MaxWaitSeconds = 50;
}

public sealed record CreateDirectChatRequest(
    [Required]
    Guid UserId
);

public sealed record CreateGroupChatRequest(
    [Required, MaxLength(ChatTitle.MaxLength)]
    string Title,

    [MaxLength(100)]
    IReadOnlyList<Guid>? MemberIds
);

public sealed record AddChatMemberRequest(
    [Required]
    Guid UserId
);

public sealed record ChangeChatMemberRoleRequest(
    [Required]
    ChatRole? Role
);

public sealed record SendMessageRequest(
    [Required, MaxLength(MessageText.MaxLength)]
    string Text,

    [Range(1, long.MaxValue)]
    long? ReplyToSeq
);

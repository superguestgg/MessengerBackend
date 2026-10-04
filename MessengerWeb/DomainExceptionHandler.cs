using Messenger.Users.Domain;
using Messenger.Chats.Application;
using ChatsDomain = Messenger.Chats.Domain;
using FilesDomain = Messenger.Files.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public DomainExceptionHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            EmailAlreadyTakenException => (StatusCodes.Status409Conflict, "Email already taken"),
            AccountNotFoundException => (StatusCodes.Status404NotFound, "Account not found"),
            AccessTokenNotFoundException => (StatusCodes.Status404NotFound, "Access token not found"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Invalid credentials"),
            AccessDeniedException => (StatusCodes.Status403Forbidden, "Access denied"),
            IncorrectPasswordException => (StatusCodes.Status403Forbidden, "Incorrect password"),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            ChatsDomain.ChatNotFoundException => (StatusCodes.Status404NotFound, "Chat not found"),
            ChatsDomain.ParticipantNotFoundException => (StatusCodes.Status404NotFound, "Account not found"),
            ChatsDomain.AttachmentNotFoundException => (StatusCodes.Status404NotFound, "File not found"),
            ChatsDomain.ChatAccessDeniedException => (StatusCodes.Status403Forbidden, "Access denied"),
            ChatsDomain.ChatConcurrencyException => (StatusCodes.Status409Conflict, "Concurrent change"),
            ChatsDomain.DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            FilesDomain.FileTooLargeException => (StatusCodes.Status413PayloadTooLarge, "File too large"),
            FilesDomain.DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            TooManyWaitsException => (StatusCodes.Status429TooManyRequests, "Too many waits"),
            _ => (0, null)
        };

        if (status == 0)
            return false;

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message
            }
        });
    }
}

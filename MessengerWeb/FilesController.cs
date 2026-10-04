using System.ComponentModel.DataAnnotations;
using Mediator;
using Messenger.Files.Application;
using Messenger.Files.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    // The file plus multipart framing; a larger body is cut off before it is read.
    private const long MaxRequestSize = StoredFile.MaxSize + 64 * 1024;

    private readonly IMediator _mediator;

    public FilesController(IMediator mediator)
    {
        _mediator = mediator;
    }


    // Step one of sending a file: upload it, then pass the id in fileIds or voice of a message.
    [HttpPost]
    [RequestSizeLimit(MaxRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestSize)]
    public async Task<ActionResult<UploadFileResult>> Upload(
        [Required] IFormFile file)
    {
        await using var content = file.OpenReadStream();

        var result = await _mediator.Send(new UploadFileCommand(
            User.GetAccountId(),
            file.FileName,
            file.ContentType,
            file.Length,
            content));

        return Ok(result);
    }
}

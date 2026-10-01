using Core.Application.Livestock;
using Core.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/animals")]
public sealed class AnimalPhotosController(IAnimalPhotoService photoService, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("{id:guid}/photo")]
    [Authorize]
    [HasPermission("animals.change_animal")]
    public async Task<ActionResult<AnimalPhotoResult>> Upload(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("An image file is required.", nameof(file));
        }

        await using var stream = file.OpenReadStream();

        var result = await photoService.UploadAsync(
            id,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            currentUser.UserId,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}/photo")]
    [Authorize]
    [HasPermission("animals.change_animal")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await photoService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

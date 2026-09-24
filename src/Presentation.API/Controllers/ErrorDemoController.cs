using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/demo/errors")]
public sealed class ErrorDemoController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("{kind}")]
    public IActionResult Throw(string kind)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return kind switch
        {
            "not-found" => throw new KeyNotFoundException("The sample resource was not found."),
            "invalid-operation" => throw new InvalidOperationException("The sample operation is invalid."),
            "unexpected" => throw new Exception("Simulated internal detail that must remain private."),
            _ => BadRequest("Use not-found, invalid-operation, or unexpected.")
        };
    }
}

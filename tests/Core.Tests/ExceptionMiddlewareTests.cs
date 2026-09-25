using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Presentation.API.Middleware;

namespace Core.Tests;

public sealed class ExceptionMiddlewareTests
{
    [Theory]
    [InlineData("not-found", 404, "Not Found")]
    [InlineData("invalid-operation", 400, "Bad Request")]
    [InlineData("unexpected", 500, "Internal Server Error")]
    public async Task MapsExceptionsToProblemDetails(string kind, int expectedStatus, string expectedTitle)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/demo/errors/" + kind;
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw kind switch
        {
            "not-found" => new KeyNotFoundException("The item was not found."),
            "invalid-operation" => new InvalidOperationException("The operation is invalid."),
            _ => new Exception("Sensitive internal detail")
        };

        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var problem = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("about:blank", problem.RootElement.GetProperty("type").GetString());
        Assert.Equal(expectedTitle, problem.RootElement.GetProperty("title").GetString());
        Assert.Equal(expectedStatus, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(context.Request.Path, problem.RootElement.GetProperty("instance").GetString());

        var detail = problem.RootElement.GetProperty("detail").GetString();
        Assert.DoesNotContain("stackTrace", problem.RootElement.ToString());
        if (expectedStatus == 500)
        {
            Assert.Equal("An unexpected error occurred.", detail);
            Assert.DoesNotContain("Sensitive internal detail", problem.RootElement.ToString());
        }
    }
}

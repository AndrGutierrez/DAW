using System.Text.Json;
using Core.Application.Management;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Presentation.API.Middleware;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var statusCode = exception switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                ConflictException => StatusCodes.Status409Conflict,
                Core.Application.Operations.ExchangeRateUnavailableException => StatusCodes.Status503ServiceUnavailable,
                ValidationException => StatusCodes.Status400BadRequest,
                InvalidOperationException or ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled request exception");
            }
            else
            {
                logger.LogWarning(exception, "Request rejected with status {StatusCode}", statusCode);
            }

            var problem = new ProblemDetails
            {
                Type = "about:blank",
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Status = statusCode,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Message,
                Instance = context.Request.Path
            };

            if (exception is ValidationException validation)
                problem.Extensions["errors"] = validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                problem,
                JsonOptions,
                context.RequestAborted);
        }
    }
}

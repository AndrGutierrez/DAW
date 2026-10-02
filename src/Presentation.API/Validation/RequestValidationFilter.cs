using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Presentation.API.Validation;

// Authorization filters run first; validation always supports async rules.
public sealed class RequestValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator)
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                if (!result.IsValid) throw new ValidationException(result.Errors);
            }
        }
        await next();
    }
}

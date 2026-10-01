using Core.Application.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute(string codename) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        var permissionChecker = context.HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();
        var userId = currentUser.UserId;

        if (userId is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!await permissionChecker.HasPermissionAsync(userId.Value, codename))
        {
            context.Result = new ForbidResult();
        }
    }
}

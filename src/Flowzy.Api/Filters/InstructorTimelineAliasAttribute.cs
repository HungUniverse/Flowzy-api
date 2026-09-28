using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Flowzy.Api.Filters;

// Spring protects /api/instructor/** before MVC, while /api/course-milestones/** only requires authentication.
public sealed class InstructorTimelineAliasAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/instructor") &&
            context.HttpContext.User.Identity?.IsAuthenticated == true && !context.HttpContext.User.IsInRole("INSTRUCTOR"))
            context.Result = new ForbidResult();
    }
}

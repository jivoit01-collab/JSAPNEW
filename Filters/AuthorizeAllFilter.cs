using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace JSAPNEW.Filters
{
    public class AuthorizeAllFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .OfType<AllowAnonymousAttribute>()
                .Any();

            if (hasAllowAnonymous)
                return;

            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult(
                    new { success = false, message = "Authentication required. Please log in." });
            }
        }
    }
}

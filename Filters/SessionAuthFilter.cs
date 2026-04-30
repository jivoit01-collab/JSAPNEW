using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Filters
{
    public class SessionAuthFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
                .Any();
            if (hasAllowAnonymous) return;

            if (context.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor)
                return;

            if (!descriptor.ControllerTypeInfo.IsSubclassOf(typeof(Microsoft.AspNetCore.Mvc.Controller)))
                return;

            var userId = context.HttpContext.Session.GetInt32("userId");
            if (!userId.HasValue || userId.Value <= 0)
            {
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
                }
                else
                {
                    context.Result = new RedirectResult("/Login");
                }
            }
        }
    }
}

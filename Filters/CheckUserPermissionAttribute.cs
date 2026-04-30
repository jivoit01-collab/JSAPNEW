using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace JSAPNEW.Filters
{
    public class CheckUserPermissionAttribute : ActionFilterAttribute
    {
        private readonly string _moduleName;
        private readonly string _permissionType;

        public CheckUserPermissionAttribute(string moduleName, string permissionType)
        {
            _moduleName = moduleName;
            _permissionType = permissionType;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var companyIdClaim = context.HttpContext.Session.GetInt32("companyId");

            if (string.IsNullOrWhiteSpace(userIdClaim) || companyIdClaim == null)
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Session expired or missing. Please log in again." });
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var permissionResponse = await permissionService.CheckUserPermissionAsync(new Models.UserPermissionRequest
            {
                UserId = int.Parse(userIdClaim),
                CompanyId = companyIdClaim.Value,
                ModuleName = _moduleName,
                PermissionType = _permissionType
            });

            if (permissionResponse == null || !permissionResponse.HasPermission)
            {
                context.Result = new ForbidResult("You do not have permission to access this resource.");
                return;
            }

            await next();
        }
    }
}

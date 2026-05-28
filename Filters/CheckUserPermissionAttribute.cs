using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

public class CheckUserPermissionAttribute : ActionFilterAttribute
{
    private readonly string _moduleName;
    private readonly string _permissionType;

    public CheckUserPermissionAttribute(string moduleName, string permissionType)
    {
        _moduleName = moduleName;
        _permissionType = permissionType;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var httpContext = context.HttpContext;

        // Read userId and companyId from session
        var userId = httpContext.Session.GetInt32("userId");
        var companyId = httpContext.Session.GetInt32("companyId");

        if (userId == null || companyId == null)
        {
            context.Result = new UnauthorizedObjectResult(new { success = false, message = "Session expired or missing. Please log in again." });
            return;
        }

        // Resolve permission service from DI
        var permissionService = (IPermissionService)httpContext.RequestServices.GetService(typeof(IPermissionService));

        if (permissionService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var permissionResponse = permissionService.CheckUserPermissionAsync(new UserPermissionRequest
        {
            UserId = userId.Value,
            CompanyId = companyId.Value,
            ModuleName = _moduleName,
            PermissionType = _permissionType
        }).Result;

        if (permissionResponse == null || !permissionResponse.HasPermission)
        {
            context.Result = new ForbidResult("You do not have permission to access this resource.");
        }
    }
}

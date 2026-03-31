namespace JSAPNEW.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
    public class PermissionModels
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }
    public class UserGroupAssignmentModel
    {
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public int GroupId { get; set; }
    }
    public class UserGroupViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
    }
    public class UserGroupMemberViewModel
    {
        public int UserId { get; set; }
        public string LoginUser { get; set; }
        public string UserEmail { get; set; }
        public DateTime CreatedOn { get; set; }
    }
    public class PermissionRequest
    {
        public int GroupId { get; set; }
        public string PermissionId { get; set; }
    }
    public class RemovePermissionRequest
    {
        public int GroupId { get; set; }
        public int PermissionId { get; set; }
    }
    public class GroupPermissionViewModel
    {
        public int PermissionId { get; set; }
        public string ModuleName { get; set; }
        public string PermissionType { get; set; }
    }
    public class ModulePermissionViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int PermissionId { get; set; }
        public string PermissionType { get; set; }
    }
    public class UserPermissionRequest
    {
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public string ModuleName { get; set; }
        public string PermissionType { get; set; }
    }

    public class UserPermissionResponse
    {
        public bool HasPermission { get; set; }
    }

    public class UserEffectivePermissionResponse
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int PermissionId { get; set; }
        public string PermissionType { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
    }
    public class CreateModuleRequest
    {
        public string ModuleName { get; set; }
        public string? Description { get; set; }
    }
    public class CreateGroupRequest
    {
        public string GroupName { get; set; }
        public string Description { get; set; }
    }
    public class ModuleResponseModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string Description { get; set; }
    }
    public class PermissionResponseModel
    {
        public int PermissionId { get; set; }
        public string PermissionType { get; set; }
    }
    public class CreatePermissionRequest
    {
        public string permissionType { get; set; }
        public int moduleId { get; set; }
    }
    public class AllPermissionModel
    {
        public int permissionId { get; set; }
        public int moduleId { get; set; }
        public string moduleName { get; set; }
        public string permissionType { get; set; }
        public string modulePermission { get; set; }
    }
}

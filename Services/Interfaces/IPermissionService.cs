using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<PermissionModels> AddUserToGroupAsync(UserGroupAssignmentModel model);
        Task<PermissionModels> RemoveUserFromGroupAsync(UserGroupAssignmentModel model);
        Task<List<UserGroupViewModel>> GetUserGroupsByCompanyAsync(int userId, int companyId);
        Task<List<UserGroupMemberViewModel>> GetUsersByGroupAndCompanyAsync(int groupId, int companyId);
        Task<PermissionModels> AssignPermissionToGroupAsync(PermissionRequest request);
        Task<PermissionModels> RemovePermissionFromGroupAsync(RemovePermissionRequest request);
        Task<List<GroupPermissionViewModel>> GetPermissionsByGroupAsync(int groupId);
        Task<List<ModulePermissionViewModel>> GetModulesAndPermissionsByGroupAsync(int groupId);
        Task<UserPermissionResponse> CheckUserPermissionAsync(UserPermissionRequest request);
        Task<List<UserEffectivePermissionResponse>> GetUserEffectivePermissionsAsync(int userId, int companyId);
        Task<PermissionModels> CreateModuleWithPermissionsAsync(CreateModuleRequest request);
        Task<PermissionModels> CreateGroupAsync(CreateGroupRequest request);
        Task<List<UserGroupViewModel>> GetAllGroupsAsync();
        Task<List<ModuleResponseModel>> GetAllModulesAsync();
        Task<List<PermissionResponseModel>> GetPermissionsByModuleAsync(int moduleId);
        Task<IEnumerable<PermissionModels>> CreatePermissionAsync(CreatePermissionRequest request);
        Task<IEnumerable<AllPermissionModel>> GetAllPermissionAsync();
    }
}

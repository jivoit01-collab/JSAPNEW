using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IPermissionService _permissionService; //An interface for bom-related operations

        private readonly ILogger<PermissionController> _permissionlogger; //for recording events or errors
        public PermissionController(IConfiguration configuration, IPermissionService PermissionService, ILogger<PermissionController> permissionlogger)
        {
            _configuration = configuration;
            _permissionService = PermissionService;
            _permissionlogger = permissionlogger;
        }

        [HttpPost("AddUserGroup")]
        public async Task<ActionResult<PermissionModels>> AddUserToGroup([FromBody] UserGroupAssignmentModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PermissionModels
                {
                    Success = false,
                    Message = "Invalid input"
                });
            }

            var result = await _permissionService.AddUserToGroupAsync(model);
            return Ok(result);
        }

        [HttpPost("RemoveUserGroup")]
        public async Task<ActionResult<PermissionModels>> RemoveUserFromGroup([FromBody] UserGroupAssignmentModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PermissionModels
                {
                    Success = false,
                    Message = "Invalid input"
                });
            }

            var result = await _permissionService.RemoveUserFromGroupAsync(model);
            return Ok(result);
        }

        [HttpGet("GetUserGroup")]
        public async Task<ActionResult<List<UserGroupViewModel>>> GetUserGroups([FromQuery] int userId, [FromQuery] int companyId)
        {
            if (userId <= 0 || companyId <= 0)
                return BadRequest(new ApiResponse<List<UserGroupViewModel>>
                {
                    Success = false,
                    Message = "Invalid user or company ID.",
                    Data = null
                });

            var groups = await _permissionService.GetUserGroupsByCompanyAsync(userId, companyId);
            return Ok(new ApiResponse<List<UserGroupViewModel>>
            {
                Success = true,
                Message = "Groups retrieved successfully.",
                Data = groups
            });
        }

        [HttpGet("GetUsersByGroupAndCompany")]
        public async Task<ActionResult<List<UserGroupMemberViewModel>>> GetUsersByGroupAndCompany([FromQuery] int groupId, [FromQuery] int companyId)
        {
            if (groupId <= 0 || companyId <= 0)
                return BadRequest(new ApiResponse<List<UserGroupMemberViewModel>>
                {
                    Success = false,
                    Message = "Invalid group or company ID.",
                    Data = null
                });

            var result = await _permissionService.GetUsersByGroupAndCompanyAsync(groupId, companyId);
            return Ok(new ApiResponse<List<UserGroupMemberViewModel>>
            {
                Success = true,
                Message = "Users retrieved successfully.",
                Data = result
            });
        }

        [HttpPost("AddPermissionToGroup")]
        public async Task<ActionResult<PermissionModels>> AssignPermission([FromBody] PermissionRequest request)
        {
            if (request == null)
            {
                return BadRequest(new PermissionModels { Success = false, Message = "Request cannot be null." });
            }

            if (request.GroupId <= 0)
            {
                return BadRequest(new PermissionModels { Success = false, Message = "Invalid Group ID." });
            }

            if (string.IsNullOrWhiteSpace(request.PermissionId))
            {
                return BadRequest(new PermissionModels { Success = false, Message = "Permission ID cannot be empty." });
            }

            // Optional: Validate comma-separated IDs are numeric
            var ids = request.PermissionId.Split(',');
            if (ids.Any(id => !int.TryParse(id.Trim(), out _)))
            {
                return BadRequest(new PermissionModels { Success = false, Message = "Permission ID(s) must be valid numeric values." });
            }

            var result = await _permissionService.AssignPermissionToGroupAsync(request);
            return Ok(result);
        }


        [HttpPost("RemovePermissionGroup")]
        public async Task<ActionResult<PermissionModels>> RemovePermission([FromBody] RemovePermissionRequest request)
        {
            if (request.GroupId <= 0 || request.PermissionId <= 0)
                return BadRequest(new PermissionModels { Success = false, Message = "Invalid group or permission ID." });

            var result = await _permissionService.RemovePermissionFromGroupAsync(request);
            return Ok(result);
        }

        [HttpGet("GetPermissionsByGroup")]
        public async Task<ActionResult<List<GroupPermissionViewModel>>> GetPermissionsByGroup(int groupId)
        {
            if (groupId <= 0)
                return BadRequest(new ApiResponse<List<GroupPermissionViewModel>>
                {
                    Success = false,
                    Message = "Invalid group ID.",
                    Data = null
                });

            var result = await _permissionService.GetPermissionsByGroupAsync(groupId);
            return Ok(new ApiResponse<List<GroupPermissionViewModel>>
            {
                Success = true,
                Message = "Permissions By Group Data retrieved successfully.",
                Data = result
            });

        }

        [HttpGet("GetModulesAndPermissionsByGroup")]
        public async Task<ActionResult<List<ModulePermissionViewModel>>> GetModulesAndPermissionsByGroup(int groupId)
        {
            if (groupId <= 0)
                return BadRequest(new ApiResponse<List<ModulePermissionViewModel>>
                {
                    Success = false,
                    Message = "Invalid group ID.",
                    Data = null
                });

            var result = await _permissionService.GetModulesAndPermissionsByGroupAsync(groupId);
            return Ok(new ApiResponse<List<ModulePermissionViewModel>>
            {
                Success = true,
                Message = "Permissions By Group Data retrieved successfully.",
                Data = result
            });
        }

        [HttpPost("CheckUserPermission")]
        public async Task<IActionResult> CheckUserPermission([FromBody] UserPermissionRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request.");

            var response = await _permissionService.CheckUserPermissionAsync(request);
            return Ok(response);
        }

        [HttpGet("GetUserEffectivePermissions")]
        public async Task<ActionResult<ApiResponse<object>>> GetUserEffectivePermissions([FromQuery] int userId, [FromQuery] int companyId)
        {
            if (userId <= 0 || companyId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid user or company ID.",
                    Data = null
                });
            }

            var result = await _permissionService.GetUserEffectivePermissionsAsync(userId, companyId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Effective permissions retrieved successfully.",
                Data = result
            });
        }


        [HttpPost("CreateModuleWithPermissions")]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ModuleName))
            {
                return Ok(new PermissionModels
                {
                    Success = false,
                    Message = "Module name is required."
                });
            }

            var result = await _permissionService.CreateModuleWithPermissionsAsync(request);

            return Ok(result); // Always returns a PermissionModels object
        }

        [HttpPost("CreateGroup")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GroupName))
                return BadRequest(new PermissionModels
                {
                    Success = false,
                    Message = "Group name is required."
                });

            var result = await _permissionService.CreateGroupAsync(request);
            return Ok(result);
        }

        [HttpGet("GetAllGroups")]
        public async Task<IActionResult> GetAllGroups()
        {
            try
            {
                var groups = await _permissionService.GetAllGroupsAsync();

                if (groups == null || !groups.Any())
                {
                    return Ok(new
                    {
                        success = false,
                        message = "No groups found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Groups fetched successfully.",
                    data = groups
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error while fetching groups: " + ex.Message
                });
            }
        }

        [HttpGet("GetAllModules")]
        public async Task<IActionResult> GetAllModules()
        {
            try
            {
                var modules = await _permissionService.GetAllModulesAsync();

                if (modules == null || !modules.Any())
                {
                    return Ok(new
                    {
                        success = false,
                        message = "No modules found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Modules fetched successfully.",
                    data = modules
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error while fetching modules: " + ex.Message
                });
            }
        }

        [HttpGet("GetPermissionsByModule")]
        public async Task<IActionResult> GetPermissionsByModule(int moduleId)
        {
            try
            {
                var permissions = await _permissionService.GetPermissionsByModuleAsync(moduleId);

                if (permissions == null || !permissions.Any())
                {
                    return Ok(new
                    {
                        success = false,
                        message = "No permissions found for the specified module."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Permissions fetched successfully.",
                    data = permissions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching permissions: " + ex.Message
                });
            }
        }

        [HttpPost("CreatePermission")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.permissionType))
            {
                return BadRequest(new PermissionModels
                {
                    Success = false,
                    Message = "Invalid permission type."
                });
            }
            try
            {
                var result = await _permissionService.CreatePermissionAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PermissionModels
                {
                    Success = false,
                    Message = "Error creating permission: " + ex.Message
                });
            }
        }


        [HttpGet("GetAllPermissions")]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionAsync();
                if (permissions == null || !permissions.Any())
                {
                    return Ok(new
                    {
                        success = false,
                        message = "No permissions found."
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "Permissions fetched successfully.",
                    data = permissions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error while fetching permissions: " + ex.Message
                });
            }
        }
    }
}

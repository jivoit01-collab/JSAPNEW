using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JSAPNEW.Services.Implementation
{
    public class PermissionService : IPermissionService
    {
        private readonly IConfiguration _configuration;
        public PermissionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<PermissionModels> AddUserToGroupAsync(UserGroupAssignmentModel model)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                var parameters = new DynamicParameters();
                parameters.Add("@userId", model.UserId);
                parameters.Add("@companyId", model.CompanyId);
                parameters.Add("@groupId", model.GroupId);

                await connection.ExecuteAsync("dbo.jsAddUserToGroup", parameters, commandType: CommandType.StoredProcedure);

                return new PermissionModels
                {
                    Success = true,
                    Message = "User successfully added to the group."
                };
            }
            catch (SqlException ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }
        public async Task<PermissionModels> RemoveUserFromGroupAsync(UserGroupAssignmentModel model)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                var parameters = new DynamicParameters();
                parameters.Add("@userId", model.UserId);
                parameters.Add("@companyId", model.CompanyId);
                parameters.Add("@groupId", model.GroupId);

                await connection.ExecuteAsync("dbo.jsRemoveUserFromGroup", parameters, commandType: CommandType.StoredProcedure);

                return new PermissionModels
                {
                    Success = true,
                    Message = "User successfully removed from the group."
                };
            }
            catch (SqlException ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }
        public async Task<List<UserGroupViewModel>> GetUserGroupsByCompanyAsync(int userId, int companyId)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);

                var result = await connection.QueryAsync<UserGroupViewModel>(
                    "[dbo].[jsGetUserGroupsByCompany]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch
            {
                return new List<UserGroupViewModel>(); // return empty list on error
            }
        }
        public async Task<List<UserGroupMemberViewModel>> GetUsersByGroupAndCompanyAsync(int groupId, int companyId)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                var parameters = new DynamicParameters();
                parameters.Add("@groupId", groupId);
                parameters.Add("@companyId", companyId);

                var users = await connection.QueryAsync<UserGroupMemberViewModel>(
                    "dbo.jsGetUsersByGroupAndCompany",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return users.ToList();
            }
            catch
            {
                return new List<UserGroupMemberViewModel>();
            }
        }
        public async Task<PermissionModels> AssignPermissionToGroupAsync(PermissionRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                var parameters = new DynamicParameters();
                parameters.Add("@groupId", request.GroupId);
                parameters.Add("@permissionId", request.PermissionId);

                await connection.ExecuteAsync("dbo.jsAssignPermissionToGroup", parameters, commandType: CommandType.StoredProcedure);

                return new PermissionModels
                {
                    Success = true,
                    Message = "Permission assigned successfully."
                };
            }
            catch (SqlException ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = "Unexpected error: " + ex.Message
                };
            }
        }
        public async Task<PermissionModels> RemovePermissionFromGroupAsync(RemovePermissionRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                var parameters = new DynamicParameters();
                parameters.Add("@groupId", request.GroupId);
                parameters.Add("@permissionId", request.PermissionId);

                await connection.ExecuteAsync("dbo.jsRemovePermissionFromGroup", parameters, commandType: CommandType.StoredProcedure);

                return new PermissionModels
                {
                    Success = true,
                    Message = "Permission removed successfully."
                };
            }
            catch (SqlException ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new PermissionModels
                {
                    Success = false,
                    Message = "Unexpected error: " + ex.Message
                };
            }
        }
        public async Task<List<GroupPermissionViewModel>> GetPermissionsByGroupAsync(int groupId)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            var parameters = new DynamicParameters();
            parameters.Add("@groupId", groupId);

            var result = await connection.QueryAsync<GroupPermissionViewModel>(
                "dbo.jsGetPermissionsByGroup",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }
        public async Task<List<ModulePermissionViewModel>> GetModulesAndPermissionsByGroupAsync(int groupId)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            var parameters = new DynamicParameters();
            parameters.Add("@groupId", groupId);

            var result = await connection.QueryAsync<ModulePermissionViewModel>(
                "dbo.jsGetModulesAndPermissionsByGroup",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }
        public async Task<UserPermissionResponse> CheckUserPermissionAsync(UserPermissionRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("jsCheckUserPermission", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@userId", request.UserId);
                command.Parameters.AddWithValue("@companyId", request.CompanyId);
                command.Parameters.AddWithValue("@moduleName", request.ModuleName);
                command.Parameters.AddWithValue("@permissionType", request.PermissionType);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                return new UserPermissionResponse
                {
                    HasPermission = Convert.ToInt32(result) == 1
                };
            }
        }
        public async Task<List<UserEffectivePermissionResponse>> GetUserEffectivePermissionsAsync(int userId, int companyId)
        {
            var permissions = new List<UserEffectivePermissionResponse>();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("jsGetUserEffectivePermissions", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@companyId", companyId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permissions.Add(new UserEffectivePermissionResponse
                        {
                            ModuleId = reader.GetInt32(reader.GetOrdinal("moduleId")),
                            ModuleName = reader.GetString(reader.GetOrdinal("moduleName")),
                            PermissionId = reader.GetInt32(reader.GetOrdinal("permissionId")),
                            PermissionType = reader.GetString(reader.GetOrdinal("permissionType")),
                            GroupId = reader.GetInt32(reader.GetOrdinal("groupId")),
                            GroupName = reader.GetString(reader.GetOrdinal("groupName"))
                        });
                    }
                }
            }

            return permissions;
        }
        public async Task<PermissionModels> CreateModuleWithPermissionsAsync(CreateModuleRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("jsCreateModuleWithPermissions", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@moduleName", request.ModuleName);
                command.Parameters.AddWithValue("@description", string.IsNullOrEmpty(request.Description) ? (object)DBNull.Value : request.Description);

                try
                {
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    return new PermissionModels
                    {
                        Success = true,
                        Message = "Module created successfully with default permissions."
                    };
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 50000 && ex.Message.Contains("Module already exists."))
                    {
                        return new PermissionModels
                        {
                            Success = false,
                            Message = "Module already exists."
                        };
                    }

                    return new PermissionModels
                    {
                        Success = false,
                        Message = "An error occurred: " + ex.Message
                    };
                }
            }
        }
        public async Task<PermissionModels> CreateGroupAsync(CreateGroupRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("jsCreateGroup", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@groupName", request.GroupName);
                command.Parameters.AddWithValue("@description", request.Description ?? (object)DBNull.Value);

                try
                {
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    return new PermissionModels
                    {
                        Success = true,
                        Message = "Group created successfully."
                    };
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 50000 && ex.Message.Contains("Group name already exists."))
                    {
                        return new PermissionModels
                        {
                            Success = false,
                            Message = "Group name already exists."
                        };
                    }

                    return new PermissionModels
                    {
                        Success = false,
                        Message = "An error occurred: " + ex.Message
                    };
                }
            }
        }
        public async Task<List<UserGroupViewModel>> GetAllGroupsAsync()
        {
            var groups = new List<UserGroupViewModel>();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("jsGetAllGroups", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        groups.Add(new UserGroupViewModel
                        {
                            GroupId = reader.GetInt32(reader.GetOrdinal("groupId")),
                            GroupName = reader.GetString(reader.GetOrdinal("groupName")),
                            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("description"))
                        });
                    }
                }
            }

            return groups;
        }
        public async Task<List<ModuleResponseModel>> GetAllModulesAsync()
        {
            var modules = new List<ModuleResponseModel>();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("jsGetAllModules", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        modules.Add(new ModuleResponseModel
                        {
                            ModuleId = reader.GetInt32(reader.GetOrdinal("moduleId")),
                            ModuleName = reader.GetString(reader.GetOrdinal("moduleName")),
                            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("description"))
                        });
                    }
                }
            }

            return modules;
        }
        public async Task<List<PermissionResponseModel>> GetPermissionsByModuleAsync(int moduleId)
        {
            var permissions = new List<PermissionResponseModel>();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("jsGetPermissionsByModule", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@moduleId", moduleId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permissions.Add(new PermissionResponseModel
                        {
                            PermissionId = reader.GetInt32(reader.GetOrdinal("permissionId")),
                            PermissionType = reader.GetString(reader.GetOrdinal("permissionType"))
                        });
                    }
                }
            }

            return permissions;
        }

        public async Task<IEnumerable<PermissionModels>> CreatePermissionAsync(CreatePermissionRequest request)
        {
            var permissions = new List<PermissionModels>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("[dbo].[jsCreatePermission]", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@moduleId", request.moduleId);
                command.Parameters.AddWithValue("@permissionType", request.permissionType);
                try
                {
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    permissions.Add(new PermissionModels
                    {
                        Success = true,
                        Message = "Permission created successfully."
                    });
                }
                catch (SqlException ex)
                {
                    permissions.Add(new PermissionModels
                    {
                        Success = false,
                        Message = "An error occurred: " + ex.Message
                    });
                }
            }
            return permissions;
        }

        public async Task<IEnumerable<AllPermissionModel>> GetAllPermissionAsync()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            var result = await connection.QueryAsync<AllPermissionModel>(
                "[dbo].[jsGetAllPermission]",
                commandType: CommandType.StoredProcedure
            );
            return result;
        }
    }
}

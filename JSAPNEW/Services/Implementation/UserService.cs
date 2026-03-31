using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using ServiceStack.Web;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;


namespace JSAPNEW.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _configuration;
        //private readonly Interfaces.ITokenService _tokenService;
        private readonly string _connectionString;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserService> _logger;

        public UserService(IConfiguration configuration, Interfaces.ITokenService tokenService, INotificationService notificationService, ILogger<UserService> logger)
        {
            _configuration = configuration;
            // _tokenService = tokenService;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _notificationService = notificationService;
            _logger = logger;
        }


        public async Task<LoginResponse> ValidateUserAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.loginUser) || string.IsNullOrWhiteSpace(request.password))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Username and password must be provided"
                };
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string EncryptedPassword = Encryption.Encrypt(request.password);

                    var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                        "jsCheckLogin",
                        new { loginUser = request.loginUser, password = EncryptedPassword },
                        commandType: CommandType.StoredProcedure
                    );

                    if (user == null)
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Message = "Invalid username or password"
                        };
                    }

                    return new LoginResponse
                    {
                        Success = true,
                        Message = "Login successful",
                        User = user
                    };
                }
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred. Please try again."
                };
            }
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var user = await connection.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM jsUser WHERE UserId = @UserId",
                    new { UserId = userId }
                );

                return user == null ? null : new UserDto
                {
                    userId = user.UserId,
                    loginUser = user.LoginUser
                };
            }
        }
        public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    Message = "Current and new passwords are required"
                };
            }
            if (!await ValidateCurrentPasswordAsync(request.userId, request.CurrentPassword))
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    Message = "Current password is incorrect"
                };
            }
            if (request.CurrentPassword == request.NewPassword)
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    Message = "New password must be different from current password"
                };
            }
            string encryptedNewPassword = Encryption.Encrypt(request.NewPassword);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@updatedBy", request.updatedBy);
                parameters.Add("@newPassword", encryptedNewPassword);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsResetPassword", parameters, commandType: CommandType.StoredProcedure);

                return new ChangePasswordResponse
                {
                    Success = true,
                    Message = "Password updated successfully"
                };
            }
        }

        public async Task<ChangePasswordResponse> ChangePasswordAsync2(ChangePasswordRequest2 request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    Message = "new password are required"
                };
            }

            string encryptedNewPassword = Encryption.Encrypt(request.NewPassword);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@updatedBy", request.updatedBy);
                parameters.Add("@newPassword", encryptedNewPassword);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsResetPassword", parameters, commandType: CommandType.StoredProcedure);

                return new ChangePasswordResponse
                {
                    Success = true,
                    Message = "Password updated successfully"
                };
            }
        }
        public async Task<bool> ValidateCurrentPasswordAsync(int userId, string currentPassword)
        {
            // SQL query with proper parameter to avoid SQL injection
            var sqlQuery = "SELECT Password FROM JSUser WHERE UserId = @UserId";

            using (var connection = new SqlConnection(_connectionString))
            {
                // Get encrypted password from database
                var encryptedPassword = await connection.QueryFirstOrDefaultAsync<string>(
                    sqlQuery,
                    new { UserId = userId }
                );

                // Check if user exists and has a password
                if (string.IsNullOrEmpty(encryptedPassword))
                {
                    return false;
                }

                try
                {
                    // Decrypt the stored password
                    string decryptedPassword = Encryption.Decrypt(encryptedPassword);
                    // Compare the decrypted password with current password
                    return decryptedPassword.Equals(currentPassword, StringComparison.Ordinal);
                }
                catch (Exception)
                {
                    // Log error if needed
                    return false;
                }
            }
        }
        public async Task<int> RegisterUserAsync(UserRegistrationDTO userDTO)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Encrypt the password before saving it
                    string encryptedPassword = Encryption.Encrypt(userDTO.password);

                    var parameters = new DynamicParameters();
                    parameters.Add("@firstName", userDTO.firstName);
                    parameters.Add("@lastName", userDTO.lastName);
                    parameters.Add("@userPhoneNumber", userDTO.userPhoneNumber);
                    parameters.Add("@userEmail", userDTO.userEmail);
                    parameters.Add("@loginUser", userDTO.loginUser);
                    parameters.Add("@deptIds", userDTO.deptIds);
                    parameters.Add("@EmpId", userDTO.empId);
                    parameters.Add("@createdBy", userDTO.createdBy);
                    parameters.Add("@password", encryptedPassword);
                    parameters.Add("@doj", userDTO.doj);
                    parameters.Add("@statusCode", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    // Execute the stored procedure
                    await connection.ExecuteAsync("dbo.jsUserInfo", parameters, commandType: CommandType.StoredProcedure);

                    // Get the output status code
                    int statusCode = parameters.Get<int>("@statusCode");
                    return statusCode;
                }
            }
            catch (Exception ex)
            {
                // Log exception if needed
                Console.WriteLine(ex.Message);
                return 0; // Return 0 if an error occurred
            }
        }
        public async Task<IEnumerable<VarietyModel>> GetVarietyAsync(int company)
        {
            var mode = "variety";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<VarietyModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<EffMonthModel>> GeteffMonthAsync(int company)
        {
            var mode = "effMonth";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<EffMonthModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<BudgetModel>> GetBudgetAsync(int company)
        {
            var mode = "budget";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<SubBudgetModel>> GetSubBudgetAsync(int company)
        {
            var mode = "subBudget";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<SubBudgetModel>(
                    sqlQuery,
                   new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<StateModel>> GetStateAsync(int company)
        {
            var mode = "state";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<StateModel>(
                    sqlQuery,
                   new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<RoleModel>> GetRoleAsync(int company)
        {
            var mode = "role";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<RoleModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<BranchModel>> GetBranchAsync(int company)
        {
            var mode = "branch";
            var sqlQuery = "EXEC jsPostSapTables @mode,  @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BranchModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<DepartmentModel>> GetDepartmentAsync(int company)
        {
            var mode = "department";
            var sqlQuery = "EXEC jsPostSapTables @mode ,@company";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<DepartmentModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<ReportModel>> GetReportAsync(int company)
        {
            var mode = "report";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ReportModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<GetAllUserModel>> GetUserAsync(int company)
        {

            var sqlQuery = "EXEC jsfetchUser @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetAllUserModel>(
                    sqlQuery,
                    new { company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<ApprovalModel>> GetApprovalAsync(int company)
        {
            var mode = "approval";
            var sqlQuery = "EXEC jsPostSapTables @mode, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ApprovalModel>(
                    sqlQuery,
                    new { mode, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<CompanyModel>> GetCompanyAsync(int userId)
        {
            var sqlQuery = "EXEC jsfetchCompany @userId";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<CompanyModel>(
                    sqlQuery,
                     new { userId } // Parameters for the stored procedure
                );
            }
        }
        public async Task<int> AddStageAsync(AddStage StageData)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@stage", StageData.stage);
                    parameters.Add("@approvalId", StageData.approvalId);
                    parameters.Add("@rejectId", StageData.rejectId);
                    parameters.Add("@userIds", StageData.userIds);
                    parameters.Add("@createdBy", StageData.createdBy);
                    parameters.Add("@company", StageData.company);
                    parameters.Add("@description", StageData.description);

                    try
                    {
                        await connection.ExecuteAsync("dbo.jsAddStage", parameters, commandType: CommandType.StoredProcedure);
                        return 1; // Success
                    }
                    catch (SqlException ex)
                    {
                        // Check for the specific error number thrown by the stored procedure
                        if (ex.Number == 50004) // The error number from THROW 50004
                        {
                            return 0; // Already exists
                        }
                        throw; // Rethrow other SQL exceptions
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex.Message);
                return -1; // General failure
            }
        }
        public async Task<int> UserAssignPermissionAsync(AssignPermissionDetail Data)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@branchIds", Data.branchIds);
                    parameters.Add("@varietyIds", Data.varietyIds);
                    parameters.Add("@budgetIds", Data.budgetIds);
                    parameters.Add("@sBudgetIds", Data.sBudgetIds);
                    parameters.Add("@stateIds", Data.stateIds);
                    parameters.Add("@reportIds", Data.reportIds);
                    parameters.Add("@approvalIds", Data.approvalIds);
                    parameters.Add("@fromDate", Data.fromDate);
                    parameters.Add("@toDate", Data.toDate);
                    parameters.Add("@userId", Data.userId);
                    parameters.Add("@company", Data.company);
                    parameters.Add("@roleId", Data.roleId);
                    parameters.Add("@adminId", Data.adminId);
                    parameters.Add("@statusCode", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    // Execute the stored procedure
                    await connection.ExecuteAsync("dbo.jsUserDetails", parameters, commandType: CommandType.StoredProcedure);

                    // Get the output status code
                    int statusCode = parameters.Get<int>("@statusCode");
                    return statusCode;
                }
            }
            catch (Exception ex)
            {
                // Log exception if needed
                Console.WriteLine(ex.Message);
                return 0; // Return 0 if an error occurred
            }
        }
        public async Task<IEnumerable<StageModel>> GetStageAsync(int company)
        {
            var sqlQuery = "EXEC jsGetStage @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<StageModel>(
                    sqlQuery,
                    new { company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<GetAllUserModel>> GetAllUserAsync(int company)
        {
            var sqlQuery = "EXEC jsGetAllUser @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetAllUserModel>(
                    sqlQuery,
                    new { company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<GetAllUserModel>> GetSelectedUserAsync(int company, int userId)
        {
            var sqlQuery = "EXEC jsGetSelectedUser @company,@userId";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetAllUserModel>(
                    sqlQuery,
                    new { company, userId } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<CountApprovalModel>> GetCountApprovalAsync()
        {
            var sqlQuery = "EXEC jsCountApproval ";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<CountApprovalModel>(
                    sqlQuery // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<CountRejectionModel>> GetCountRejectionAsync()
        {
            var sqlQuery = "EXEC jsCountRejection";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<CountRejectionModel>(
                    sqlQuery // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<GetAllUserModel>> GetUserNotRegisterInCompanyAsync(int company)
        {
            var sqlQuery = "EXEC jsGetUserNotRegisterInCompany @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetAllUserModel>(
                    sqlQuery,
                    new { company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<bool> UpdateUserStatus(UserStatusUpdateModel model)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand("jsChangeProfileStatus", connection))
                    {
                        int statusAsInt = model.Status ? 1 : 0;
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@userId", model.UserId);
                        command.Parameters.AddWithValue("@updatedBy", model.UpdatedBy);
                        command.Parameters.AddWithValue("@companyId", model.CompanyId);
                        command.Parameters.AddWithValue("@status", statusAsInt);

                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Log the exception here
                return false;
            }
        }
        public async Task<int> GetAddTemplateAsync(AddTemplateModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@template", request.template);
                parameters.Add("@createdBy", request.createdBy);
                parameters.Add("@stageIds", request.stageIds);
                parameters.Add("@priority", request.priority);
                parameters.Add("@approvalIds", request.approvalIds);
                parameters.Add("@company", request.company);
                parameters.Add("@queries", request.queries);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsAddTemplate", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<int> GetAddPageAsync(AddPageModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@pageName", request.pageName);
                parameters.Add("@pageUrl", request.pageUrl);
                parameters.Add("@createdBy", request.createdBy);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsAddPage", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<int> GetAddRoleAsync(AddRoleModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@role", request.role);
                parameters.Add("@company", request.company);
                parameters.Add("@createdBy", request.createdBy);
                parameters.Add("@description", request.description);
                parameters.Add("@pageIds", request.pageIds);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsAddRole", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }

        public async Task<IEnumerable<ActiveUserModel>> GetActiveUser()
        {
            var sqlQuery = "EXEC [dbo].[jsGetActiveUsers] ";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ActiveUserModel>(
                    sqlQuery
                );
            }
        }

        public async Task<IEnumerable<BudgetApprovalCounts>> GetAllBudgetApprovalCountsAsync(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetBudgetInsight] @userId,@company,@month";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetApprovalCounts>(
                    sqlQuery,
                    new { userId, company, month }
                );
            }
        }
        public async Task<int> UpdateBudgetAsync(int userId, int updatedBy, string budgetId, bool status, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                int statusAsInt = status ? 1 : 0;
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@updatedBy", updatedBy);
                parameters.Add("@budgetId", budgetId);
                parameters.Add("@status", statusAsInt);
                parameters.Add("@company", company);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsUpdateBudget", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<IEnumerable<QueryNameModel>> GetQueryNameAsync(string type, int company)
        {
            var sqlQuery = "EXEC jsGetQueryName @type, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<QueryNameModel>(
                    sqlQuery,
                    new { type, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<int> UpdateUserApprovalAsync(useraprrovalModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@approvalId", request.approvalId);
                parameters.Add("@company", request.company);
                parameters.Add("@view", request.view);
                parameters.Add("@create", request.create);
                parameters.Add("@update", request.update);
                parameters.Add("@delete", request.delete);
                parameters.Add("@status", request.status);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("updateUserApproval", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<int> UpdateStateAsync(int userId, int updatedBy, string stateId, bool status, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                int statusAsInt = status ? 1 : 0;
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@updatedBy", updatedBy);
                parameters.Add("@stateId", stateId);
                parameters.Add("@status", statusAsInt);
                parameters.Add("@company", company);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsUpdateState", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<int> UpdateSubBudgetAsync(int userId, int updatedBy, string subBudgetId, bool status, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                int statusAsInt = status ? 1 : 0;
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@updatedBy", updatedBy);
                parameters.Add("@subBudgetId", subBudgetId);
                parameters.Add("@status", statusAsInt);
                parameters.Add("@company", company);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsUpdateSubBudget", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<int> UpdateFromToDateAsync(int userId, int updatedBy, DateTime toDate, DateTime fromDate, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {

                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@updatedBy", updatedBy);
                parameters.Add("@toDate", toDate);
                parameters.Add("@fromDate", fromDate);
                parameters.Add("@company", company);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsUpdateFromToDate", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<int> UpdateBranchAsync(int userId, int updatedBy, string branchId, bool status, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                int statusAsInt = status ? 1 : 0;
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@updatedBy", updatedBy);
                parameters.Add("@branchId", branchId);
                parameters.Add("@status", statusAsInt);
                parameters.Add("@company", company);


                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsUpdateBranch", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }

        public async Task<int> UpdateReportAsync(int userId, int updatedBy, string reportId, bool status, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                int statusAsInt = status ? 1 : 0;
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@updatedBy", updatedBy);
                parameters.Add("@reportId", reportId);
                parameters.Add("@status", statusAsInt);
                parameters.Add("@company", company);


                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsUpdateReport", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }

        public async Task<int> UpdateVarietyAsync(int userId, int updatedBy, string varietyId, bool status, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                int statusAsInt = status ? 1 : 0;
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@updatedBy", updatedBy);
                parameters.Add("@varietyId", varietyId);
                parameters.Add("@status", statusAsInt);
                parameters.Add("@company", company);


                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsUpdateVariety", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        
        public async Task<int> UpdateUserRoleAsync(int userId, int roleId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@roleId", roleId);
                parameters.Add("@company", company);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("updateUserRole", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }

        public async Task<IEnumerable<RejectedBudgetModel>> GetRejectedBudgetWithDetailsAsync2(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetRejectedBudgets] @userId, @company ,@month";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<RejectedBudgetModel>(
                    sqlQuery,
                   new { userId, company, month = string.IsNullOrEmpty(month) ? null : month } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<PendingBudgetModel>> GetPendingBudgetWithDetailsAsync2(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetPendingBudgets] @userId, @company, @month";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PendingBudgetModel>(
                    sqlQuery,
                   new { userId, company, month } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<ApproveBudgetModel>> GetApprovedBudgetWithDetailsAsync2(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetApprovedBudgets] @userId, @company,@month";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ApproveBudgetModel>(
                    sqlQuery,
                   new { userId, company, month = string.IsNullOrEmpty(month) ? null : month } // Parameters for the stored procedure
                );
            }
        }
            
        public async Task<IEnumerable<PendingBudgetModel>> GetPendingBudgetWithDetailsAsync(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetPendingBudgets] @userId, @company, @month";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PendingBudgetModel>(
                    sqlQuery,
                   new { userId, company, month } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<ApproveBudgetModel>> GetApprovedBudgetWithDetailsAsync(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetApprovedBudgets] @userId, @company,@month";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ApproveBudgetModel>(
                    sqlQuery,
                   new { userId, company, month } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<NextApproverModel>> GetNextApproverAsync(int budgetId)
        {
            var sqlQuery = "EXEC [bud].[jsGetNextApprover] @budgetId";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<NextApproverModel>(
                    sqlQuery,
                    new { budgetId }
                );
            }
        }
        public async Task<IEnumerable<RejectedBudgetModel>> GetRejectedBudgetWithDetailsAsync(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetRejectedBudgets] @userId, @company ,@month";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<RejectedBudgetModel>(
                    sqlQuery,
                   new { userId, company, month } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<AllBudgetRequestsModel>> GetAllBudgetWithDetailsAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Open the connection
                await connection.OpenAsync();

                // Fetch data from the stored procedures
                var pendingBudgets = await connection.QueryAsync<AllBudgetRequestsModel>(
                    "EXEC  [bud].[jsGetPendingBudgets] @userId, @company,@month",
                    new { userId, company, month });

                var approvedBudgets = await connection.QueryAsync<AllBudgetRequestsModel>(
                    "EXEC [bud].[jsGetApprovedBudgets] @userId, @company,@month",
                    new { userId, company, month });

                var rejectedBudgets = await connection.QueryAsync<AllBudgetRequestsModel>(
                    "EXEC [bud].[jsGetRejectedBudgets] @userId, @company,@month",
                    new { userId, company, month });

                // Add status to each record
                foreach (var budget in pendingBudgets)
                {
                    budget.Status = "Pending";
                }

                foreach (var budget in approvedBudgets)
                {
                    budget.Status = "Approved";
                }

                foreach (var budget in rejectedBudgets)
                {
                    budget.Status = "Rejected";
                }

                // Combine all records into one list
                var allBudgets = pendingBudgets.ToList();
                allBudgets.AddRange(approvedBudgets);
                allBudgets.AddRange(rejectedBudgets);

                return allBudgets;
            }
        }
        
        public async Task<IEnumerable<GetUserBudgetSummaryByTypeModel>> GetUserBudgetSummaryByTypeAsync(int userId, int company, string budgetType)
        {
            var sqlQuery = "EXEC jsGetUserBudgetSummaryByType @userId, @company , @budgetType";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetUserBudgetSummaryByTypeModel>(
                    sqlQuery,
                   new { userId, company, budgetType } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<DocEntryModel>> getDocEntryDataAsync(int docEntry, int userId, int company)
        {
            var sqlQuery = "EXEC getDocEntryData @docEntry,@userId,@company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<DocEntryModel>(
                    sqlQuery,
                   new { docEntry, userId, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<RemarksModel>> getRemarksDataAsync(int docEntry, int company)
        {
            var sqlQuery = "EXEC jsGetRemarksOfLastUser @docEntry, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<RemarksModel>(
                    sqlQuery,
                    new { docEntry, company }
                );
            }
        }

        public async Task<Response> RejectBudgetAsync(BudgetRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    int doc = 0;
                    if (request.docId == null)
                    {
                        doc = Convert.ToInt32(request.docIds);
                    }
                    else
                    {
                        doc = (int)request.docId;
                    }

                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("[bud].[jsRejectBudget]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@docId", doc);
                        cmd.Parameters.AddWithValue("@userId", request.userId);
                        cmd.Parameters.AddWithValue("@company", request.company);
                        cmd.Parameters.AddWithValue("@remarks", (object?)request.remarks ?? DBNull.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        return new Response { Success = true, Message = result?.ToString() ?? "Rejected completed." };
                    }
                }
            }
            catch (SqlException ex)
            {
                return new Response { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<object> GetAllPermissionsOfOneUserAsync(int userId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Open the connection
                await connection.OpenAsync();

                // Fetch data from the stored procedures
                var approvalsdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllApprovalsByUser @userId, @company",
                    new { userId, company });

                var branchsdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllBranchByUser @userId, @company",
                    new { userId, company });

                var budgetsdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllBudgetByUser @userId, @company",
                    new { userId, company });

                var datesdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllDatesByUser @userId, @company",
                    new { userId, company });

                var reportsdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllReportsByUser @userId, @company",
                    new { userId, company });

                var rolesdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllRoleByUser @userId, @company",
                    new { userId, company });

                var statesdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllStateByUser @userId, @company",
                    new { userId, company });

                var subbudgetsdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllSubBudgetByUser @userId, @company",
                    new { userId, company });

                var varietiesdata = await connection.QueryAsync<object>(
                    "EXEC jsGetAllSubGroupByUser @userId, @company",
                    new { userId, company });

                // Return all datasets in a structured object
                return new
                {
                    Approvals = approvalsdata.ToList(),
                    Branches = branchsdata.ToList(),
                    Budgets = budgetsdata.ToList(),
                    Dates = datesdata.ToList(),
                    Reports = reportsdata.ToList(),
                    Roles = rolesdata.ToList(),
                    States = statesdata.ToList(),
                    SubBudgets = subbudgetsdata.ToList(),
                    Varieties = varietiesdata.ToList()
                };
            }
        }
        public async Task<object> GetUserBudgetTypesAsync(int userId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {

                await connection.OpenAsync();
                var result = await connection.QueryAsync<object>("EXEC jsGetUserBudgetTypes @userId, @company", new { userId, company });

                return result; // This should return 1 on success
            }
        }
        public async Task<IEnumerable<OneDocEntry>> AmountOfOneDocEntryAsync(int docEntry, int userId, int company)
        {
            var sqlQuery = "EXEC getTotalAmountofSpecificDocEntry @docEntry,@userId, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<OneDocEntry>(
                    sqlQuery,
                   new { docEntry, userId, company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<string> ExecuteUserQueryAsync(string query)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("EXEC jsValidateQuery @Query", connection))
                {
                    command.Parameters.AddWithValue("@Query", query);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? "Query executed successfully.";
                }
            }
        }
        public async Task<int> GetAddQueryAsync(AddQueryModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@query", request.query);
                parameters.Add("@queryName", request.queryName);
                parameters.Add("@type", request.type);
                parameters.Add("@company", request.company);

                // Execute the stored procedure and get the result
                var result = await connection.ExecuteScalarAsync<int>("jsAddQuery", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<string> GetValidateQueryAsync(string query)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("EXEC jsValidateQuery @Query", connection))
                {
                    command.Parameters.AddWithValue("@Query", query);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? "Query executed successfully.";
                }
            }
        }
        public async Task<Response> ResetAdminPasswordAsync(AdminResetPasswordModel request)
        {
            var response = new Response
            {
                Success = false
            };

            if (request.userId <= 0)
            {
                response.Message = "Valid User ID is required";
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.newPassword))
            {
                return new Response { Success = false, Message = "New password is required." };
            }
            if (request.newPassword.Length < 6)
            {
                return new Response { Success = false, Message = "Password must be at least 6 characters long." };
            }
            if (!Regex.IsMatch(request.newPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
            {
                return new Response { Success = false, Message = "Password must contain at least one uppercase letter, one lowercase letter, and one number." };
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string encryptedNewPassword = Encryption.Encrypt(request.newPassword);

                using (SqlCommand command = new SqlCommand("[dbo].[jsResetAdminPassword]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@updatedBy", request.updatedBy);
                    command.Parameters.AddWithValue("@newPassword", encryptedNewPassword);

                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        var result = await command.ExecuteScalarAsync();

                        response.Success = true;
                        response.Message = "Password updated successfully";
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 50001)
                        {
                            response.Message = "User ID is not valid";
                        }
                        else
                        {
                            response.Message = $"{ex.Message}";
                        }

                    }
                    catch (Exception ex)
                    {
                        response.Message = $"{ex.Message}";
                    }
                }
            }

            return response;
        }
        public async Task<IEnumerable<TemplateListModel>> GetTemplateListAsync(int company)
        {
            var sqlQuery = "EXEC [dbo].[jsGetTemplateList] @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TemplateListModel>(
                    sqlQuery,
                    new { company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<object> GetOneTemplateDetailAsync(int tempId, int company)
        {
            var sqlQuery = "EXEC [dbo].[jsGetTemplateDetail] @tempId, @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                var details = await connection.QueryAsync<OneTemplateDetailModel>(
                    sqlQuery,
                    new { tempId, company }
                );

                if (!details.Any())
                    return null;

                // Group by template details (tempId)
                var groupedData = details.GroupBy(d => new
                {
                    d.tempId,
                    d.tempName,
                    d.isActive,
                    d.tempCreatedOn,
                    d.createdBy,
                    d.description // Move description to template level
                })
                .Select(g => new
                {
                    tempId = g.Key.tempId,
                    tempName = g.Key.tempName,
                    isActive = g.Key.isActive,
                    tempCreatedOn = g.Key.tempCreatedOn,
                    createdBy = g.Key.createdBy,
                    description = g.Key.description, // Moved from stages to template level

                    // Group stages separately
                    stages = g.Select(s => new
                    {
                        stageId = s.stageId,
                        stage = s.stage,
                        stageCreatedOn = s.stageCreatedOn,
                        priority = s.priority
                    }).Distinct().ToList(),

                    // Group queries separately
                    queries = g.Select(q => new
                    {
                        queryId = q.queryId,
                        queryName = q.queryName,
                        query = q.query
                    }).Distinct().ToList(),

                    // Group approvals separately
                    approvals = g.Select(a => new
                    {
                        approvalId = a.approvalId,
                        approvalName = a.approvalName
                    }).Distinct().ToList()
                })
                .FirstOrDefault();

                return groupedData;
            }
        }

        public async Task<IEnumerable<StageListModel>> GetStageListAsync(int company)
        {
            var sqlQuery = "EXEC [dbo].[jsGetStageList] @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<StageListModel>(
                    sqlQuery,
                    new { company } // Parameters for the stored procedure
                );
            }
        }
        public async Task<object> GetOneStageDetailAsync(int stageId, int company)
        {
            var sqlQuery = "EXEC [dbo].[jsGetStageDetail] @stageId, @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                var details = await connection.QueryAsync<OneStageDetailModel>(
                    sqlQuery,
                    new { stageId, company }
                );

                if (!details.Any())
                    return null;

                // Group by template details (tempId)
                var groupedData = details.GroupBy(d => new
                {
                    d.id,
                    d.stage,
                    d.createdOn,
                    d.description,
                    d.createdBy,
                    d.approval,
                    d.rejection,
                    d.company
                })
                .Select(g => new
                {
                    StageId = g.Key.id,
                    StageName = g.Key.stage,
                    createdOn = g.Key.createdOn,
                    description = g.Key.description,
                    LoginUser = g.Key.createdBy,
                    approval = g.Key.approval,
                    rejection = g.Key.rejection,
                    company = g.Key.company,

                    // Group stages separately
                    UserInstages = g.Select(s => new
                    {
                        userInstage = s.userInStage

                    }).Distinct().ToList(),

                })
                .FirstOrDefault();

                return groupedData;
            }
        }

        public async Task<IEnumerable<FlowDocEntryModel>> GetFlowDocEntryAsync(int docEntry)
        {
            var sqlQuery = "EXEC [dbo].[getFlowDocEntry] @docEntry";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<FlowDocEntryModel>(
                    sqlQuery,
                    new { docEntry } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<Response>> AddAlternativeUserToStagesAsync(AddAlternativeUserToStagesModel request)
        {
            var response = new List<Response>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("[dbo].[AddAlternativeUserToStages]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@alternativeUserId", request.alternativeUserId);
                    command.Parameters.AddWithValue("@startDate", request.startDate);
                    command.Parameters.AddWithValue("@endDate", request.endDate);
                    command.Parameters.AddWithValue("@stages", request.stages);

                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        // Get the last inserted ID
                        command.CommandText = "SELECT SCOPE_IDENTITY()";
                        command.CommandType = CommandType.Text;
                        var result = await command.ExecuteScalarAsync();

                        response.Add(new Response
                        {
                            Success = true,
                            Message = "Stage added successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        // Handle specific error codes from the stored procedure
                        if (ex.Number == 50002)
                        {
                            response.Add(new Response { Success = false, Message = "Invalid Alternative User Id" });
                        }
                        else if (ex.Number == 50001)
                        {
                            response.Add(new Response { Success = false, Message = "Invalid User Id" });
                        }
                        else if (ex.Number == 50003)
                        {
                            response.Add(new Response { Success = false, Message = "One or more stages do not exist" });
                        }
                        else if (ex.Number == 50004)
                        {
                            response.Add(new Response { Success = false, Message = "Original user is not present in one or more specified stages" });
                        }
                        else
                        {
                            response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                }
            }
            return response;
        }
        public async Task<IEnumerable<Response>> DeactivateDelegationAsync(DeactivateDelegationModel request)
        {
            var response = new List<Response>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("[dbo].[jsDeactivateDelegation]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@alternativeUserId", request.alternativeUserId);

                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        // Get the last inserted ID
                        command.CommandText = "SELECT SCOPE_IDENTITY()";
                        command.CommandType = CommandType.Text;
                        var result = await command.ExecuteScalarAsync();

                        response.Add(new Response
                        {
                            Success = true,
                            Message = "Deactivate Delegation successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                    catch (Exception ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                }
            }
            return response;
        }
        public async Task<IEnumerable<FlowDocEntryTwoModel>> GetFlowDocEntryTwoAsync(int docEntry)
        {
            var sqlQuery = "EXEC [dbo].[getFlowDocEntryTwo] @docEntry";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<FlowDocEntryTwoModel>(
                    sqlQuery,
                    new { docEntry } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<UserBudgetAllocationModel>> GetUserBudgetAllocationAsync(int userId, int company, string month)
        {
            const string sqlQuery = "EXEC [bud].[jsGetUserBudgetAllocation] @userId, @company, @month";

            _logger.LogDebug("Executing {StoredProc} with UserId={UserId}, Company={Company}, Month={Month}",
                "jsGetUserBudgetAllocation", userId, company, month);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<UserBudgetAllocationModel>(
                    sqlQuery,
                    new { userId, company, month },
                    commandTimeout: 30
                );

                stopwatch.Stop();
                _logger.LogDebug("Query completed in {ElapsedMs}ms, returned {Count} records",
                    stopwatch.ElapsedMilliseconds, result.Count());

                return result;
            }
            catch (SqlException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SQL Error {ErrorNumber} executing {StoredProc} | UserId={UserId}, Company={Company}, Month={Month} | Duration={ElapsedMs}ms",
                    ex.Number, "jsGetUserBudgetAllocation", userId, company, month, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Unexpected error executing {StoredProc} | UserId={UserId}, Company={Company}, Month={Month} | Duration={ElapsedMs}ms",
                    "jsGetUserBudgetAllocation", userId, company, month, stopwatch.ElapsedMilliseconds);
                throw;
            }

        }

        public async Task<IEnumerable<ApprovalDelegationListModel>> GetApprovalDelegationListAsync()
        {
            var sqlQuery = "EXEC [dbo].[jsGetApprovalDelegationList]";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ApprovalDelegationListModel>(
                    sqlQuery
                );
            }
        }

        public async Task<IEnumerable<TemplateListAccordingToUserModel>> GetTemplateListAccordingToUserAsync(int company, int userId)
        {
            var sqlQuery = "EXEC [dbo].[jsGetTemplateListAccordingToUser] @company, @userId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TemplateListAccordingToUserModel>(
                    sqlQuery,
                    new { company, userId } // Parameters for the stored procedure
                );
            }
        }

        public async Task<Response> CreateCategoryMonthlyBudgetAsync(MonthlyBudgetModel request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("[bud].[jsCreateCategoryMonthlyBudget]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@budgetCategory", request.budgetCategory);
                        cmd.Parameters.AddWithValue("@subBudget", request.subBudget);
                        cmd.Parameters.AddWithValue("@month", request.month);
                        cmd.Parameters.AddWithValue("@totalAmount", request.totalAmount);
                        cmd.Parameters.AddWithValue("@company", request.company);

                        var result = await cmd.ExecuteScalarAsync();
                        return new Response { Success = true, Message = result?.ToString() ?? "Create completed." };
                    }
                }
            }
            catch (SqlException ex)
            {
                return new Response { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<IEnumerable<BudgetCategorySummaryDashboardModel>> GetBudgetCategorySummaryDashboardAsync(string month, int company)
        {
            var sqlQuery = "EXEC [bud].[jsGetBudgetCategorySummaryDashboard] @month,@company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetCategorySummaryDashboardModel>(
                    sqlQuery,
                   new { month, company } // Parameters for the stored procedure
                );
            }
        }

        /*public async Task<BudgetResponseDTO> GetBudgetDetailByIdAsync(int budgetId,int company, IUrlHelper urlHelper)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new { budgetId };

                BudgetDetailDTO header = null;
                List<BudgetLineDetailDTO> lines = new List<BudgetLineDetailDTO>();

                using (var multi = await connection.QueryMultipleAsync("[bud].[jsGetBudgetDetailById]", parameters, commandType: CommandType.StoredProcedure))
                {
                    header = await multi.ReadFirstOrDefaultAsync<BudgetDetailDTO>();
                    lines = (await multi.ReadAsync<BudgetLineDetailDTO>()).AsList();
                }

                // Fetch attachments
                var attachments = (await GetBudgetAttachmentsAsync(budgetId)).ToList();

                foreach (var attachment in attachments)
                {
                    attachment.DownloadUrl = $"http://103.89.44.176:8000/files/{Uri.EscapeDataString(attachment.FileName)}.{Uri.EscapeDataString(attachment.FileExt)}?company={company}";
                }

                // Add DownloadUrl to each attachment
               *//* foreach (var attachment in attachments)
                {
                    attachment.DownloadUrl = string.IsNullOrEmpty(attachment.trgtPath) ? null :
                        urlHelper.Action("DownloadFile", "File", new
                        {
                            filePath = Uri.EscapeDataString(attachment.trgtPath.Replace("\\", "/")),
                            fileName = Uri.EscapeDataString(attachment.FileName ?? ""),
                            fileExt = Uri.EscapeDataString(attachment.FileExt ?? "")
                        }, "http");
                }*//*

                return new BudgetResponseDTO
                {
                    BudgetHeader = header,
                    BudgetLines = lines,
                    Attachments = attachments // You can update BudgetAttachmentModels to include DownloadUrl if needed
                };
            }
        }
*/
        public async Task<BudgetResponseDTO> GetBudgetDetailByIdAsync(int budgetId, IUrlHelper urlHelper)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new { budgetId };

                BudgetDetailDTO header = null;
                List<BudgetLineDetailDTO> lines = new List<BudgetLineDetailDTO>();

                using (var multi = await connection.QueryMultipleAsync("[bud].[jsGetBudgetDetailById]", parameters, commandType: CommandType.StoredProcedure))
                {
                    header = await multi.ReadFirstOrDefaultAsync<BudgetDetailDTO>();
                    lines = (await multi.ReadAsync<BudgetLineDetailDTO>()).AsList();
                }

                // Fetch attachments
                var attachments = (await GetBudgetAttachmentsAsync(budgetId)).ToList();

                // Add DownloadUrl to each attachment
                foreach (var attachment in attachments)
                {
                    attachment.DownloadUrl = string.IsNullOrEmpty(attachment.trgtPath) ? null :
                        urlHelper.Action("DownloadFile", "File", new
                        {
                            filePath = Uri.EscapeDataString(attachment.trgtPath.Replace("\\", "/")),
                            fileName = Uri.EscapeDataString(attachment.FileName ?? ""),
                            fileExt = Uri.EscapeDataString(attachment.FileExt ?? "")
                        }, "http");
                }

                return new BudgetResponseDTO
                {
                    BudgetHeader = header,
                    BudgetLines = lines,
                    Attachments = attachments // You can update BudgetAttachmentModels to include DownloadUrl if needed
                };
            }
        }
        public async Task<BudgetResponseDTO> GetBudgetDetailByIdAsyncv2(int budgetId, int company, IUrlHelper urlHelper)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new { budgetId };

                BudgetDetailDTO header = null;
                List<BudgetLineDetailDTO> lines = new List<BudgetLineDetailDTO>();

                using (var multi = await connection.QueryMultipleAsync("[bud].[jsGetBudgetDetailById]", parameters, commandType: CommandType.StoredProcedure))
                {
                    header = await multi.ReadFirstOrDefaultAsync<BudgetDetailDTO>();
                    lines = (await multi.ReadAsync<BudgetLineDetailDTO>()).AsList();
                }

                // Fetch attachments
                var attachments = (await GetBudgetAttachmentsAsync(budgetId)).ToList();

                foreach (var attachment in attachments)
                {
                    //attachment.DownloadUrl = $"http://files.jivocanola.com/files/{Uri.EscapeDataString(attachment.FileName)}.{Uri.EscapeDataString(attachment.FileExt)}?company={company}";
                    attachment.DownloadUrl = $"http://files.jivo.in:8000/files/{Uri.EscapeDataString(attachment.FileName)}.{Uri.EscapeDataString(attachment.FileExt)}?company={company}";

                }

                return new BudgetResponseDTO
                {
                    BudgetHeader = header,
                    BudgetLines = lines,
                    Attachments = attachments // You can update BudgetAttachmentModels to include DownloadUrl if needed
                };
            }
        }
        public async Task<BudgetResponseDTO> GetBudgetDetailByIdAsync2(int budgetId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new { budgetId };

                BudgetDetailDTO header = null;
                List<BudgetLineDetailDTO> lines = new List<BudgetLineDetailDTO>();

                using (var multi = await connection.QueryMultipleAsync("[bud].[jsGetBudgetDetailById]", parameters, commandType: CommandType.StoredProcedure))
                {
                    header = await multi.ReadFirstOrDefaultAsync<BudgetDetailDTO>();
                    lines = (await multi.ReadAsync<BudgetLineDetailDTO>()).AsList();
                }

                return new BudgetResponseDTO
                {
                    BudgetHeader = header,
                    BudgetLines = lines,
                };
            }
        }

        public async Task<IEnumerable<CategoryMonthlyBudgetModel>> GetCategoryMonthlyBudgetAsync(string budgetCategory, string subBudget, string month, int company)
        {
            var sqlQuery = "EXEC [bud].[jsGetCategoryMonthlyBudget] @budgetCategory,@subBudget ,@month,@company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<CategoryMonthlyBudgetModel>(
                    sqlQuery,
                   new { budgetCategory, subBudget, month, company } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<BudgetAttachmentModels>> GetBudgetAttachmentsAsync(int budgetId)
        {
            var sqlQuery = "EXEC [bud].[jsGetBudgetAttachments] @budgetId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetAttachmentModels>(
                    sqlQuery,
                   new { budgetId } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<BudgetCategoryDropdownModel>> BudgetCategoryDropdownAsync(int userId, int company)
        {
            var sqlQuery = "EXEC [bud].[jsBudgetCategoryDropdown] @userId, @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetCategoryDropdownModel>(
                    sqlQuery,
                   new { userId, company } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<BudgetApprovalFlowModel>> GetBudgetApprovalFlowAsync(int budgetId)
        {
            var sqlQuery = "EXEC [bud].[jsGetBudgetApprovalFlow] @budgetId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetApprovalFlowModel>(
                    sqlQuery,
                   new { budgetId } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<Response>> DelegateApprovalStagesTwoAsync(DelegateApprovalStagesTwoModel request)
        {
            var response = new List<Response>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("[dbo].[jsDelegateApprovalStagesTwo]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@delegatedUserId", request.delegatedUserId);
                    command.Parameters.AddWithValue("@stages", request.stages);
                    command.Parameters.AddWithValue("@startDate", request.startDate);
                    command.Parameters.AddWithValue("@endDate", request.endDate);
                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new Response
                        {
                            Success = true,
                            Message = "Delegation completed successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                    catch (Exception ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                }
            }
            return response;
        }

        public async Task<IEnumerable<DelegatedUserListTwoModel>> GetDelegatedUserListTwoAsync()
        {
            var sqlQuery = "EXEC [dbo].[jsGetDelegatedUserListTwo] ";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<DelegatedUserListTwoModel>(
                    sqlQuery
                );
            }
        }

        public async Task<IEnumerable<Response>> UpdateDelegationDatesTwoAsync(UpdateDelegationDatesTwoModel request)
        {
            var response = new List<Response>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("[dbo].[jsUpdateDelegationDatesTwo]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@delegatedUserId", request.delegatedUserId);
                    command.Parameters.AddWithValue("@stageId", request.stageId);
                    command.Parameters.AddWithValue("@newStartDate", request.newStartDate);
                    command.Parameters.AddWithValue("@newEndDate", request.newEndDate);
                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new Response
                        {
                            Success = true,
                            Message = "Delegation updated successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                    catch (Exception ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                }
            }
            return response;
        }

        public async Task<IEnumerable<Response>> UpdateUserStageStatusTwoAsync(UpdateUserStageStatusTwoModel request)
        {
            var response = new List<Response>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("[dbo].[jsUpdateUserStageStatusTwo]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // Add parameters
                    command.Parameters.AddWithValue("@StageId", request.StageId);
                    command.Parameters.AddWithValue("@UserId", request.UserId);
                    command.Parameters.AddWithValue("@delegatedUserId", request.delegatedUserId);
                    command.Parameters.AddWithValue("@Activate", request.Activate);
                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new Response
                        {
                            Success = true,
                            Message = "Stage updated successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                    catch (Exception ex)
                    {
                        response.Add(new Response { Success = false, Message = $"{ex.Message}" });
                    }
                }
            }
            return response;
        }

        public async Task<IEnumerable<ActiveTemplateModel>> GetActiveTemplateSync(int company)
        {
            var sqlQuery = "EXEC [dbo].[jsGetActiveTemplate] @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ActiveTemplateModel>(
                    sqlQuery,
                   new { company } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<BudgetRelatedToTemplateModel>> GetBudgetRelatedToTemplateAsync(int templateId)
        {
            var sqlQuery = "EXEC [dbo].[jsGetBudgetRelatedToTemplate] @templateId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetRelatedToTemplateModel>(
                    sqlQuery,
                   new { templateId } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<GetOneDelegatedUser>> GetOneDelegatedUserAsync(int stageId, int delegatedBy, int delegatedTo)
        {
            var sqlQuery = "EXEC [dbo].[jsGetOneDelegatedUserListTwo] @stageId ,@delegatedBy, @delegatedTo";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetOneDelegatedUser>(
                    sqlQuery,
                   new { stageId, delegatedBy, delegatedTo } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<GetAllBudgetInsight>> GetBudgetInsightAsync(int company, string month)
        {
            var sqlQuery = "EXEC [bud].[jsGetBudgetInsightAll] @company,@month ";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetAllBudgetInsight>(
                    sqlQuery,
                   new { company, month },
                   commandTimeout: 300// Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<UserIdsForNotificationModel>> GetUserIdsSendNotificatiosAsync(int budgetId)
        {
            var sqlQuery = "EXEC [bud].[jsBudgetNotify] @budgetId ";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<UserIdsForNotificationModel>(
                    sqlQuery,
                   new { budgetId } // Parameters for the stored procedure
                );
            }
        }

        public async Task<Response> ApproveBudgetAsync(BudgetRequest2 request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // Split docIds if multiple (assumes comma-separated string)
                    var docIds = request.docIds?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(id => int.Parse(id.Trim()))
                                                .ToList() ?? new List<int>();

                    var resultMessages = new List<string>();
                    var allNotificationModels = new List<UserIdsForNotificationModel>();

                    foreach (var docId in docIds)
                    {
                        using (SqlCommand cmd = new SqlCommand("[bud].[jsApproveBudget]", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@docId", docId);
                            cmd.Parameters.AddWithValue("@userId", request.userId);
                            cmd.Parameters.AddWithValue("@company", request.company);
                            cmd.Parameters.AddWithValue("@remarks", (object?)request.remarks ?? DBNull.Value);

                            var result = await cmd.ExecuteScalarAsync();
                            resultMessages.Add(result?.ToString() ?? $"Approved Budget ID {docId}");

                            var notifications = await GetUserIdsSendNotificatiosAsync(docId);
                            if (notifications != null)
                                allNotificationModels.AddRange(notifications);
                        }
                    }

                    // ✅ FIX 1: Deduplicate notification models (same userId multiple times)
                    allNotificationModels = allNotificationModels
                        .Where(m => !string.IsNullOrWhiteSpace(m.userIdsToApprove))
                        .GroupBy(m => m.userIdsToApprove)
                        .Select(g => g.First())
                        .ToList();

                    // ✅ FIX 2: Get unique user IDs
                    var uniqueUserIds = new HashSet<int>(
                        allNotificationModels
                            .SelectMany(m => m.userIdsToApprove.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            .Select(s => int.Parse(s.Trim()))
                    );

                    // Decide title/body based on single vs multiple approvals
                    string notificationTitle = docIds.Count == 1
                        ? "You have received a new request from JSAP"
                        : "You have received new requests from JSAP";

                    string notificationBody = docIds.Count == 1
                        ? $"New budget approval request for Budget ID {docIds[0]}. Please approve within 48 hours, or it will be auto-approved."
                        : $"New budget approval requests: {docIds.Count}. Please approve within 48 hours, or it will be auto-approved.";

                    var data = new Dictionary<string, string>
                    {
                       { "screen", "details" },
                       { "company", request.company.ToString() },
                       { "budgetIds", string.Join(",", docIds) }
                    };

                    // ✅ FIX 3: Track sent tokens to avoid duplicates
                    var sentTokens = new HashSet<string>();

                    foreach (var userId in uniqueUserIds)
                    {
                        var fcmToken = await _notificationService.GetUserFcmTokenAsync(userId);
                        if (fcmToken == null || fcmToken.Count == 0)
                            continue;

                        // ✅ FIX 4: Send push notification to all tokens (but only once per token)
                        foreach (var token in fcmToken)
                        {
                            if (string.IsNullOrWhiteSpace(token.fcmToken))
                                continue;

                            // ✅ Check if already sent to this token
                            if (sentTokens.Contains(token.fcmToken))
                                continue;

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                token.fcmToken,
                                data
                            );

                            // ✅ Mark this token as sent
                            sentTokens.Add(token.fcmToken);
                        }

                        // ✅ FIX 5: Insert database notification only once per user (not per docId)
                        // If you want one notification per docId, keep the foreach loop
                        // If you want one notification per user regardless of docIds, use this:

                        await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                        {
                            userId = userId,
                            title = "Pending request",
                            message = notificationBody,
                            pageId = 6,
                            data = docIds.Count == 1
                                ? $"Budget Id: {docIds[0]}"
                                : $"Budget Ids: {string.Join(", ", docIds)}",
                            BudgetId = docIds[0]  // First budget ID
                        });
                    }

                    return new Response
                    {
                        Success = true,
                        Message = string.Join(" | ", resultMessages)
                    };
                }
            }
            catch (SqlException ex)
            {
                return new Response { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Response> SendPendingCountNotificationAsync()
        {
            var responseMessage = new StringBuilder();
            bool overallSuccess = true;
            bool foundAnyPending = false;

            try
            {
                var activeUsers = await GetActiveUser();
                if (activeUsers == null || !activeUsers.Any())
                    return new Response { Success = false, Message = "No active users found." };

                // ✅ Track which tokens we've already sent to (to prevent duplicates in this request)
                var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Track which users we've already processed
                HashSet<int> notifiedUsers = new HashSet<int>();

                foreach (var user in activeUsers)
                {
                    int userId = user.userId;

                    // Skip if we've already notified this user
                    if (notifiedUsers.Contains(userId))
                        continue;

                    int company = user.company;
                    string month = DateTime.Now.ToString("MM-yyyy");
                    //string month = "05-2025";

                    // fetch all counts for this single user
                    var counts = await GetAllBudgetApprovalCountsAsync(userId, company, month);
                    if (counts == null || !counts.Any())
                    {
                        responseMessage.AppendLine($"No budget counts for user {userId}.");
                        continue;
                    }

                    // total up ALL pendings for this user
                    int totalPending = counts.Sum(c => c.TotalPending);
                    if (totalPending <= 0)
                        continue;   // nothing to send

                    foundAnyPending = true;

                    // ✅ Get list of FCM tokens
                    var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);

                    if (fcmTokens == null || fcmTokens.Count == 0)
                    {
                        responseMessage.AppendLine($"No FCM token for user {userId}.");
                        overallSuccess = false;
                        continue;
                    }

                    // build notification data
                    string title = $"You have {totalPending} " +
                                   (totalPending == 1 ? "pending request" : "pending requests");
                    string body = "Please approve or it will be auto-approved.";

                    var data = new Dictionary<string, string>
                    {
                        { "userId",  userId.ToString() },
                        { "company", company.ToString() },
                        { "screen",  "pending" }
                    };

                    // ✅ Send push notification to all tokens (but only once per token)
                    int tokensSent = 0;
                    foreach (var token in fcmTokens)
                    {
                        var normalizedToken = token.fcmToken?.Trim();

                        if (string.IsNullOrWhiteSpace(normalizedToken))
                            continue;

                        // ✅ Check if already sent to this token
                        if (sentTokens.Contains(normalizedToken))
                        {
                            Console.WriteLine($"⏭️ Skipping duplicate token for userId {userId}");
                            continue;
                        }

                        await _notificationService.SendPushNotificationAsync(
                            title,
                            body,
                            normalizedToken,
                            data
                        );

                        // ✅ Mark this token as sent
                        sentTokens.Add(normalizedToken);
                        tokensSent++;
                    }

                    if (tokensSent > 0)
                    {
                        // Add to our tracking set after successful notification
                        notifiedUsers.Add(userId);
                        responseMessage.AppendLine($"Notification sent to user {userId} ({tokensSent} device(s)).");
                    }
                    else
                    {
                        responseMessage.AppendLine($"No valid/unique tokens for user {userId}.");
                    }
                }

                if (!foundAnyPending)
                    return new Response
                    {
                        Success = true,
                        Message = "No pending requests for any active user."
                    };

                return new Response
                {
                    Success = overallSuccess,
                    Message = responseMessage.ToString().Trim()
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }


        public async Task<IEnumerable<SubBudgetCategoryDropdownModel>> GetSubBudgetCategoryDropdownAsync(int userId, int company)
        {
            var sqlQuery = "EXEC [bud].[jsSubBudgetCategoryDropdown] @userId, @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<SubBudgetCategoryDropdownModel>(
                    sqlQuery,
                   new { userId, company } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<BudgetSummaryModel>> GetBudgetSummaryAsync(int userId, string budgetCategory, string subBudget, string month, int company)
        {
            var sqlQuery = "EXEC [bud].[jsGetBudgetSummary] @userId, @budgetCategory, @subBudget, @month, @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BudgetSummaryModel>(
                    sqlQuery,
                   new { userId, budgetCategory, subBudget, month, company } // Parameters for the stored procedure
                );
            }
        }

        public async Task<CombinedBudgetDTO> GetCombinedBudgetsAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Open the connection
                await connection.OpenAsync();

                // Fetch data from the stored procedures
                var pendingBudgets = await connection.QueryAsync<AllBudgetRequestsModel>(
                    "EXEC  [bud].[jsGetPendingBudgets] @userId, @company,@month",
                    new { userId, company, month = string.IsNullOrEmpty(month) ? null : month });

                var approvedBudgets = await connection.QueryAsync<AllBudgetRequestsModel>(
                    "EXEC [bud].[jsGetApprovedBudgets] @userId, @company,@month",
                    new { userId, company, month = string.IsNullOrEmpty(month) ? null : month });

                var rejectedBudgets = await connection.QueryAsync<AllBudgetRequestsModel>(
                    "EXEC [bud].[jsGetRejectedBudgets] @userId, @company,@month",
                    new { userId, company, month = string.IsNullOrEmpty(month) ? null : month });

                // Add status to each record
                foreach (var budget in pendingBudgets)
                {
                    budget.Status = "Pending";
                }

                foreach (var budget in approvedBudgets)
                {
                    budget.Status = "Approved";
                }

                foreach (var budget in rejectedBudgets)
                {
                    budget.Status = "Rejected";
                }

                // Combine all records into one list
                var allBudgets = pendingBudgets.ToList();
                allBudgets.AddRange(approvedBudgets);
                allBudgets.AddRange(rejectedBudgets);

                var budgetDetails = new List<BudgetDetailOnlyDTO>();

                foreach (var Budgets in allBudgets)
                {
                    // For each pending budget, fetch its details
                    var parameters = new { budgetId = Budgets.BudgetId };

                    using (var multi = await connection.QueryMultipleAsync(
                        "[bud].[jsGetBudgetDetailById]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    ))
                    {
                        var header = await multi.ReadFirstOrDefaultAsync<BudgetDetailDTO>();
                        var lines = (await multi.ReadAsync<BudgetLineDetailDTO>()).AsList();

                        budgetDetails.Add(new BudgetDetailOnlyDTO
                        {
                            BudgetHeader = header,
                            BudgetLines = lines
                        });
                    }
                }

                return new CombinedBudgetDTO
                {
                    BudgetData = allBudgets,
                    BudgetDetails = budgetDetails
                };
            }
        }

        public async Task<int> GetDocIdsUsingDocEntryAsync(int docEntry)
        {

            var sqlQuery = "EXEC [bud].[jsGetDocIdsUsingDocEntry] @docEntry";
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = await connection.QuerySingleOrDefaultAsync<int>(
                    sqlQuery,
                   new { docEntry } // Parameters for the stored procedure
                );
                return result;
            }
        }

        public async Task<IEnumerable<ApprovedDocEntriesModel>> GetApprovedDocEntriesAsync(int company, int docEntry)
        {
            var sqlQuery = "EXEC [bud].[jsGetAllApprovedDocEntriesForCompany] @company,@docEntry";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ApprovedDocEntriesModel>(
                    sqlQuery,
                   new { company, docEntry } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<PendingDocEntriesModel>> GetPendingDocEntriesAsync(int company, int docEntry)
        {
            var sqlQuery = "EXEC [bud].[jsGetAllPendingDocEntriesForCompany] @company,@docEntry";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PendingDocEntriesModel>(
                    sqlQuery,
                   new { company, docEntry } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<RejectedDocEntriesModel>> GetRejectedDocEntriesAsync(int company, int docEntry)
        {
            var sqlQuery = "EXEC [bud].[jsGetAllRejectedDocEntriesForCompany] @company,@docEntry";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<RejectedDocEntriesModel>(
                    sqlQuery,
                   new { company, docEntry } // Parameters for the stored procedure
                );
            }
        }

        //for web
        public async Task<Response> ValidateUsernameAsync(string username)
        {
            var response = new Response();

            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("[dbo].[jsValidateUsername]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userNAME", username);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    // If no exception is thrown
                    response.Success = true;
                    response.Message = "Username is valid.";
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50001)
                {
                    response.Success = false;
                    //response.Message = ex.Message;
                    response.Message = "UserName already exist";
                }
                else
                {
                    response.Success = false;
                    response.Message = "An unexpected error occurred.";
                }
            }

            return response;
        }

        public async Task<ApiResponse> UpdateUserInfoAsync(UserUpdateDTO model)
        {
            var response = new ApiResponse();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("jsUpdateUserInfo", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@userId", model.UserId);
                cmd.Parameters.AddWithValue("@userPhoneNumber", model.UserPhoneNumber);
                cmd.Parameters.AddWithValue("@userEmail", model.UserEmail);
                cmd.Parameters.AddWithValue("@firstName", model.FirstName);
                cmd.Parameters.AddWithValue("@lastName", model.LastName);
                cmd.Parameters.AddWithValue("@empId", model.EmpId);
                cmd.Parameters.AddWithValue("@deptIds", model.DeptIds);
                cmd.Parameters.AddWithValue("@updatedBy", model.UpdatedBy);

                var statusCodeParam = new SqlParameter("@statusCode", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(statusCodeParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int statusCode = (int)statusCodeParam.Value;

                response.StatusCode = statusCode;
                response.Success = (statusCode == 2001);
                response.Message = statusCode switch
                {
                    2001 => "User updated successfully.",
                    4001 => "Phone number is required.",
                    4002 => "Email is required.",
                    4005 => "First name is required.",
                    4006 => "Last name is required.",
                    4007 => "Employee ID is required.",
                    4008 => "Department IDs required.",
                    4011 => "Invalid phone number length.",
                    4012 => "Invalid email format.",
                    4016 => "Invalid department IDs format.",
                    4017 => "Invalid department IDs (commas).",
                    4020 => "Valid updater ID required.",
                    4021 => "Updater user does not exist.",
                    4404 => "User not found.",
                    5003 => "Phone number already exists for another user.",
                    5004 => "Email already exists for another user.",
                    5005 => "Employee ID already exists for another user.",
                    5000 => "Database error occurred.",
                    _ => "Unknown error occurred."
                };
            }

            return response;
        }
        public async Task<UserListResponseDto> GetUsersByDepartmentId(int deptId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@deptId", deptId);

                    var users = await connection.QueryAsync<UserResponseDto>(
                        "jsGetUserUsingDeptId",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var userList = users.ToList();

                    return new UserListResponseDto
                    {
                        Users = userList,
                        TotalCount = userList.Count
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving users by department: {ex.Message}", ex);
            }
        }

    }
}

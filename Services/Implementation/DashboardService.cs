using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using ServiceStack;
using System.Data;
using TicketSystem.Models;

namespace JSAPNEW.Services.Implementation
{
    public class DashboardService : IDashboardService
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;

        // Phase 1 (Dashboard): server-side cache settings for dropdown lookups only.
        private static readonly TimeSpan DropdownCacheDuration = TimeSpan.FromMinutes(15);

        public DashboardService(IConfiguration configuration, IMemoryCache cache)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _cache = cache;
        }

        public async Task<IEnumerable<ITStandardsModel>> GetITStandardsMasterAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<ITStandardsModel>(
                    "[it].[Get_IT_Standards_Master]",
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching IT standards master: {ex.Message}", ex);
            }
        }

        public async Task<ITStandardsSummary> GetITStandardsSummaryAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<ITStandardsSummary>(
                    "[it].[Get_IT_Standards_Summary]",
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching IT standards summary: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<ITStandardsModel>> GetITStandardsByPriorityAsync(string priority)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Priority", priority);

                var result = await connection.QueryAsync<ITStandardsModel>(
                    "[it].[Get_IT_Standards_ByPriority]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching IT standards by priority: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<DashboardStatsModel>> GetDashboardStatsAsync(string group_by, int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@group_by", group_by);
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<DashboardStatsModel>(
                    "[it].[sp_GetDashboardStats]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching dashboard stats: {ex.Message}", ex);
            }
        }

        /// Get all tasks with filters
        public async Task<IEnumerable<TaskModel>> GetAllTasksAsync(TaskFilterRequest filter)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // Parse string to int? (nullable int)
                int? assignedTo = null;
                int? deptId = null;

                if (!string.IsNullOrEmpty(filter?.AssignedTo) && int.TryParse(filter.AssignedTo, out int parsedAssignedTo))
                {
                    assignedTo = parsedAssignedTo;
                }

                if (!string.IsNullOrEmpty(filter?.DeptId) && int.TryParse(filter.DeptId, out int parsedDeptId))
                {
                    deptId = parsedDeptId;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@search", filter?.Search);
                parameters.Add("@priority", filter?.Priority);
                parameters.Add("@status", filter?.Status);
                parameters.Add("@project", filter?.Project);
                parameters.Add("@assigned_to", assignedTo);    // Now int?
                parameters.Add("@dept_id", deptId);            // Now int?
                parameters.Add("@start_date", filter?.StartDate);
                parameters.Add("@end_date", filter?.EndDate);

                var result = await connection.QueryAsync<TaskModel>(
                    "[it].[sp_GetAllTasks]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching tasks: {ex.Message}", ex);
            }
        }

        /// Get task by ID
        public async Task<TaskModel> GetTaskByIdAsync(string taskId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@task_id", taskId);

                var result = await connection.QueryFirstOrDefaultAsync<TaskModel>(
                    "[it].[sp_GetTaskById]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching task by ID: {ex.Message}", ex);
            }
        }

        /// Get status distribution
        public async Task<IEnumerable<StatusDistributionModel>> GetStatusDistributionAsync(int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<StatusDistributionModel>(
                    "[it].[sp_GetStatusDistribution]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching status distribution: {ex.Message}", ex);
            }
        }

        /// Get priority distribution
        public async Task<IEnumerable<PriorityDistributionModel>> GetPriorityDistributionAsync(int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<PriorityDistributionModel>(
                    "[it].[sp_GetPriorityDistribution]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching priority distribution: {ex.Message}", ex);
            }
        }

        /// Get employee stats
        public async Task<IEnumerable<EmployeeStatsModel>> GetEmployeeStatsAsync(int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<EmployeeStatsModel>(
                    "[it].[sp_GetEmployeeStats]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching employee stats: {ex.Message}", ex);
            }
        }

        /// Get employee tasks
        public async Task<IEnumerable<TaskModel>> GetEmployeeTasksAsync(int employeeId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@employee_id", employeeId);

                var result = await connection.QueryAsync<TaskModel>(
                    "[it].[sp_GetEmployeeTasks]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching employee tasks: {ex.Message}", ex);
            }
        }

        /// Get project list
        public async Task<IEnumerable<ProjectListModel>> GetProjectListAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var result = await connection.QueryAsync<ProjectListModel>(
                    "[it].[sp_GetProjectList]",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching project list: {ex.Message}", ex);
            }
        }

        /// Get employee list
        public async Task<IEnumerable<EmployeeListModel>> GetEmployeeListAsync(int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<EmployeeListModel>(
                    "[it].[sp_GetEmployeeList]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching employee list: {ex.Message}", ex);
            }
        }

        /// Get project breakdown
        public async Task<IEnumerable<ProjectBreakdownModel>> GetProjectBreakdownAsync(int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<ProjectBreakdownModel>(
                    "[it].[sp_GetProjectBreakdown]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching project breakdown: {ex.Message}", ex);
            }
        }

        /// Get daily task trend
        public async Task<IEnumerable<DailyTaskTrendModel>> GetDailyTaskTrendAsync(int days = 30, int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@days", days);
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<DailyTaskTrendModel>(
                    "[it].[sp_GetDailyTaskTrend]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching daily task trend: {ex.Message}", ex);
            }
        }

        /// Get overdue tasks
        public async Task<IEnumerable<TaskModel>> GetOverdueTasksAsync(int? deptId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@dept_id", deptId);

                var result = await connection.QueryAsync<TaskModel>(
                    "[it].[sp_GetOverdueTasks]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching overdue tasks: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<DashboardByCompanyModel>> GetDashboardByCompanyAsync(string clientName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@ClientName", clientName);

                var result = await connection.QueryAsync<DashboardByCompanyModel>(
                    "[it].Dashboard_ByCompany",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching overdue tasks: {ex.Message}", ex);
            }
        }
        public async Task<IEnumerable<DashboardMasterModel>> GetDashboardMasterAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                var result = await connection.QueryAsync<DashboardMasterModel>(
                    "[it].Dashboard_Master",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching overdue tasks: {ex.Message}", ex);
            }
        }
        public async Task<IEnumerable<DashboardProjectModel>> GetDashboardProjectAsync(int projectId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@ProjectID", projectId);

                var result = await connection.QueryAsync<DashboardProjectModel>(
                    "[it].Dashboard_Project",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching overdue tasks: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<GetAllMomModel>> GetAllMoMAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                var result = await connection.QueryAsync<GetAllMomModel>(
                    "[it].[sp_GetAllMoM]",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching overdue tasks: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<MomStatusUpdateResponse>> UpdateMoMStatusAsync(MomStatusUpdateRequest request)
        {
            var response = new List<MomStatusUpdateResponse>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("[it].[sp_UpdateMoMStatus]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@MomId", request.MomId);
                    command.Parameters.AddWithValue("@status", request.status);

                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Add(new MomStatusUpdateResponse
                        {
                            MomId = request.MomId,
                            status = request.status,
                            Success = true,
                            Message = "Updated successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        response.Add(new MomStatusUpdateResponse { Success = false, Message = $"{ex.Message}" });

                    }
                    catch (Exception ex)
                    {
                        response.Add(new MomStatusUpdateResponse { Success = false, Message = $"{ex.Message}" });
                    }
                }
            }
            return response;
        }

        public async Task<IEnumerable<MomPointResponse>> AddMoMPointAsync(MomPointRequest request)
        {
            var response = new List<MomPointResponse>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("[it].[sp_AddMoMPoint]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@MeetingDate", request.MeetingDate);
                    command.Parameters.AddWithValue("@Attendees", request.Attendees);
                    command.Parameters.AddWithValue("@Agenda", request.Agenda);
                    command.Parameters.AddWithValue("@notes", request.notes);
                    command.Parameters.AddWithValue("@status", request.status);

                    try
                    {
                        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

                        response.Add(new MomPointResponse
                        {
                            Success = true,
                            Message = "Added successfully",
                            NewMomId = newId
                        });
                    }
                    catch (Exception ex)
                    {
                        response.Add(new MomPointResponse { Success = false, Message = ex.Message });
                    }
                }
            }

            return response;
        }

        public async Task<IEnumerable<AllbudgetDataModel>> GetAllBudgetDataAsync(string? branch)
        {
            var budgetList = new List<AllbudgetDataModel>();
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // Phase 1 (Dashboard): project only the columns the dashboard UI actually consumes
                // (branch, acctName, budget, currentMonth, amount). The remaining 25 view columns
                // were never referenced by AvtarDashboard.js and are dropped to reduce payload size.
                // ORDER BY CreateDate DESC is retained unchanged (a view column may be used in ORDER BY
                // even when not projected), so row order is identical to before.
                var query = @"
           SELECT
               [Branch],
               [AcctName],
               [BUDGET],
               [CURRENTMONTH],
               [AMOUNT]

           FROM bud.jsbudgetTable_vg";

                // Add WHERE clause only if branch is provided
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    query += " WHERE Branch = @Branch";
                }

                query += " ORDER BY CreateDate DESC";

                using var cmd = new SqlCommand(query, conn);

                // Add parameter only if branch is provided
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    cmd.Parameters.AddWithValue("@Branch", branch);
                }

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    budgetList.Add(new AllbudgetDataModel
                    {
                        Branch = reader["Branch"] as string,
                        AcctName = reader["AcctName"] as string,
                        Budget = reader["BUDGET"] as string,
                        CurrentMonth = reader["CURRENTMONTH"] as string,
                        Amount = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : null
                    });
                }

                return budgetList;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetAllBudgetDataAsync: {ex.Message}", ex);
            }
        }
        public async Task<IEnumerable<budgetAcctModel>> GetUniqueAccounts(string? branch)
        {
            // Phase 1 (Dashboard): cache the accounts dropdown per branch for 15 minutes.
            var cacheKey = $"Dashboard:Accounts:{(string.IsNullOrWhiteSpace(branch) ? "ALL" : branch)}";
            if (_cache.TryGetValue(cacheKey, out List<budgetAcctModel> cachedAccounts))
            {
                return cachedAccounts;
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = @"SELECT DISTINCT AcctName FROM bud.jsBudgetTable_vg WHERE AcctName IS NOT NULL";

                // Add branch filter only if provided
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    query += " AND Branch = @Branch";
                }

                query += " ORDER BY AcctName";

                using var cmd = new SqlCommand(query, conn);

                // Add parameter only if branch is provided
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    cmd.Parameters.AddWithValue("@Branch", branch);
                }

                var accountList = new List<budgetAcctModel>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    accountList.Add(new budgetAcctModel
                    {
                        AcctName = reader["AcctName"]?.ToString() ?? string.Empty
                    });
                }

                _cache.Set(cacheKey, accountList, DropdownCacheDuration);
                return accountList;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetUniqueAccounts: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<budgetBudgetModel>> GetUniqueBudgets(string? branch)
        {
            // Phase 1 (Dashboard): cache the budgets dropdown per branch for 15 minutes.
            var cacheKey = $"Dashboard:Budgets:{(string.IsNullOrWhiteSpace(branch) ? "ALL" : branch)}";
            if (_cache.TryGetValue(cacheKey, out List<budgetBudgetModel> cachedBudgets))
            {
                return cachedBudgets;
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = @"SELECT DISTINCT Budget FROM bud.jsBudgetTable_vg WHERE BUDGET IS NOT NULL AND BUDGET != '' ";

                // Add branch filter only if provided
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    query += " AND Branch = @Branch";
                }

                query += " ORDER BY Budget";

                using var cmd = new SqlCommand(query, conn);

                // Add parameter only if branch is provided
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    cmd.Parameters.AddWithValue("@Branch", branch);
                }

                var budgetList = new List<budgetBudgetModel>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    budgetList.Add(new budgetBudgetModel
                    {
                        Budget = reader["Budget"]?.ToString() ?? string.Empty
                    });
                }

                _cache.Set(cacheKey, budgetList, DropdownCacheDuration);
                return budgetList;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetUniqueBudgets: {ex.Message}", ex);
            }
        }
    }
}

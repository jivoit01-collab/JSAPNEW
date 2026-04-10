using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JSAPNEW.Services.Implementation
{
    public class TaskService : ITaskService
    {
        private readonly string _connectionString;

        public TaskService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ============================================================
        // Create Task (uses V2 SP with hierarchy)
        // ============================================================
        public async Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_name", taskDto.TaskName);
                    parameters.Add("@description", taskDto.Description);
                    parameters.Add("@project_name", taskDto.ProjectName);
                    parameters.Add("@module_name", taskDto.ModuleName);
                    parameters.Add("@assigned_to_employee_id", taskDto.AssignedToEmployeeId);
                    parameters.Add("@created_by_employee_id", taskDto.CreatedByEmployeeId);
                    parameters.Add("@assigned_by_employee_id", taskDto.AssignedByEmployeeId);
                    parameters.Add("@start_date", taskDto.StartDate);
                    parameters.Add("@expected_end_date", taskDto.ExpectedEndDate);
                    parameters.Add("@priority", taskDto.Priority);
                    parameters.Add("@dept_id", taskDto.DeptId);
                    parameters.Add("@sub_department_id", taskDto.SubDepartmentId);
                    parameters.Add("@task_type", taskDto.TaskType);
                    parameters.Add("@slot", taskDto.Slot);

                    var result = await connection.QueryFirstOrDefaultAsync<TaskResponseDto>(
                        "tasks.CreateTaskV2",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == null)
                        throw new Exception("Task creation failed. Stored procedure returned no result.");

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating task: {ex.Message}", ex);
            }
        }

        // ============================================================
        // Get All Tasks (Hierarchy-aware with view modes)
        // ============================================================
        public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(TaskFilterDto dto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@page", dto.Page);
                    parameters.Add("@limit", dto.Limit);
                    parameters.Add("@status", dto.Status);
                    parameters.Add("@priority", dto.Priority);
                    parameters.Add("@project_name", dto.ProjectName);
                    parameters.Add("@module_name", dto.ModuleName);
                    parameters.Add("@assigned_to_employee_id", dto.AssignedToEmployeeId);
                    parameters.Add("@created_by_employee_id", dto.CreatedByEmployeeId);
                    parameters.Add("@dept_id", dto.DeptId);
                    parameters.Add("@sub_department_id", dto.SubDepartmentId);
                    parameters.Add("@task_type", dto.TaskType);
                    parameters.Add("@employee_id", dto.EmployeeId);
                    parameters.Add("@role_type_id", dto.RoleTypeId);
                    parameters.Add("@view_mode", dto.ViewMode);
                    parameters.Add("@sort_by", dto.SortBy);
                    parameters.Add("@sort_order", dto.SortOrder);

                    var result = await connection.QueryAsync<TaskResponseDto>(
                        "tasks.GetAllTasksV2",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var filtered = result
                        .Where(t =>
                            !(t.IsDeleted ?? false) &&
                            t.DeletedOn == null &&
                            !string.Equals(t.Status, "DELETED", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (filtered.Count > 0)
                    {
                        var totalCount = filtered.Count;
                        var totalPages = dto.Limit > 0 ? (int)Math.Ceiling((double)totalCount / dto.Limit) : 1;

                        filtered.ForEach(task =>
                        {
                            task.TotalCount = totalCount;
                            task.TotalPages = totalPages;
                        });
                    }

                    return filtered;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching tasks: {ex.Message}", ex);
            }
        }

        // ============================================================
        // Delete Task
        // ============================================================
        public async Task<TaskResponse> DeleteTaskAsync(DeleteTaskRequestDto dto)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_id", dto.TaskId);
                    parameters.Add("@deleted_by", dto.DeletedByEmployeeId);
                    parameters.Add("@deleted_at", dto.DeletedAt);
                    parameters.Add("@deleted_reason", dto.DeletedReason);

                    var result = await connection.QueryFirstOrDefaultAsync<string>(
                        "tasks.DeleteTask",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return new TaskResponse
                    {
                        Success = true,
                        Message = result ?? "Task deleted successfully"
                    };
                }
                catch (SqlException ex)
                {
                    if (ex.Message.Contains("Task not found"))
                    {
                        return new TaskResponse
                        {
                            Success = false,
                            Message = "Task not found"
                        };
                    }
                    throw new Exception("SQL Error: " + ex.Message, ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error deleting task: " + ex.Message, ex);
                }
            }
        }

        // ============================================================
        // Complete Task (V2 with remarks)
        // ============================================================
        public async Task<CompleteTaskResponseDto> CompleteTaskAsync(CompleteTaskRequestDto dto)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_id", dto.TaskId);
                    parameters.Add("@completion_date", dto.CompletionDate);
                    parameters.Add("@last_modified_by_employee_id", dto.LastModifiedByEmployeeId);
                    parameters.Add("@remarks", dto.Remarks);

                    var result = await connection.QueryFirstOrDefaultAsync<CompleteTaskResponseDto>(
                        "tasks.CompleteTaskV2",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == null)
                        throw new Exception("No response received from stored procedure.");

                    return result;
                }
                catch (SqlException ex)
                {
                    if (ex.Message.Contains("Task not found"))
                        throw new Exception("Task not found.");
                    throw new Exception("SQL Error: " + ex.Message, ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error completing task: " + ex.Message, ex);
                }
            }
        }

        // ============================================================
        // Dashboard (My tasks + Team tasks + Per-member breakdown)
        // ============================================================
        public async Task<TaskDashboardResponseDto> GetTaskDashboardAsync(TaskDashboardRequestDto dto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@employee_id", dto.EmployeeId);
                    parameters.Add("@role_type_id", dto.RoleTypeId);

                    using (var multi = await connection.QueryMultipleAsync(
                        "tasks.GetTaskDashboard",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    ))
                    {
                        var response = new TaskDashboardResponseDto();

                        // Result set 1: My tasks
                        response.MyTasks = await multi.ReadFirstOrDefaultAsync<TaskCountDto>();

                        // Result set 2 & 3: Team tasks (only for HOD/SubHOD)
                        if (dto.RoleTypeId == 1 || dto.RoleTypeId == 2)
                        {
                            if (!multi.IsConsumed)
                                response.TeamTasks = await multi.ReadFirstOrDefaultAsync<TaskCountDto>();
                            if (!multi.IsConsumed)
                                response.TeamMemberBreakdown = (await multi.ReadAsync<TeamMemberTaskCountDto>()).ToList();
                        }

                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching dashboard: {ex.Message}", ex);
            }
        }

        public async Task<TaskReportResponseDto> GetTaskReportAsync(TaskReportRequestDto dto)
        {
            try
            {
                var reportType = (dto.ReportType ?? "WEEKLY").Trim().ToUpperInvariant();
                var (fromDate, toDate) = GetReportRange(reportType);

                var baseFilter = new TaskFilterDto
                {
                    Page = 1,
                    Limit = 5000,
                    Status = dto.Status,
                    Priority = dto.Priority,
                    ProjectName = dto.ProjectName,
                    ModuleName = dto.ModuleName,
                    AssignedTo = dto.AssignedTo,
                    CreatedBy = dto.CreatedBy,
                    DeptId = dto.DeptId,
                    SubDepartmentId = dto.SubDepartmentId,
                    TaskType = dto.TaskType,
                    AssignedToEmployeeId = dto.AssignedToEmployeeId,
                    CreatedByEmployeeId = dto.CreatedByEmployeeId,
                    EmployeeId = dto.EmployeeId,
                    RoleTypeId = dto.RoleTypeId,
                    ViewMode = string.IsNullOrWhiteSpace(dto.ViewMode) ? "OWN" : dto.ViewMode,
                    SortBy = string.IsNullOrWhiteSpace(dto.SortBy) ? "created_on" : dto.SortBy,
                    SortOrder = string.IsNullOrWhiteSpace(dto.SortOrder) ? "DESC" : dto.SortOrder
                };

                var tasks = (await GetAllTasksAsync(baseFilter))
                    .Where(t => t.CreatedOn.Date >= fromDate && t.CreatedOn.Date <= toDate)
                    .OrderByDescending(t => t.CreatedOn)
                    .ToList();

                var completedCount = tasks.Count(t => t.IsCompleted);
                var inProgressCount = tasks.Count(t => string.Equals(t.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase));
                var overdueCount = tasks.Count(t => string.Equals(t.Status, "OVERDUE", StringComparison.OrdinalIgnoreCase));
                var pendingCount = tasks.Count(t =>
                    !t.IsCompleted &&
                    !string.Equals(t.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(t.Status, "OVERDUE", StringComparison.OrdinalIgnoreCase));

                return new TaskReportResponseDto
                {
                    ReportType = reportType,
                    PeriodLabel = GetPeriodLabel(reportType, fromDate, toDate),
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalTasks = tasks.Count,
                    PendingTasks = pendingCount,
                    InProgressTasks = inProgressCount,
                    CompletedTasks = completedCount,
                    OverdueTasks = overdueCount,
                    CompletionRate = tasks.Count == 0 ? 0 : Math.Round((decimal)completedCount * 100 / tasks.Count, 2),
                    Tasks = tasks
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching task report: {ex.Message}", ex);
            }
        }

        // ============================================================
        // Add Progress Update
        // ============================================================
        public async Task<TaskProgressResponseDto> AddProgressUpdateAsync(TaskProgressCreateDto dto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_id", dto.TaskId);
                    parameters.Add("@employee_id", dto.EmployeeId);
                    parameters.Add("@update_text", dto.UpdateText);
                    parameters.Add("@percent_complete", dto.PercentComplete);

                    var result = await connection.QueryFirstOrDefaultAsync<TaskProgressResponseDto>(
                        "tasks.AddTaskProgressUpdate",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == null)
                        throw new Exception("Failed to add progress update.");

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding progress update: {ex.Message}", ex);
            }
        }

        // ============================================================
        // Get Progress Updates for a Task
        // ============================================================
        public async Task<IEnumerable<TaskProgressResponseDto>> GetProgressUpdatesAsync(string taskId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_id", taskId);

                    var result = await connection.QueryAsync<TaskProgressResponseDto>(
                        "tasks.GetTaskProgressUpdates",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching progress updates: {ex.Message}", ex);
            }
        }

        public async Task<TaskDetailsResponseDto?> GetTaskDetailsAsync(string taskId)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@task_id", taskId);

                var task = await connection.QueryFirstOrDefaultAsync<TaskResponseDto>(
                    "tasks.GetTaskDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                if (task == null) return null;

                return new TaskDetailsResponseDto
                {
                    Task = task
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching task details: {ex.Message}", ex);
            }
        }

        // ============================================================
        // Reassign Task (HOD/SubHOD can reassign)
        // ============================================================
        public async Task<TaskResponse> ReassignTaskAsync(ReassignTaskRequestDto dto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_id", dto.TaskId);
                    parameters.Add("@new_assigned_to_employee_id", dto.NewAssignedToEmployeeId);
                    parameters.Add("@reassigned_by_employee_id", dto.ReassignedByEmployeeId);
                    parameters.Add("@notes", dto.Notes);

                    await connection.ExecuteAsync(
                        "tasks.ReassignTask",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return new TaskResponse
                    {
                        Success = true,
                        Message = "Task reassigned successfully"
                    };
                }
            }
            catch (Exception ex)
            {
                return new TaskResponse
                {
                    Success = false,
                    Message = $"Error reassigning task: {ex.Message}"
                };
            }
        }

        private static (DateTime FromDate, DateTime ToDate) GetReportRange(string reportType)
        {
            var today = DateTime.Today;

            return reportType switch
            {
                "WEEKLY" => GetWeeklyRange(today),
                "MONTHLY" => GetMonthlyRange(today),
                _ => throw new ArgumentException("Invalid report type. Use WEEKLY or MONTHLY.")
            };
        }

        private static (DateTime FromDate, DateTime ToDate) GetWeeklyRange(DateTime today)
        {
            var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var fromDate = today.AddDays(-diff).Date;
            var toDate = fromDate.AddDays(6).Date;
            return (fromDate, toDate);
        }

        private static (DateTime FromDate, DateTime ToDate) GetMonthlyRange(DateTime today)
        {
            var fromDate = new DateTime(today.Year, today.Month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);
            return (fromDate, toDate);
        }

        private static string GetPeriodLabel(string reportType, DateTime fromDate, DateTime toDate)
        {
            return reportType switch
            {
                "WEEKLY" => $"{fromDate:dd MMM yyyy} - {toDate:dd MMM yyyy}",
                "MONTHLY" => fromDate.ToString("MMMM yyyy"),
                _ => $"{fromDate:dd MMM yyyy} - {toDate:dd MMM yyyy}"
            };
        }
    }
}

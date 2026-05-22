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
                if (taskDto.AssignedToEmployeeId <= 0
                    && string.Equals(taskDto.TaskType, "SELF", StringComparison.OrdinalIgnoreCase)
                    && taskDto.CreatedByEmployeeId > 0)
                {
                    taskDto.AssignedToEmployeeId = taskDto.CreatedByEmployeeId;
                }

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

                    if (IsSelfSubmittedTask(taskDto))
                    {
                        await MarkTaskSubmittedAsync(connection, result.TaskId, taskDto.CreatedByEmployeeId);
                        result.Status = "SUBMITTED";
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating task: {ex.Message}", ex);
            }
        }

        private static bool IsSelfSubmittedTask(TaskCreateDto taskDto)
        {
            return string.Equals(taskDto.TaskType, "SELF", StringComparison.OrdinalIgnoreCase)
                   && taskDto.CreatedByEmployeeId > 0
                   && taskDto.AssignedToEmployeeId == taskDto.CreatedByEmployeeId;
        }

        private static async Task MarkTaskSubmittedAsync(SqlConnection connection, string? taskId, int employeeId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                return;

            const string updateSql = @"
                UPDATE [tasks].[Tasks]
                SET status = 'SUBMITTED',
                    last_modified_on = GETDATE(),
                    last_modified_by = ISNULL((SELECT EmployeeName FROM [Hie].[Employees] WHERE EmployeeId = @EmployeeId), last_modified_by)
                WHERE task_id = @TaskId
                  AND DeletedAt IS NULL
                  AND ISNULL(is_completed, 0) = 0;";

            await connection.ExecuteAsync(updateSql, new { TaskId = taskId, EmployeeId = employeeId });

            const string historySql = @"
                INSERT INTO [tasks].[TaskAssignmentHistory] (TaskId, FromEmployeeId, ToEmployeeId, ActionType, Notes, ActionDate)
                VALUES (@TaskId, @EmployeeId, @EmployeeId, 'SUBMITTED', 'Task submitted by employee', GETDATE());";

            await connection.ExecuteAsync(historySql, new { TaskId = taskId, EmployeeId = employeeId });
        }

        private static bool IsPendingLikeStatus(string? status)
        {
            return string.Equals(status, "PENDING", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(status, "SUBMITTED", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCompletedLikeStatus(string? status)
        {
            return string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(status, "COMPLETED_AHEAD", StringComparison.OrdinalIgnoreCase);
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
                    var statusFilter = dto.Status?.Trim();
                    var parameters = new DynamicParameters();
                    parameters.Add("@page", dto.Page);
                    parameters.Add("@limit", dto.Limit);
                    parameters.Add("@status", IsPendingLikeStatus(statusFilter) ? null : statusFilter);
                    parameters.Add("@priority", dto.Priority);
                    parameters.Add("@project_name", dto.ProjectName);
                    parameters.Add("@module_name", dto.ModuleName);
                    parameters.Add("@assigned_to_employee_id", null);
                    parameters.Add("@created_by_employee_id", dto.CreatedByEmployeeId);
                    parameters.Add("@dept_id", null);
                    parameters.Add("@sub_department_id", null);
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

                    var allTasks = result.ToList();
                    var originallyUnassignedSelfTaskIds = allTasks
                        .Where(t => !(t.AssignedToEmployeeId > 0)
                                    && string.Equals(t.TaskType, "SELF", StringComparison.OrdinalIgnoreCase)
                                    && !string.IsNullOrWhiteSpace(t.TaskId))
                        .Select(t => t.TaskId!)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    await NormalizeTaskOwnershipAsync(connection, allTasks, dto.EmployeeId);
                    await PersistFallbackOwnershipRepairsAsync(connection, allTasks, originallyUnassignedSelfTaskIds, dto.EmployeeId);

                    var (empByCode, empById, empByName) = await GetEmployeeDeptMapAsync(connection);
                    foreach (var t in allTasks.Where(t => !(t.DeptId > 0)))
                        ResolveDeptFromMap(t, empByCode, empById, empByName);

                    var filtered = allTasks
                        .Where(t =>
                            !(t.IsDeleted ?? false) &&
                            t.DeletedOn == null &&
                            !string.Equals(t.Status, "DELETED", StringComparison.OrdinalIgnoreCase))
                        .Where(t => string.IsNullOrWhiteSpace(statusFilter)
                                    || (IsPendingLikeStatus(statusFilter)
                                        ? IsPendingLikeStatus(t.Status)
                                        : string.Equals(t.Status, statusFilter, StringComparison.OrdinalIgnoreCase)))
                        .Where(t => dto.AssignedToEmployeeId == null || (t.AssignedToEmployeeId ?? 0) == dto.AssignedToEmployeeId.Value)
                        .Where(t => dto.DeptId == null || (t.DeptId ?? 0) == dto.DeptId.Value)
                        .Where(t => dto.SubDepartmentId == null || (t.SubDepartmentId ?? 0) == dto.SubDepartmentId.Value)
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

        private sealed class EmployeeLookupRow
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public int RoleTypeId { get; set; }
            public string RoleName { get; set; } = "";
        }

        private sealed class TeamMemberLookupRow
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = "";
            public int RoleTypeId { get; set; }
        }

        private static async Task NormalizeTaskOwnershipAsync(SqlConnection conn, List<TaskResponseDto> tasks, int? fallbackEmployeeId = null)
        {
            if (tasks.Count == 0) return;

            var needsOwner = tasks
                .Where(t => !(t.AssignedToEmployeeId > 0)
                            && string.Equals(t.TaskType, "SELF", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (needsOwner.Count == 0) return;

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            var ownerIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var task in needsOwner)
            {
                if (task.CreatedByEmployeeId > 0)
                {
                    ownerIds[task.TaskId ?? ""] = task.CreatedByEmployeeId.Value;
                    continue;
                }

                var progressOwner = await GetLatestProgressEmployeeIdAsync(conn, task.TaskId);
                if (progressOwner > 0)
                {
                    ownerIds[task.TaskId ?? ""] = progressOwner.Value;
                    continue;
                }

                if (fallbackEmployeeId > 0)
                    ownerIds[task.TaskId ?? ""] = fallbackEmployeeId.Value;
            }

            var employeeIds = ownerIds.Values.Where(id => id > 0).Distinct().ToArray();
            if (employeeIds.Length == 0) return;

            const string empSql = @"
                SELECT e.EmployeeId,
                       ISNULL(e.EmployeeCode, '') AS EmployeeCode,
                       ISNULL(e.EmployeeName, '') AS EmployeeName,
                       ISNULL(e.RoleTypeId, 0) AS RoleTypeId,
                       ISNULL(rt.RoleName, '') AS RoleName
                FROM [Hie].[Employees] e
                LEFT JOIN [Hie].[RoleTypes] rt ON rt.RoleTypeId = e.RoleTypeId
                WHERE e.EmployeeId IN @EmployeeIds";

            var employees = (await conn.QueryAsync<EmployeeLookupRow>(empSql, new { EmployeeIds = employeeIds }))
                .ToDictionary(e => e.EmployeeId);

            foreach (var task in needsOwner)
            {
                if (string.IsNullOrWhiteSpace(task.TaskId)
                    || !ownerIds.TryGetValue(task.TaskId, out var ownerId)
                    || !employees.TryGetValue(ownerId, out var employee))
                    continue;

                task.AssignedToEmployeeId = employee.EmployeeId;
                task.AssignedTo = employee.EmployeeName;
                task.AssignedToCode = employee.EmployeeCode;
                task.AssignedToRole = string.IsNullOrWhiteSpace(employee.RoleName)
                    ? employee.RoleTypeId.ToString()
                    : employee.RoleName;
            }
        }

        private static async Task<int?> GetLatestProgressEmployeeIdAsync(SqlConnection conn, string? taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId)) return null;

            try
            {
                var updates = (await conn.QueryAsync<TaskProgressResponseDto>(
                    "tasks.GetTaskProgressUpdates",
                    new { task_id = taskId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return updates
                    .OrderByDescending(u => u.UpdateDate)
                    .Select(u => (int?)u.EmployeeId)
                    .FirstOrDefault(id => id > 0);
            }
            catch
            {
                return null;
            }
        }

        private static async Task PersistFallbackOwnershipRepairsAsync(
            SqlConnection conn,
            List<TaskResponseDto> tasks,
            HashSet<string> originallyUnassignedSelfTaskIds,
            int? fallbackEmployeeId)
        {
            if (!(fallbackEmployeeId > 0) || originallyUnassignedSelfTaskIds.Count == 0)
                return;

            var repairs = tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.TaskId)
                            && originallyUnassignedSelfTaskIds.Contains(t.TaskId!)
                            && t.AssignedToEmployeeId == fallbackEmployeeId)
                .Select(t => t.TaskId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var taskId in repairs)
            {
                try
                {
                    var reassignParams = new DynamicParameters();
                    reassignParams.Add("@task_id", taskId);
                    reassignParams.Add("@new_assigned_to_employee_id", fallbackEmployeeId.Value);
                    reassignParams.Add("@reassigned_by_employee_id", fallbackEmployeeId.Value);
                    reassignParams.Add("@notes", "Auto-repaired self task ownership");

                    await conn.ExecuteAsync(
                        "tasks.ReassignTask",
                        reassignParams,
                        commandType: CommandType.StoredProcedure
                    );
                }
                catch
                {
                    // Read-side normalization still fixes the current response even if persistence is unavailable.
                }
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
                var response = new TaskDashboardResponseDto();

                var myTasks = (await GetAllTasksAsync(new TaskFilterDto
                {
                    Page = 1,
                    Limit = 99999,
                    EmployeeId = dto.EmployeeId,
                    RoleTypeId = dto.RoleTypeId,
                    ViewMode = "OWN",
                    SortBy = "created_on",
                    SortOrder = "DESC"
                })).ToList();

                response.MyTasks = BuildTaskCount("MY", myTasks);

                if (dto.RoleTypeId == 1 || dto.RoleTypeId == 2)
                {
                    var teamTasks = (await GetAllTasksAsync(new TaskFilterDto
                    {
                        Page = 1,
                        Limit = 99999,
                        EmployeeId = dto.EmployeeId,
                        RoleTypeId = dto.RoleTypeId,
                        ViewMode = "TEAM",
                        SortBy = "created_on",
                        SortOrder = "DESC"
                    }))
                    .Where(t => (t.AssignedToEmployeeId ?? 0) != dto.EmployeeId)
                    .ToList();

                    response.TeamTasks = BuildTaskCount("TEAM", teamTasks);

                    var countByEmployee = teamTasks
                        .Where(t => (t.AssignedToEmployeeId ?? 0) > 0)
                        .GroupBy(t => t.AssignedToEmployeeId!.Value)
                        .ToDictionary(g => g.Key, g => BuildTaskCount("MEMBER", g));

                    var teamMembers = await GetDashboardTeamMembersAsync(dto.EmployeeId);
                    var knownMemberIds = teamMembers.Select(m => m.EmployeeId).ToHashSet();

                    var taskOnlyMembers = teamTasks
                        .Where(t => (t.AssignedToEmployeeId ?? 0) > 0 && !knownMemberIds.Contains(t.AssignedToEmployeeId!.Value))
                        .GroupBy(t => new
                        {
                            EmployeeId = t.AssignedToEmployeeId ?? 0,
                            EmployeeName = string.IsNullOrWhiteSpace(t.AssignedTo) ? "Unassigned" : t.AssignedTo.Trim(),
                            RoleTypeId = InferRoleTypeId(t.AssignedToRole)
                        })
                        .Select(g => new TeamMemberLookupRow
                        {
                            EmployeeId = g.Key.EmployeeId,
                            EmployeeName = g.Key.EmployeeName,
                            RoleTypeId = g.Key.RoleTypeId
                        })
                        .ToList();

                    teamMembers.AddRange(taskOnlyMembers);
                    response.TeamMemberBreakdown = teamMembers
                        .GroupBy(m => m.EmployeeId)
                        .Select(g => g.First())
                        .OrderBy(m => m.EmployeeName)
                        .Select(m =>
                        {
                            countByEmployee.TryGetValue(m.EmployeeId, out var counts);
                            counts ??= BuildTaskCount("MEMBER", Enumerable.Empty<TaskResponseDto>());
                            return new TeamMemberTaskCountDto
                            {
                                EmployeeId = m.EmployeeId,
                                EmployeeName = m.EmployeeName,
                                RoleTypeId = m.RoleTypeId,
                                TotalTasks = counts.TotalTasks,
                                PendingCount = counts.PendingCount,
                                InProgressCount = counts.InProgressCount,
                                CompletedCount = counts.CompletedCount,
                                OverdueCount = counts.OverdueCount
                            };
                        })
                        .ToList();
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching dashboard: {ex.Message}", ex);
            }
        }

        private static TaskCountDto BuildTaskCount(string section, IEnumerable<TaskResponseDto> tasks)
        {
            var list = tasks.ToList();
            return new TaskCountDto
            {
                Section = section,
                TotalTasks = list.Count,
                PendingCount = list.Count(t => IsPendingLikeStatus(t.Status)),
                InProgressCount = list.Count(t => string.Equals(t.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase)),
                CompletedCount = list.Count(t => t.IsCompleted || IsCompletedLikeStatus(t.Status)),
                OverdueCount = list.Count(t => string.Equals(t.Status, "OVERDUE", StringComparison.OrdinalIgnoreCase)),
                SelfTaskCount = list.Count(t => string.Equals(t.TaskType, "SELF", StringComparison.OrdinalIgnoreCase)),
                AssignedTaskCount = list.Count(t => !string.Equals(t.TaskType, "SELF", StringComparison.OrdinalIgnoreCase))
            };
        }

        private async Task<List<TeamMemberLookupRow>> GetDashboardTeamMembersAsync(int managerEmployeeId)
        {
            const string sql = @"
                ;WITH Team AS
                (
                    SELECT
                        e.EmployeeId,
                        ISNULL(e.EmployeeName, '') AS EmployeeName,
                        ISNULL(e.RoleTypeId, 3) AS RoleTypeId,
                        1 AS Depth
                    FROM [Hie].[EmployeeReportingRelationships] rr
                    INNER JOIN [Hie].[Employees] e
                            ON e.EmployeeId = rr.EmployeeId
                           AND e.IsActive = 1
                    WHERE rr.IsActive = 1
                      AND rr.ReportsToEmployeeId = @ManagerEmployeeId

                    UNION ALL

                    SELECT
                        e.EmployeeId,
                        ISNULL(e.EmployeeName, '') AS EmployeeName,
                        ISNULL(e.RoleTypeId, 3) AS RoleTypeId,
                        t.Depth + 1
                    FROM [Hie].[EmployeeReportingRelationships] rr
                    INNER JOIN Team t
                            ON t.EmployeeId = rr.ReportsToEmployeeId
                    INNER JOIN [Hie].[Employees] e
                            ON e.EmployeeId = rr.EmployeeId
                           AND e.IsActive = 1
                    WHERE rr.IsActive = 1
                      AND t.Depth < 10
                )
                SELECT EmployeeId, MAX(EmployeeName) AS EmployeeName, MAX(RoleTypeId) AS RoleTypeId
                FROM Team
                WHERE EmployeeId <> @ManagerEmployeeId
                GROUP BY EmployeeId
                OPTION (MAXRECURSION 10);";

            await using var connection = new SqlConnection(_connectionString);
            var rows = await connection.QueryAsync<TeamMemberLookupRow>(sql, new { ManagerEmployeeId = managerEmployeeId });
            return rows
                .Where(r => r.EmployeeId > 0)
                .Select(r =>
                {
                    r.EmployeeName = string.IsNullOrWhiteSpace(r.EmployeeName) ? $"Employee {r.EmployeeId}" : r.EmployeeName.Trim();
                    return r;
                })
                .ToList();
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

                var completedCount = tasks.Count(t => t.IsCompleted || IsCompletedLikeStatus(t.Status));
                var inProgressCount = tasks.Count(t => string.Equals(t.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase));
                var overdueCount = tasks.Count(t => string.Equals(t.Status, "OVERDUE", StringComparison.OrdinalIgnoreCase));
                var pendingCount = tasks.Count(t =>
                    !t.IsCompleted &&
                    !IsCompletedLikeStatus(t.Status) &&
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
                    var currentTask = await connection.QueryFirstOrDefaultAsync<TaskResponseDto>(
                        "tasks.GetTaskDetails",
                        new { task_id = dto.TaskId },
                        commandType: CommandType.StoredProcedure
                    );

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

                    if (dto.EmployeeId > 0 && !(currentTask?.AssignedToEmployeeId > 0))
                    {
                        try
                        {
                            var reassignParams = new DynamicParameters();
                            reassignParams.Add("@task_id", dto.TaskId);
                            reassignParams.Add("@new_assigned_to_employee_id", dto.EmployeeId);
                            reassignParams.Add("@reassigned_by_employee_id", dto.EmployeeId);
                            reassignParams.Add("@notes", "Auto-assigned from progress update");

                            await connection.ExecuteAsync(
                                "tasks.ReassignTask",
                                reassignParams,
                                commandType: CommandType.StoredProcedure
                            );
                        }
                        catch
                        {
                            // Keep the progress update successful even if legacy reassignment repair is unavailable.
                        }
                    }

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

        // ============================================================
        // Owner Dashboard — department-level aggregation
        // ============================================================
        public async Task<OwnerDashboardDto> GetOwnerDashboardAsync(DateTime? fromDate, DateTime? toDate)
        {
            // Use the proven GetAllTasksV2 SP (admin/all view) then aggregate in C#
            var parameters = new DynamicParameters();
            parameters.Add("@page", 1);
            parameters.Add("@limit", 99999);
            parameters.Add("@status", null);
            parameters.Add("@priority", null);
            parameters.Add("@project_name", null);
            parameters.Add("@module_name", null);
            parameters.Add("@assigned_to_employee_id", null);
            parameters.Add("@created_by_employee_id", null);
            parameters.Add("@dept_id", null);
            parameters.Add("@sub_department_id", null);
            parameters.Add("@task_type", null);
            parameters.Add("@employee_id", 0);
            parameters.Add("@role_type_id", 0);
            parameters.Add("@view_mode", "ALL");
            parameters.Add("@sort_by", "created_on");
            parameters.Add("@sort_order", "DESC");

            await using var conn = new SqlConnection(_connectionString);
            var allTasks = (await conn.QueryAsync<TaskResponseDto>(
                "tasks.GetAllTasksV2",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();
            await NormalizeTaskOwnershipAsync(conn, allTasks);

            // Resolve employee→department mapping for tasks that have no dept_id
            var (empByCode, empById, empByName) = await GetEmployeeDeptMapAsync(conn);
            foreach (var t in allTasks.Where(t => !(t.DeptId > 0)))
                ResolveDeptFromMap(t, empByCode, empById, empByName);

            // Filter out deleted tasks and apply date range
            var tasks = allTasks
                .Where(t => !(t.IsDeleted ?? false) && t.DeletedOn == null)
                .Where(t => fromDate == null || t.CreatedOn >= fromDate)
                .Where(t => toDate == null || t.CreatedOn < toDate.Value.AddDays(1))
                .ToList();

            // Aggregate by department
            var depts = tasks
                .GroupBy(t => new {
                    DeptId   = t.DeptId ?? 0,
                    DeptName = string.IsNullOrWhiteSpace(t.DeptName) ? "No Department" : t.DeptName.Trim()
                })
                .Select(g => new DeptTaskSummaryDto
                {
                    DeptId          = g.Key.DeptId,
                    DeptName        = g.Key.DeptName,
                    TotalTasks      = g.Count(),
                    PendingCount    = g.Count(t => IsPendingLikeStatus(t.Status)),
                    InProgressCount = g.Count(t => string.Equals(t.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase)),
                    CompletedCount  = g.Count(t => t.IsCompleted || IsCompletedLikeStatus(t.Status)),
                    OverdueCount    = g.Count(t => string.Equals(t.Status, "OVERDUE", StringComparison.OrdinalIgnoreCase)),
                    AvgProgress     = Math.Round(g.Average(t => (double)t.PercentComplete), 1)
                })
                .OrderByDescending(d => d.TotalTasks)
                .ToList();

            return new OwnerDashboardDto
            {
                TotalTasks      = depts.Sum(d => d.TotalTasks),
                PendingCount    = depts.Sum(d => d.PendingCount),
                InProgressCount = depts.Sum(d => d.InProgressCount),
                CompletedCount  = depts.Sum(d => d.CompletedCount),
                OverdueCount    = depts.Sum(d => d.OverdueCount),
                ActiveDepts     = depts.Count(d => d.TotalTasks > 0),
                AvgProgress     = depts.Count > 0 ? Math.Round(depts.Average(d => d.AvgProgress), 1) : 0,
                Departments     = depts
            };
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

        // ============================================================
        // Dept Employee Detail — employee-level performance per dept(s)
        // ============================================================
        public async Task<List<EmployeeTaskSummaryDto>> GetDeptEmployeeDetailAsync(
            List<int> deptIds, DateTime? fromDate, DateTime? toDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@page", 1);
            parameters.Add("@limit", 99999);
            parameters.Add("@status", null);
            parameters.Add("@priority", null);
            parameters.Add("@project_name", null);
            parameters.Add("@module_name", null);
            parameters.Add("@assigned_to_employee_id", null);
            parameters.Add("@created_by_employee_id", null);
            parameters.Add("@dept_id", null);
            parameters.Add("@sub_department_id", null);
            parameters.Add("@task_type", null);
            parameters.Add("@employee_id", 0);
            parameters.Add("@role_type_id", 0);
            parameters.Add("@view_mode", "ALL");
            parameters.Add("@sort_by", "created_on");
            parameters.Add("@sort_order", "DESC");

            await using var conn = new SqlConnection(_connectionString);
            var allTasks = (await conn.QueryAsync<TaskResponseDto>(
                "tasks.GetAllTasksV2",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();

            // Resolve employee→department for tasks with no dept_id
            await NormalizeTaskOwnershipAsync(conn, allTasks);
            var (empByCode2, empById2, empByName2) = await GetEmployeeDeptMapAsync(conn);
            foreach (var t in allTasks.Where(t => !(t.DeptId > 0)))
                ResolveDeptFromMap(t, empByCode2, empById2, empByName2);

            var tasks = allTasks
                .Where(t => !(t.IsDeleted ?? false) && t.DeletedOn == null)
                .Where(t => !deptIds.Any() || deptIds.Contains(t.DeptId ?? 0))
                .Where(t => fromDate == null || t.CreatedOn >= fromDate)
                .Where(t => toDate == null || t.CreatedOn < toDate.Value.AddDays(1))
                .ToList();

            return tasks
                .GroupBy(t => new {
                    EmpId   = t.AssignedToEmployeeId ?? 0,
                    EmpName = string.IsNullOrWhiteSpace(t.AssignedTo) ? "Unknown" : t.AssignedTo.Trim(),
                    Role    = t.AssignedToRole ?? "",
                    DeptId  = t.DeptId ?? 0,
                    DeptName= string.IsNullOrWhiteSpace(t.DeptName) ? "No Department" : t.DeptName.Trim()
                })
                .Select(g => new EmployeeTaskSummaryDto
                {
                    EmployeeId      = g.Key.EmpId,
                    EmployeeName    = g.Key.EmpName,
                    Role            = g.Key.Role,
                    DeptId          = g.Key.DeptId,
                    DeptName        = g.Key.DeptName,
                    TotalTasks      = g.Count(),
                    PendingCount    = g.Count(t => IsPendingLikeStatus(t.Status)),
                    InProgressCount = g.Count(t => string.Equals(t.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase)),
                    CompletedCount  = g.Count(t => t.IsCompleted || IsCompletedLikeStatus(t.Status)),
                    OverdueCount    = g.Count(t => string.Equals(t.Status, "OVERDUE",     StringComparison.OrdinalIgnoreCase)),
                    AvgProgress     = Math.Round(g.Average(t => (double)t.PercentComplete), 1)
                })
                .OrderByDescending(e => e.CompletionRate)
                .ThenByDescending(e => e.TotalTasks)
                .ToList();
        }

        // Shared helper: map EmployeeCode and EmployeeId → (DeptId, DeptName)
        // Hierarchy: Employee → EmployeeReportingRelationships.SubDepartmentId → SubDepartments.DepartmentId → Departments
        // HODs may link directly via EmployeeReportingRelationships.DepartmentId (no sub-dept)
        private static async Task<(
            Dictionary<string, (int DeptId, string DeptName)> ByCode,
            Dictionary<int, (int DeptId, string DeptName)> ById,
            Dictionary<string, (int DeptId, string DeptName)> ByName)> GetEmployeeDeptMapAsync(SqlConnection conn)
        {
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                // Resolves dept for all three role levels:
                //   Executive  → err.SubDepartmentId → sd.DepartmentId → d.DepartmentName
                //   SubHOD     → err.SubDepartmentId → sd.DepartmentId → d.DepartmentName
                //   HOD        → err.DepartmentId    → d.DepartmentName  (no sub-dept row)
                const string sql = @"
                    SELECT DISTINCT
                        e.EmployeeId,
                        e.EmployeeName,
                        ISNULL(e.EmployeeCode, '') AS EmployeeCode,
                        COALESCE(sd.DepartmentId, err.DepartmentId, e.PrimaryDepartmentId) AS DeptId,
                        ISNULL(COALESCE(d_sub.DepartmentName, d_dir.DepartmentName, d_prim.DepartmentName), '') AS DeptName
                    FROM [Hie].[Employees] e
                    INNER JOIN [Hie].[EmployeeReportingRelationships] err
                           ON  err.EmployeeId = e.EmployeeId AND err.IsActive = 1
                    LEFT  JOIN [Hie].[SubDepartments] sd
                           ON  sd.SubDepartmentId = err.SubDepartmentId
                    LEFT  JOIN [Hie].[Departments] d_sub
                           ON  d_sub.DepartmentId = sd.DepartmentId
                    LEFT  JOIN [Hie].[Departments] d_dir
                           ON  d_dir.DepartmentId = err.DepartmentId
                    LEFT  JOIN [Hie].[Departments] d_prim
                           ON  d_prim.DepartmentId = e.PrimaryDepartmentId
                    WHERE e.IsActive = 1
                      AND COALESCE(sd.DepartmentId, err.DepartmentId, e.PrimaryDepartmentId) IS NOT NULL";

                var rows = (await conn.QueryAsync<(int EmployeeId, string EmployeeName, string EmployeeCode, int DeptId, string DeptName)>(sql)).ToList();

                var byCode = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeCode))
                    .GroupBy(r => r.EmployeeCode, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => (g.First().DeptId, g.First().DeptName), StringComparer.OrdinalIgnoreCase);

                var byId = rows
                    .GroupBy(r => r.EmployeeId)
                    .ToDictionary(g => g.Key, g => (g.First().DeptId, g.First().DeptName));

                var byName = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeName))
                    .GroupBy(r => NormalizeEmployeeName(r.EmployeeName), StringComparer.OrdinalIgnoreCase)
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .ToDictionary(g => g.Key, g => (g.First().DeptId, g.First().DeptName), StringComparer.OrdinalIgnoreCase);

                return (byCode, byId, byName);
            }
            catch
            {
                return (new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase),
                        new Dictionary<int, (int, string)>(),
                        new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase));
            }
        }

        private static void ResolveDeptFromMap(
            TaskResponseDto t,
            Dictionary<string, (int DeptId, string DeptName)> byCode,
            Dictionary<int, (int DeptId, string DeptName)> byId,
            Dictionary<string, (int DeptId, string DeptName)> byName)
        {
            if (t.DeptId > 0) return;

            (int DeptId, string DeptName) d = default;
            bool found = false;

            if (!string.IsNullOrWhiteSpace(t.AssignedToCode) && byCode.TryGetValue(t.AssignedToCode, out d))
                found = true;
            else if (t.AssignedToEmployeeId > 0 && byId.TryGetValue(t.AssignedToEmployeeId!.Value, out d))
                found = true;
            else if (!string.IsNullOrWhiteSpace(t.AssignedTo) && byName.TryGetValue(NormalizeEmployeeName(t.AssignedTo), out d))
                found = true;

            if (found)
            { t.DeptId = d.DeptId; t.DeptName = d.DeptName; }
        }

        private static string NormalizeEmployeeName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(value, @"\s*\([^)]*\)", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
            return cleaned;
        }

        private static bool TaskEmployeeExistsInHierarchy(
            TaskResponseDto task,
            HashSet<int> existingEmpIds,
            HashSet<string> existingEmpCodes,
            HashSet<string> existingEmpNames)
        {
            if (task.AssignedToEmployeeId > 0 && existingEmpIds.Contains(task.AssignedToEmployeeId.Value))
                return true;

            if (!string.IsNullOrWhiteSpace(task.AssignedToCode) && existingEmpCodes.Contains(task.AssignedToCode))
                return true;

            var normalizedName = NormalizeEmployeeName(task.AssignedTo);
            return !string.IsNullOrWhiteSpace(normalizedName) && existingEmpNames.Contains(normalizedName);
        }

        private static int InferRoleTypeId(string? roleName)
        {
            var role = (roleName ?? "").Trim();
            if (role.Equals("HOD", StringComparison.OrdinalIgnoreCase)
                || role.Contains("Head", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (role.Contains("Sub", StringComparison.OrdinalIgnoreCase))
                return 2;

            return 3;
        }

        // ============================================================
        // Dept Hierarchy — HOD → SubHOD → Executive tree with task counts
        // ============================================================
        public async Task<DeptHierarchyResponseDto> GetDeptHierarchyAsync(int deptId, DateTime? fromDate, DateTime? toDate)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Dept name
            var deptName = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 DepartmentName FROM [Hie].[Departments] WHERE DepartmentId = @DeptId",
                new { DeptId = deptId }) ?? "Department";

            // All employees in this dept with their hierarchy positions.
            // Uses COALESCE(sd.DepartmentId, err.DepartmentId) — same pattern as GetEmployeeDeptMapAsync
            // so it works for HODs (direct DepartmentId) and SubHODs/Execs (via SubDepartment).
            // UNION approach:
            //   Part 1 – Employees linked via ERR → Dept/SubDept
            //   Part 2 – HODs linked by Employees.PrimaryDepartmentId
            //   Part 3 – Parent HODs referenced by SubHOD/Executive rows in this dept
            const string empSql = @"
                SELECT DISTINCT
                    e.EmployeeId,
                    ISNULL(e.EmployeeCode,   '')     AS EmployeeCode,
                    ISNULL(e.EmployeeName,   '')     AS EmployeeName,
                    e.RoleTypeId,
                    ISNULL(rt.RoleName,      '')     AS RoleName,
                    err.ReportsToEmployeeId,
                    err.SubDepartmentId,
                    ISNULL(sd.SubDepartmentName, '') AS SubDepartmentName
                FROM [Hie].[Employees] e
                INNER JOIN [Hie].[EmployeeReportingRelationships] err
                       ON  err.EmployeeId = e.EmployeeId AND err.IsActive = 1
                LEFT  JOIN [Hie].[RoleTypes] rt  ON rt.RoleTypeId  = e.RoleTypeId
                LEFT  JOIN [Hie].[SubDepartments] sd ON sd.SubDepartmentId = err.SubDepartmentId
                WHERE e.IsActive = 1
                  AND COALESCE(sd.DepartmentId, err.DepartmentId) = @DeptId

                UNION

                SELECT DISTINCT
                    e.EmployeeId,
                    ISNULL(e.EmployeeCode,   '')     AS EmployeeCode,
                    ISNULL(e.EmployeeName,   '')     AS EmployeeName,
                    e.RoleTypeId,
                    ISNULL(rt.RoleName,      '')     AS RoleName,
                    NULL                             AS ReportsToEmployeeId,
                    NULL                             AS SubDepartmentId,
                    ''                               AS SubDepartmentName
                FROM [Hie].[Employees] e
                LEFT  JOIN [Hie].[RoleTypes] rt  ON rt.RoleTypeId  = e.RoleTypeId
                WHERE e.IsActive = 1
                  AND e.RoleTypeId = 1
                  AND e.PrimaryDepartmentId = @DeptId

                UNION

                SELECT DISTINCT
                    h.EmployeeId,
                    ISNULL(h.EmployeeCode,   '')     AS EmployeeCode,
                    ISNULL(h.EmployeeName,   '')     AS EmployeeName,
                    h.RoleTypeId,
                    ISNULL(rt.RoleName,      '')     AS RoleName,
                    NULL                             AS ReportsToEmployeeId,
                    NULL                             AS SubDepartmentId,
                    ''                               AS SubDepartmentName
                FROM [Hie].[EmployeeReportingRelationships] childErr
                INNER JOIN [Hie].[Employees] h
                        ON h.EmployeeId = childErr.ReportsToEmployeeId
                       AND h.IsActive = 1
                       AND h.RoleTypeId = 1
                LEFT  JOIN [Hie].[RoleTypes] rt
                        ON rt.RoleTypeId = h.RoleTypeId
                LEFT  JOIN [Hie].[SubDepartments] sd
                        ON sd.SubDepartmentId = childErr.SubDepartmentId
                WHERE childErr.IsActive = 1
                  AND COALESCE(sd.DepartmentId, childErr.DepartmentId) = @DeptId";

            var empRows = (await conn.QueryAsync<(int EmployeeId, string EmployeeCode, string EmployeeName,
                int RoleTypeId, string RoleName, int? ReportsToEmployeeId,
                int? SubDepartmentId, string SubDepartmentName)>(empSql, new { DeptId = deptId })).ToList();

            // All tasks (date-filtered, not deleted)
            var tp = new DynamicParameters();
            tp.Add("@page", 1);             tp.Add("@limit", 99999);
            tp.Add("@status", null);        tp.Add("@priority", null);
            tp.Add("@project_name", null);  tp.Add("@module_name", null);
            tp.Add("@assigned_to_employee_id", null); tp.Add("@created_by_employee_id", null);
            tp.Add("@dept_id", null);       tp.Add("@sub_department_id", null);
            tp.Add("@task_type", null);
            tp.Add("@employee_id", 0);      tp.Add("@role_type_id", 0);
            tp.Add("@view_mode", "ALL");
            tp.Add("@sort_by", "created_on"); tp.Add("@sort_order", "DESC");

            var allTasks = (await conn.QueryAsync<TaskResponseDto>(
                "tasks.GetAllTasksV2", tp, commandType: CommandType.StoredProcedure)).ToList();

            await NormalizeTaskOwnershipAsync(conn, allTasks);
            var (empByCode, empById, empByName) = await GetEmployeeDeptMapAsync(conn);
            foreach (var t in allTasks.Where(t => !(t.DeptId > 0)))
                ResolveDeptFromMap(t, empByCode, empById, empByName);

            var tasks = allTasks
                .Where(t => !(t.IsDeleted ?? false) && t.DeletedOn == null)
                .Where(t => (t.DeptId ?? 0) == deptId)
                .Where(t => fromDate == null || t.CreatedOn >= fromDate)
                .Where(t => toDate == null || t.CreatedOn < toDate.Value.AddDays(1))
                .ToList();

            var existingEmpIds = empRows.Select(e => e.EmployeeId).ToHashSet();
            var existingEmpCodes = new HashSet<string>(
                empRows.Select(e => e.EmployeeCode).Where(c => !string.IsNullOrWhiteSpace(c)),
                StringComparer.OrdinalIgnoreCase);
            var existingEmpNames = new HashSet<string>(
                empRows.Select(e => NormalizeEmployeeName(e.EmployeeName)).Where(n => !string.IsNullOrWhiteSpace(n)),
                StringComparer.OrdinalIgnoreCase);

            var missingTaskEmployees = tasks
                .Where(t => !TaskEmployeeExistsInHierarchy(t, existingEmpIds, existingEmpCodes, existingEmpNames))
                .GroupBy(t => new
                {
                    EmployeeId = t.AssignedToEmployeeId ?? 0,
                    EmployeeCode = t.AssignedToCode ?? "",
                    EmployeeName = string.IsNullOrWhiteSpace(t.AssignedTo) ? "Unmapped Employee" : t.AssignedTo.Trim(),
                    RoleName = string.IsNullOrWhiteSpace(t.AssignedToRole) ? "Executive" : t.AssignedToRole.Trim()
                })
                .Select(g => (
                    g.Key.EmployeeId,
                    g.Key.EmployeeCode,
                    g.Key.EmployeeName,
                    RoleTypeId: InferRoleTypeId(g.Key.RoleName),
                    g.Key.RoleName,
                    ReportsToEmployeeId: (int?)null,
                    SubDepartmentId: (int?)null,
                    SubDepartmentName: "Assigned in this department"))
                .ToList();

            if (missingTaskEmployees.Count > 0)
                empRows.AddRange(missingTaskEmployees);

            // Task lookups by employee code (primary) and ID (fallback)
            var tasksByCode = tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.AssignedToCode))
                .GroupBy(t => t.AssignedToCode!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var tasksById = tasks
                .Where(t => t.AssignedToEmployeeId > 0)
                .GroupBy(t => t.AssignedToEmployeeId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tasksByName = tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.AssignedTo))
                .GroupBy(t => NormalizeEmployeeName(t.AssignedTo), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            HierarchyEmpNodeDto BuildNode(int empId, string empCode, string empName,
                int roleTypeId, string roleName, int? subDeptId, string subDeptName)
            {
                var empTasks =
                    (!string.IsNullOrWhiteSpace(empCode) && tasksByCode.TryGetValue(empCode, out var bc)) ? bc :
                    (tasksById.TryGetValue(empId, out var bi) ? bi :
                    (tasksByName.TryGetValue(NormalizeEmployeeName(empName), out var bn) ? bn : new List<TaskResponseDto>()));

                return new HierarchyEmpNodeDto
                {
                    EmployeeId       = empId,
                    EmployeeCode     = empCode,
                    EmployeeName     = empName,
                    RoleTypeId       = roleTypeId,
                    RoleName         = roleName,
                    SubDepartmentId  = subDeptId,
                    SubDepartmentName = subDeptName,
                    TotalTasks       = empTasks.Count,
                    PendingCount     = empTasks.Count(t => IsPendingLikeStatus(t.Status)),
                    InProgressCount  = empTasks.Count(t => string.Equals(t.Status, "IN_PROGRESS",  StringComparison.OrdinalIgnoreCase)),
                    CompletedCount   = empTasks.Count(t => t.IsCompleted || IsCompletedLikeStatus(t.Status)),
                    OverdueCount     = empTasks.Count(t => string.Equals(t.Status, "OVERDUE",      StringComparison.OrdinalIgnoreCase)),
                    AvgProgress      = empTasks.Count > 0 ? Math.Round(empTasks.Average(t => (double)t.PercentComplete), 1) : 0
                };
            }

            // Deduplicate by EmployeeId (an employee may appear in multiple relationships)
            var hodRows    = empRows.Where(r => r.RoleTypeId == 1).GroupBy(r => r.EmployeeId).Select(g => g.First()).ToList();
            var subHodRows = empRows.Where(r => r.RoleTypeId == 2).GroupBy(r => r.EmployeeId).Select(g => g.First()).ToList();
            var execRows   = empRows.Where(r => r.RoleTypeId == 3).GroupBy(r => r.EmployeeId).Select(g => g.First()).ToList();

            var hodIds    = hodRows.Select(h => h.EmployeeId).ToHashSet();
            var subHodIds = subHodRows.Select(s => s.EmployeeId).ToHashSet();

            // Build HOD → SubHOD → Executive tree
            var hodNodes = hodRows.Select(hod =>
            {
                var hodNode = BuildNode(hod.EmployeeId, hod.EmployeeCode, hod.EmployeeName,
                    hod.RoleTypeId, hod.RoleName, hod.SubDepartmentId, hod.SubDepartmentName);

                // SubHODs whose ReportsToEmployeeId points to this HOD
                var mySubHods = subHodRows.Where(s => s.ReportsToEmployeeId == hod.EmployeeId).ToList();

                // Fallback: if no SubHOD has ReportsToEmployeeId set correctly but there is only
                // one HOD, assign all SubHODs to that HOD
                if (!mySubHods.Any() && hodRows.Count == 1)
                    mySubHods = subHodRows;

                hodNode.Children = mySubHods.Select(sub =>
                {
                    var subNode = BuildNode(sub.EmployeeId, sub.EmployeeCode, sub.EmployeeName,
                        sub.RoleTypeId, sub.RoleName, sub.SubDepartmentId, sub.SubDepartmentName);

                    // Execs whose ReportsToEmployeeId points to this SubHOD
                    var myExecs = execRows.Where(ex => ex.ReportsToEmployeeId == sub.EmployeeId).ToList();

                    // Fallback: if no exec is linked but there is only one SubHOD, assign all execs to it
                    if (!myExecs.Any() && mySubHods.Count == 1)
                        myExecs = execRows.Where(ex => !subHodIds.Contains(ex.ReportsToEmployeeId ?? 0) && !hodIds.Contains(ex.ReportsToEmployeeId ?? 0)).ToList();

                    subNode.Children = myExecs
                        .Select(ex => BuildNode(ex.EmployeeId, ex.EmployeeCode, ex.EmployeeName,
                            ex.RoleTypeId, ex.RoleName, ex.SubDepartmentId, ex.SubDepartmentName))
                        .ToList();

                    return subNode;
                }).ToList();

                // Executives that report directly to HOD (no SubHOD in between)
                var directExecs = execRows.Where(ex => ex.ReportsToEmployeeId == hod.EmployeeId).ToList();
                if (hodRows.Count == 1)
                {
                    var existingDirectIds = directExecs.Select(ex => ex.EmployeeId).ToHashSet();
                    directExecs.AddRange(execRows.Where(ex =>
                        ex.ReportsToEmployeeId == null
                        && !existingDirectIds.Contains(ex.EmployeeId)));
                }

                hodNode.Children.AddRange(directExecs.Select(ex =>
                    BuildNode(ex.EmployeeId, ex.EmployeeCode, ex.EmployeeName,
                        ex.RoleTypeId, ex.RoleName, ex.SubDepartmentId, ex.SubDepartmentName)));

                return hodNode;
            }).ToList();

            // If no HODs found but SubHODs/Execs exist, surface them directly
            if (!hodNodes.Any() && subHodRows.Any())
            {
                var orphanSubHodNode = new HierarchyEmpNodeDto
                {
                    EmployeeId = 0, EmployeeName = "Sub-HOD Teams", RoleTypeId = 1, RoleName = "HOD",
                    Children = subHodRows.Select(sub =>
                    {
                        var subNode = BuildNode(sub.EmployeeId, sub.EmployeeCode, sub.EmployeeName,
                            sub.RoleTypeId, sub.RoleName, sub.SubDepartmentId, sub.SubDepartmentName);
                        subNode.Children = execRows
                            .Where(ex => ex.ReportsToEmployeeId == sub.EmployeeId)
                            .Select(ex => BuildNode(ex.EmployeeId, ex.EmployeeCode, ex.EmployeeName,
                                ex.RoleTypeId, ex.RoleName, ex.SubDepartmentId, ex.SubDepartmentName))
                            .ToList();
                        return subNode;
                    }).ToList()
                };
                hodNodes.Add(orphanSubHodNode);
            }
            else if (!hodNodes.Any() && execRows.Any())
            {
                // Only executives, no HOD/SubHOD — surface them directly
                hodNodes.AddRange(execRows.Select(ex =>
                    BuildNode(ex.EmployeeId, ex.EmployeeCode, ex.EmployeeName,
                        ex.RoleTypeId, ex.RoleName, ex.SubDepartmentId, ex.SubDepartmentName)));
            }

            var deptTasks = tasks;

            return new DeptHierarchyResponseDto
            {
                DeptId          = deptId,
                DeptName        = deptName,
                TotalTasks      = deptTasks.Count,
                PendingCount    = deptTasks.Count(t => IsPendingLikeStatus(t.Status)),
                InProgressCount = deptTasks.Count(t => string.Equals(t.Status, "IN_PROGRESS",  StringComparison.OrdinalIgnoreCase)),
                CompletedCount  = deptTasks.Count(t => t.IsCompleted || IsCompletedLikeStatus(t.Status)),
                OverdueCount    = deptTasks.Count(t => string.Equals(t.Status, "OVERDUE",      StringComparison.OrdinalIgnoreCase)),
                AvgProgress     = deptTasks.Count > 0 ? Math.Round(deptTasks.Average(t => (double)t.PercentComplete), 1) : 0,
                Hods            = hodNodes
            };
        }
    }
}

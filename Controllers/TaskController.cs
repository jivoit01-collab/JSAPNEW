using JSAPNEW.Models;
using JSAPNEW.Services;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JSAPNEW.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IHierarchyService _hierarchyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TaskController> _logger;

        public TaskController(
            ITaskService taskService,
            IHierarchyService hierarchyService,
            ILogger<TaskController> logger,
            IConfiguration configuration)
        {
            _taskService = taskService;
            _hierarchyService = hierarchyService;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<int?> ResolveLoggedInEmployeeIdAsync()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (!userId.HasValue || userId.Value <= 0)
                return null;

            try
            {
                string? employeeCode = null;
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                await using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string query = "SELECT empId FROM jsUser WHERE userId = @UserId";
                    await using var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", userId.Value);

                    var result = await command.ExecuteScalarAsync();
                    employeeCode = result?.ToString();
                }

                if (string.IsNullOrWhiteSpace(employeeCode))
                    return null;

                var employee = await _hierarchyService.GetEmployeeByCodeAsync(employeeCode);
                return employee?.EmployeeId > 0 ? employee.EmployeeId : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to resolve employee id for logged-in user {UserId}", userId.Value);
                return null;
            }
        }

        // ============================================================
        // Create Task (Self or Assigned by HOD/SubHOD)
        // ============================================================
        [HttpPost("CreateTask")]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto taskDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Auto-detect task type
                if (taskDto.AssignedToEmployeeId != taskDto.CreatedByEmployeeId)
                {
                    taskDto.TaskType = "ASSIGNED";
                    taskDto.AssignedByEmployeeId = taskDto.CreatedByEmployeeId;
                }
                else
                {
                    taskDto.TaskType = "SELF";
                }

                var result = await _taskService.CreateTaskAsync(taskDto);

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new { Success = false, Message = "An error occurred while creating the task." });
            }
        }

        // ============================================================
        // Get All Tasks (Hierarchy-aware: OWN / TEAM / ALL)
        // ============================================================
        [HttpPost("GetAllTasks")]
        public async Task<IActionResult> GetAllTasks([FromBody] TaskFilterDto filter)
        {
            if (filter == null)
                return BadRequest("Request body cannot be null.");

            try
            {
                var result = await _taskService.GetAllTasksAsync(filter);

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching tasks.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        // ============================================================
        // Delete Task
        // ============================================================
        [HttpPost("DeleteTask")]
        public async Task<IActionResult> DeleteTask([FromBody] DeleteTaskRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TaskId))
                return BadRequest("TaskId is required.");

            if (!dto.DeletedByEmployeeId.HasValue || dto.DeletedByEmployeeId.Value <= 0)
                dto.DeletedByEmployeeId = await ResolveLoggedInEmployeeIdAsync();

            if (!dto.DeletedAt.HasValue)
                dto.DeletedAt = DateTime.Now;

            if (!dto.DeletedByEmployeeId.HasValue || dto.DeletedByEmployeeId.Value <= 0)
                return BadRequest("DeletedByEmployeeId is required.");

            try
            {
                var response = await _taskService.DeleteTaskAsync(dto);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        // ============================================================
        // Complete Task
        // ============================================================
        [HttpPost("CompleteTask")]
        public async Task<IActionResult> CompleteTask([FromBody] CompleteTaskRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TaskId))
                return BadRequest("TaskId is required.");

            if (!dto.LastModifiedByEmployeeId.HasValue || dto.LastModifiedByEmployeeId.Value <= 0)
                dto.LastModifiedByEmployeeId = await ResolveLoggedInEmployeeIdAsync();

            if (!dto.LastModifiedByEmployeeId.HasValue || dto.LastModifiedByEmployeeId.Value <= 0)
                return BadRequest("LastModifiedByEmployeeId is required.");

            try
            {
                var response = await _taskService.CompleteTaskAsync(dto);
                return Ok(new { Success = true, Data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        // ============================================================
        // Dashboard - Counts for My Tasks + Team Tasks
        // ============================================================
        [HttpPost("GetDashboard")]
        public async Task<IActionResult> GetDashboard([FromBody] TaskDashboardRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _taskService.GetTaskDashboardAsync(dto);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task dashboard");
                return StatusCode(500, new { Success = false, Message = "Error fetching dashboard." });
            }
        }

        [HttpPost("GetTaskReport")]
        public async Task<IActionResult> GetTaskReport([FromBody] TaskReportRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Request body cannot be null.");

            try
            {
                var result = await _taskService.GetTaskReportAsync(dto);
                return Ok(new { Success = true, Data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task report");
                return StatusCode(500, new { Success = false, Message = "Error fetching task report." });
            }
        }

        // ============================================================
        // Add Progress Update to a Task
        // ============================================================
        [HttpPost("AddProgressUpdate")]
        public async Task<IActionResult> AddProgressUpdate([FromBody] TaskProgressCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _taskService.AddProgressUpdateAsync(dto);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding progress update");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        // ============================================================
        // Get Progress Updates for a Task
        // ============================================================
        [HttpGet("GetProgressUpdates/{taskId}")]
        public async Task<IActionResult> GetProgressUpdates(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                return BadRequest("TaskId is required.");

            try
            {
                var result = await _taskService.GetProgressUpdatesAsync(taskId);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching progress updates");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetTaskDetails/{taskId}")]
        public async Task<IActionResult> GetTaskDetails(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                return BadRequest("TaskId is required.");

            try
            {
                var result = await _taskService.GetTaskDetailsAsync(taskId);
                if (result == null)
                    return NotFound(new { Success = false, Message = "Task not found." });

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task details");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        // ============================================================
        // Reassign Task (HOD/SubHOD can reassign to their reports)
        // ============================================================
        [HttpPost("ReassignTask")]
        public async Task<IActionResult> ReassignTask([FromBody] ReassignTaskRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _taskService.ReassignTaskAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning task");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}

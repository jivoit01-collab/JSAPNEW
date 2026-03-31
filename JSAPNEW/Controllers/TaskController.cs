using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JSAPNEW.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TaskController> _logger;

        public TaskController(ITaskService taskService, ILogger<TaskController> logger, IConfiguration configuration)
        {
            _taskService = taskService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("CreateTask")]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto taskDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
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
                return StatusCode(500, "An error occurred while creating the task.");
            }
        }

        [HttpPost("GetAllTasks")]
        public async Task<IActionResult> GetAllTasks([FromBody] TaskFilterDto filter)
        {
            if (filter == null)
            {
                return BadRequest("Request body cannot be null.");
            }

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
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("DeleteTask")]
        public async Task<IActionResult> DeleteTask(string TaskId)
        {
            if (TaskId == null || string.IsNullOrWhiteSpace(TaskId))
                return BadRequest("TaskId is required.");

            try
            {
                var response = await _taskService.DeleteTaskAsync(TaskId);

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
        [HttpPost("CompleteTask")]
        public async Task<IActionResult> CompleteTask([FromBody] CompleteTaskRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TaskId))
                return BadRequest("TaskId is required.");

            if (string.IsNullOrWhiteSpace(dto.LastModifiedBy))
                return BadRequest("LastModifiedBy is required.");

            try
            {
                var response = await _taskService.CompleteTaskAsync(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

    }
}

using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger, IConfiguration configuration)
        {
            _dashboardService = dashboardService;
            _configuration = configuration;
            _logger = logger;
        }
        [HttpGet("GetMaster")]
        public async Task<IActionResult> GetMaster()
        {
            try
            {
                var result = await _dashboardService.GetITStandardsMasterAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching IT standards master.");
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("GetSummary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var result = await _dashboardService.GetITStandardsSummaryAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching IT standards summary.");
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("GetByPriority")]
        public async Task<IActionResult> GetByPriority([FromQuery] string priority)
        {
            if (string.IsNullOrEmpty(priority))
                return BadRequest("Priority parameter is required.");

            try
            {
                var result = await _dashboardService.GetITStandardsByPriorityAsync(priority);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching IT standards by priority.");
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// Get dashboard statistics
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats(string group_by, int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetDashboardStatsAsync(group_by,deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching dashboard stats.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get all tasks with filters
        [HttpGet("list")]
        public async Task<IActionResult> GetAllTasks([FromQuery] TaskFilterRequest filter)
        {
            try
            {
                var result = await _dashboardService.GetAllTasksAsync(filter);
                return Ok(new { Success = true, Data = result });
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

        /// Get task by ID
        [HttpGet("details")]
        public async Task<IActionResult> GetTaskById(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return BadRequest(new { Success = false, Message = "Task ID is required." });

            try
            {
                var result = await _dashboardService.GetTaskByIdAsync(taskId);
                if (result == null)
                    return NotFound(new { Success = false, Message = "Task not found." });

                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching task by ID.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get status distribution
        [HttpGet("status-distribution")]
        public async Task<IActionResult> GetStatusDistribution([FromQuery] int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetStatusDistributionAsync(deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching status distribution.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get priority distribution
        [HttpGet("priority-distribution")]
        public async Task<IActionResult> GetPriorityDistribution([FromQuery] int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetPriorityDistributionAsync(deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching priority distribution.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get employee stats
        [HttpGet("employee-stats")]
        public async Task<IActionResult> GetEmployeeStats([FromQuery] int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetEmployeeStatsAsync(deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching employee stats.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get employee tasks
        [HttpGet("employee/{employeeId}/tasks")]
        public async Task<IActionResult> GetEmployeeTasks(int employeeId)
        {
            if (employeeId <= 0)
                return BadRequest(new { Success = false, Message = "Valid Employee ID is required." });

            try
            {
                var result = await _dashboardService.GetEmployeeTasksAsync(employeeId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching employee tasks.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get project list
        [HttpGet("projects")]
        public async Task<IActionResult> GetProjectList()
        {
            try
            {
                var result = await _dashboardService.GetProjectListAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching project list.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get employee list
        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployeeList([FromQuery] int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetEmployeeListAsync(deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching employee list.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }
        /// Get project breakdown
        [HttpGet("project-breakdown")]
        public async Task<IActionResult> GetProjectBreakdown([FromQuery] int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetProjectBreakdownAsync(deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching project breakdown.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get daily task trend
        [HttpGet("trend")]
        public async Task<IActionResult> GetDailyTaskTrend([FromQuery] int days = 30, [FromQuery] int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetDailyTaskTrendAsync(days, deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching daily task trend.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        /// Get overdue tasks
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueTasks([FromQuery] int? deptId = null)
        {
            try
            {
                var result = await _dashboardService.GetOverdueTasksAsync(deptId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching overdue tasks.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        [HttpGet("GetDashboardByCompany")]
        public async Task<IActionResult> GetDashboardByCompany(string clientName)
        {
            try
            {
                var result = await _dashboardService.GetDashboardByCompanyAsync(clientName);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching dashboard by company.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }
        [HttpGet("GetDashboardMaster")]
        public async Task<IActionResult> GetDashboardMaster()
        {
            try
            {
                var result = await _dashboardService.GetDashboardMasterAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching dashboard master.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        [HttpGet("GetDashboardProject")]
        public async Task<IActionResult> GetDashboardProject(int projectId)
        {
            try
            {
                var result = await _dashboardService.GetDashboardProjectAsync(projectId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching dashboard project.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        [HttpGet("GetAllMoM")]
        public async Task<IActionResult> GetAllMoM()
        {
            try
            {
                var result = await _dashboardService.GetAllMoMAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching dashboard project.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        [HttpPost("AddMoMPoint")]
        public async Task<IActionResult> AddMoMPoint(MomPointRequest request)
        {
            try
            {
                var result = await _dashboardService.AddMoMPointAsync(request);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching dashboard project.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }

        [HttpPost("UpdateMoMStatus")]
        public async Task<IActionResult> UpdateMoMStatus(MomStatusUpdateRequest request)
        {
            try
            {
                var result = await _dashboardService.UpdateMoMStatusAsync(request);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while fetching dashboard project.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred." });
            }
        }
        [HttpGet("getUniqueAccounts")]
        public async Task<IActionResult> GetUniqueAccounts([FromQuery] string? branch = null)
        {
            try
            {
                var accounts = await _dashboardService.GetUniqueAccounts(branch);

                if (accounts == null || !accounts.Any())
                {
                    return NotFound(new { message = "No accounts found" });
                }

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching accounts", error = ex.Message });
            }
        }


        [HttpGet("getUniqueBudgets")]
        public async Task<IActionResult> GetUniqueBudgets([FromQuery] string? branch = null)
        {
            try
            {
                var budgets = await _dashboardService.GetUniqueBudgets(branch);

                if (budgets == null || !budgets.Any())
                {
                    return NotFound(new { message = "No budgets found" });
                }

                return Ok(budgets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching budgets", error = ex.Message });
            }
        }

        [HttpGet("getBudgetDataByBranch")]
        public async Task<IActionResult> GetBudgetDataByBranch([FromQuery] string? branch = null)
        {
            try
            {
                var budgetData = await _dashboardService.GetAllBudgetDataAsync(branch);

                if (budgetData == null || !budgetData.Any())
                {
                    return NotFound(new { message = "No budget data found" });
                }

                return Ok(budgetData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching budget data", error = ex.Message });
            }
        }
    }

}

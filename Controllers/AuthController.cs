using Azure.Core;
using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.AccessControl;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService; //An interface for user-related operations
        private readonly ILogger<AuthController> _logger; //for recording events or errors

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _userService.ValidateUserAsync(request); //check user credentials

                if (response.Success)
                {
                    _logger.LogInformation($"Successful login attempt for user: {request.loginUser}");
                    return Ok(response);
                }

                _logger.LogWarning($"Failed login attempt for user: {request.loginUser}");
                return Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login attempt for user: {request.loginUser}");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,

                    Message = "An error occurred while processing your request"
                });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] UserRegistrationDTO request)
        {
            try
            {
                // Validate the model state (check if all fields are provided and are valid)
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Call the UserService to register the user
                int statusCode = await _userService.RegisterUserAsync(request); //create a new user

                if (statusCode == 2000) // Success
                {
                    _logger.LogInformation("User registered successfully");
                    return Ok(new { Success = true, Message = "User registered successfully" });
                }
                else if (statusCode == 5001) // User already exists
                {
                    _logger.LogInformation("User already exists");
                    return Ok(new { Success = false, Message = "User already exists" });
                }

                // If registration failed, log the error and return failure response
                _logger.LogWarning($"Registration failed for user: {request.loginUser}");
                return BadRequest(new { Success = false, Message = "Registration failed" });
            }
            catch (Exception ex)
            {
                // Log any unexpected errors that occur during the registration process
                _logger.LogError(ex, $"Error during registration attempt for user: {request.loginUser}");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }


                if (request.userId == 0)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _userService.ChangePasswordAsync(request);

                return result.Success
                    ? Ok(result)
                    : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user {UserId}", request.userId);
                return StatusCode(500, new { Message = "An error occurred while changing password" });
            }
        }

        [HttpPost("change-password2")]
        public async Task<IActionResult> ChangePassword2([FromBody] ChangePasswordRequest2 request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }


                if (request.userId == 0)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _userService.ChangePasswordAsync2(request);

                return result.Success
                    ? Ok(result)
                    : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user {UserId}", request.userId);
                return StatusCode(500, new { Message = "An error occurred while changing password" });
            }
        }

        [HttpGet("getVarieties")]
        public async Task<ActionResult> GetVarieties([FromQuery] int company)
        {
            try
            {
                var varieties = await _userService.GetVarietyAsync(company);

                if (!varieties.Any())
                {
                    _logger.LogInformation("No varieties found.");
                    return NotFound(new { Success = false, Message = "No varieties found" });
                }

                _logger.LogInformation("Varieties retrieved successfully.");
                return Ok(new { Success = true, Data = varieties });
            }
            catch (SqlException ex) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "SQL Error :" + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving varieties.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getEffMonths")]
        public async Task<ActionResult> GetEffMonth([FromQuery] int company)
        {
            try
            {
                var EffMonths = await _userService.GeteffMonthAsync(company);

                if (!EffMonths.Any())
                {
                    _logger.LogInformation("No EffMonths found.");
                    return NotFound(new { Success = false, Message = "No EffMonths found" });
                }

                _logger.LogInformation("EffMonths retrieved successfully.");
                return Ok(new { Success = true, Data = EffMonths });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving EffMonths.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getbudgets")]
        public async Task<ActionResult> GetBudget([FromQuery] int company)
        {
            try
            {
                var Budgets = await _userService.GetBudgetAsync(company);

                if (!Budgets.Any())
                {
                    _logger.LogInformation("No Budgets found.");
                    return NotFound(new { Success = false, Message = "No Budgets found" });
                }

                _logger.LogInformation("Budgets retrieved successfully.");
                return Ok(new { Success = true, Data = Budgets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving Budgets.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getSubBudgets")]
        public async Task<ActionResult> GetSubBudget([FromQuery] int company)
        {
            try
            {
                var SubBudgets = await _userService.GetSubBudgetAsync(company);

                if (!SubBudgets.Any())
                {
                    _logger.LogInformation("No SubBudgets found.");
                    return NotFound(new { Success = false, Message = "No SubBudgets found" });
                }

                _logger.LogInformation("SubBudgets retrieved successfully.");
                return Ok(new { Success = true, Data = SubBudgets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving SubBudgets.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getstates")]
        public async Task<ActionResult> GetState([FromQuery] int company)
        {
            try
            {
                var states = await _userService.GetStateAsync(company); // Assuming _stateService is your service to get states.

                if (!states.Any())
                {
                    _logger.LogInformation("No states found.");
                    return NotFound(new { Success = false, Message = "No states found" });
                }

                _logger.LogInformation("States retrieved successfully.");
                return Ok(new { Success = true, Data = states });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving states.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getroles")]
        public async Task<ActionResult> GetRole([FromQuery] int company)
        {
            try
            {
                var roles = await _userService.GetRoleAsync(company); // Assuming _userService is your service to get roles.

                if (!roles.Any())
                {
                    _logger.LogInformation("No roles found.");
                    return NotFound(new { Success = false, Message = "No roles found" });
                }

                _logger.LogInformation("Roles retrieved successfully.");
                return Ok(new { Success = true, Data = roles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving roles.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getbranches")]
        public async Task<ActionResult> GetBranch(int company)
        {
            try
            {
                var branches = await _userService.GetBranchAsync(company); // Assuming _userService is your service to get branches.

                if (!branches.Any())
                {
                    _logger.LogInformation("No branches found.");
                    return NotFound(new { Success = false, Message = "No branches found" });
                }

                _logger.LogInformation("Branches retrieved successfully.");
                return Ok(new { Success = true, Data = branches });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving branches.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getdepartments")]
        public async Task<ActionResult> GetDepartment(int company)
        {
            try
            {
                var department = await _userService.GetDepartmentAsync(company); // Assuming _userService is your service to get department.

                if (!department.Any())
                {
                    _logger.LogInformation("No department found.");
                    return NotFound(new { Success = false, Message = "No department found" });
                }

                _logger.LogInformation("department retrieved successfully.");
                return Ok(new { Success = true, Data = department });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving department.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getreports")]
        public async Task<ActionResult> GetReports([FromQuery] int company)
        {
            try
            {
                var report = await _userService.GetReportAsync(company); // Assuming _userService is your service to get report.

                if (!report.Any())
                {
                    _logger.LogInformation("No report found.");
                    return NotFound(new { Success = false, Message = "No report found" });
                }

                _logger.LogInformation("report retrieved successfully.");
                return Ok(new { Success = true, Data = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving report.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getusers")]
        public async Task<ActionResult> GetUsers([FromQuery] int company)
        {
            try
            {
                var user = await _userService.GetUserAsync(company); // Assuming _userService is your service to get user.

                if (!user.Any())
                {
                    _logger.LogInformation("No user found.");
                    return NotFound(new { Success = false, Message = "No user found" });
                }

                _logger.LogInformation("user retrieved successfully.");
                return Ok(new { Success = true, Data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving user.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getapprovals")]
        public async Task<ActionResult> GetApprovals([FromQuery] int company)
        {
            try
            {
                var approval = await _userService.GetApprovalAsync(company); // Assuming _userService is your service to get approval.

                if (!approval.Any())
                {
                    _logger.LogInformation("No approval found.");
                    return NotFound(new { Success = false, Message = "No approval found" });
                }

                _logger.LogInformation("approval retrieved successfully.");
                return Ok(new { Success = true, Data = approval });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving approval.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getcompanies")]
        public async Task<ActionResult> GetCompanies([FromQuery] int userId)
        {
            try
            {
                var company = await _userService.GetCompanyAsync(userId); // Assuming _userService is your service to get company.

                if (!company.Any())
                {
                    _logger.LogInformation("No company found.");
                    return NotFound(new { Success = false, Message = "No company found" });
                }

                _logger.LogInformation("company retrieved successfully.");
                return Ok(new { Success = true, Data = company });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving company.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" + ex });
            }
        }

        [HttpPost("addStage")]
        public async Task<ActionResult> AddStage([FromBody] AddStage stageData)
        {
            try
            {
                var result = await _userService.AddStageAsync(stageData);
                switch (result)
                {
                    case 1:
                        _logger.LogInformation("Stage added successfully.");
                        return Ok(new { Success = true, Message = "Stage added successfully." });
                    case 0:
                        _logger.LogInformation("Stage already exists.");
                        return Ok(new { Success = false, Message = "Stage already exists." });
                    default:
                        _logger.LogInformation("Failed to add Stage.");
                        return BadRequest(new { Success = false, Message = "Failed to add Stage." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding Stage.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("addUserPermissions")]
        public async Task<ActionResult> AddUserPermission([FromBody] AssignPermissionDetail Data)
        {
            try
            {
                // Validate the model state (check if all fields are provided and are valid)
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                int statusCode = await _userService.UserAssignPermissionAsync(Data);

                if (statusCode == 2000) // Success
                {
                    _logger.LogInformation("User Assign Permissions successfully");
                    return Ok(new { Success = true, Message = "User Assign Permissions successfully" });
                }
                else if (statusCode == 5001) // User already exists
                {
                    _logger.LogInformation("Assign Permissions already exists");
                    return Ok(new { Success = false, Message = "Assign Permissions already exists" });
                }

                // If registration failed, log the error and return failure response
                _logger.LogWarning("Assign Permission Registration failed");
                return BadRequest(new { Success = false, Message = "Registration failed" });
            }
            catch (Exception ex)
            {
                // Log any unexpected errors that occur during the registration process
                _logger.LogError(ex, "Error during Permission registration");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getstages")]
        public async Task<ActionResult> GetStages([FromQuery] int company)
        {
            try
            {
                var stage = await _userService.GetStageAsync(company); // Assuming _userService is your service to get stage.

                if (!stage.Any())
                {
                    _logger.LogInformation("No stage found.");
                    return NotFound(new { Success = false, Message = "No stage found" });
                }

                _logger.LogInformation("stage retrieved successfully.");
                return Ok(new { Success = true, Data = stage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving stage.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getallusers")]
        public async Task<ActionResult> GetAllUsers([FromQuery] int company)
        {
            try
            {
                var allusers = await _userService.GetAllUserAsync(company); // Fetch all users

                if (!allusers.Any())
                {
                    _logger.LogInformation("No users found.");
                    return NotFound(new { Success = false, Message = "No users found" });
                }

                // Group users by userId and aggregate department names into an array
                var groupedUsers = allusers
                    .GroupBy(u => new { u.userId, u.userName, u.userPhoneNumber, u.userEmail, u.role, u.status })
                    .Select(g => new
                    {
                        g.Key.userId,
                        g.Key.userName,
                        g.Key.userPhoneNumber,
                        g.Key.userEmail,
                        g.Key.role,
                        g.Key.status,
                        deptIds = g.Select(u => u.department).Distinct().ToList() // Aggregate department names into an array
                    })
                    .ToList();

                _logger.LogInformation("Users retrieved and grouped successfully.");
                return Ok(new { Success = true, Data = groupedUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving users.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getselecteduserdata")]
        public async Task<ActionResult> GetSelectedUser([FromQuery] int company, int userId)
        {
            try
            {
                var user = await _userService.GetSelectedUserAsync(company, userId); // Assuming _userService is your service to get user.

                if (!user.Any())
                {
                    _logger.LogInformation("No user found.");
                    return NotFound(new { Success = false, Message = "No user found" });
                }

                var groupedUsers = user
                    .GroupBy(u => new { u.userId, u.userName, u.userPhoneNumber, u.firstName, u.lastName, u.empId, u.dateOfJoining, u.userEmail, u.role, u.status })
                    .Select(g => new
                    {
                        g.Key.userId,
                        g.Key.userName,
                        g.Key.userPhoneNumber,
                        g.Key.firstName,
                        g.Key.lastName,
                        g.Key.empId,
                        g.Key.dateOfJoining,
                        g.Key.userEmail,
                        g.Key.role,
                        g.Key.status,
                        deptIds = g.Select(u => u.department).Distinct().ToList() // Aggregate department names into an array
                    })
                    .ToList();

                _logger.LogInformation("Users retrieved and grouped successfully.");
                return Ok(new { Success = true, Data = groupedUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving user.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getcountapproval")]
        public async Task<ActionResult> GetCountApproval()
        {
            try
            {
                var countapproval = await _userService.GetCountApprovalAsync(); // Assuming _userService is your service to get countapproval.

                if (!countapproval.Any())
                {
                    _logger.LogInformation("No countapproval found.");
                    return NotFound(new { Success = false, Message = "No countapproval found" });
                }

                _logger.LogInformation("countapproval retrieved successfully.");
                return Ok(new { Success = true, Data = countapproval });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving countapproval.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getcountrejection")]
        public async Task<ActionResult> GetCountRejection()
        {
            try
            {
                var countrejection = await _userService.GetCountRejectionAsync(); // Assuming _userService is your service to get countrejection.

                if (!countrejection.Any())
                {
                    _logger.LogInformation("No countrejection found.");
                    return NotFound(new { Success = false, Message = "No countrejection found" });
                }

                _logger.LogInformation("countrejection retrieved successfully.");
                return Ok(new { Success = true, Data = countrejection });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving countrejection.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getusernotregisterincompany")]
        public async Task<ActionResult> GetUserNotRegisterInCompany([FromQuery] int company)
        {
            try
            {
                var allusers = await _userService.GetUserNotRegisterInCompanyAsync(company); // Assuming _userService is your service to get allusers.

                if (!allusers.Any())
                {
                    _logger.LogInformation("No allusers found.");
                    return NotFound(new { Success = false, Message = "No allusers found" });
                }

                _logger.LogInformation("allusers retrieved successfully.");
                return Ok(new { Success = true, Data = allusers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving allusers.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("changelockprofile")]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UserStatusUpdateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.UpdateUserStatus(model);

            if (result)
                return Ok(new { message = "User status updated successfully" });
            else
                return StatusCode(500, new { message = "Failed to update user status" });
        }

        [HttpPost("addtemplate")]
        public async Task<ActionResult> AddTemplate([FromBody] AddTemplateModel request)
        {
            try
            {
                var addtemplate = await _userService.GetAddTemplateAsync(request);

                if (addtemplate == 0)
                {
                    _logger.LogInformation("Template added successfully.");
                    return Ok(new { Success = true, Data = addtemplate });
                }

                _logger.LogInformation("Failed to add Template.");
                return NotFound(new { Success = false, Message = "Failed to add Template." });
            }
            catch (SqlException ex) when (ex.Number == 50004) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Already Exist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding Template.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("addpage")]
        public async Task<ActionResult> AddPage([FromBody] AddPageModel request)
        {
            try
            {
                var addpage = await _userService.GetAddPageAsync(request);

                if (addpage > 0)
                {
                    _logger.LogInformation("Page added successfully.");
                    return Ok(new { Success = true, Data = addpage });
                }

                _logger.LogInformation("Failed to add page.");
                return NotFound(new { Success = false, Message = "Failed to add page." });
            }
            catch (SqlException ex) when (ex.Number == 50004) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Page is not found!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding page.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("addrole")]
        public async Task<ActionResult> AddRole([FromBody] AddRoleModel request)
        {
            try
            {
                var role = await _userService.GetAddRoleAsync(request);

                if (role > 0)
                {
                    _logger.LogInformation("role added successfully.");
                    return Ok(new { Success = true, Data = role });
                }

                _logger.LogInformation("Failed to add role.");
                return NotFound(new { Success = false, Message = "Failed to add role." });
            }
            catch (SqlException ex) when (ex.Number == 50004) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "role is not found!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding role.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("budgetstatusCount")]
        public async Task<ActionResult> GetBudgetStatusCount(int userId, int company, string month = "05-2025")
        {
            try
            {
                var budgetapproval = await _userService.GetAllBudgetApprovalCountsAsync(userId, company, month);

                if (!budgetapproval.Any())
                {
                    _logger.LogInformation("No budgetapproval found.");
                    return NotFound(new { Success = false, Message = "No budgetapproval found" });
                }

                _logger.LogInformation("budgetapproval retrieved successfully.");
                return Ok(new { Success = true, Data = budgetapproval });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving budgetapproval.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("budgetstatusCount2")]
        public async Task<ActionResult> GetBudgetStatusCount2(int userId, int company, string month)
        {
            try
            {
                var budgetapproval = await _userService.GetAllBudgetApprovalCountsAsync(userId, company, month);

                if (!budgetapproval.Any())
                {
                    _logger.LogInformation("No budgetapproval found.");
                    return NotFound(new { Success = false, Message = "No budgetapproval found" });
                }

                _logger.LogInformation("budgetapproval retrieved successfully.");
                return Ok(new { Success = true, Data = budgetapproval });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving budgetapproval.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("updatebudget")]
        public async Task<ActionResult> UpdateBudget(int userId, int updatedBy, string budgetId, bool status, int company)
        {
            try
            {
                var budget = await _userService.UpdateBudgetAsync(userId, updatedBy, budgetId, status, company);

                _logger.LogInformation("budget updated successfully.");
                return Ok(new { Success = true, Data = budget, Message = "budget updated successfully." });

            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "budget is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding budget.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("getqueryname")]
        public async Task<ActionResult> GetQueryName(string type, int company)
        {
            try
            {
                var query = await _userService.GetQueryNameAsync(type, company); // Assuming _userService is your service to get query.

                if (!query.Any())
                {
                    _logger.LogInformation("No query found.");
                    return NotFound(new { Success = false, Message = "No query found" });
                }

                _logger.LogInformation("query retrieved successfully.");
                return Ok(new { Success = true, Data = query });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving query.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("updateuserapproval")]
        public async Task<ActionResult> UpdateUserApproval([FromBody] useraprrovalModel request)
        {
            try
            {
                var approval = await _userService.UpdateUserApprovalAsync(request);

                _logger.LogInformation("approval updated successfully.");
                return Ok(new { Success = true, Data = approval, Message = "approval updated successfully." });

            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "approval is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding approval.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("updatestate")]
        public async Task<ActionResult> UpdateState(int userId, int updatedBy, string stateId, bool status, int company)
        {
            try
            {
                var state = await _userService.UpdateStateAsync(userId, updatedBy, stateId, status, company);

                _logger.LogInformation("state updated successfully.");
                return Ok(new { Success = true, Data = state, Message = "state updated successfully." });

            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "state is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding state.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("updatesubbudget")]
        public async Task<ActionResult> UpdateSubBudget(int userId, int updatedBy, string subBudgetId, bool status, int company)
        {
            try
            {
                var subbudget = await _userService.UpdateSubBudgetAsync(userId, updatedBy, subBudgetId, status, company);

                _logger.LogInformation("subbudget updated successfully.");
                return Ok(new { Success = true, Data = subbudget, Message = "subbudget updated successfully." });

            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "subbudget is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding subbudget.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("updatefromtodate")]
        public async Task<ActionResult> UpdateFromToDate(int userId, int updatedBy, DateTime toDate, DateTime fromDate, int company)
        {
            try
            {
                var dates = await _userService.UpdateFromToDateAsync(userId, updatedBy, toDate, fromDate, company);

                _logger.LogInformation("dates updated successfully.");
                return Ok(new { Success = true, Data = dates, Message = "dates updated successfully." });

            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "dates is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding dates.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("updatebranch")]
        public async Task<ActionResult> UpdateBranch(int userId, int updatedBy, string branchId, bool status, int company)
        {
            try
            {
                var branch = await _userService.UpdateBranchAsync(userId, updatedBy, branchId, status, company);

                _logger.LogInformation("branch updated successfully.");
                return Ok(new { Success = true, Data = branch, Message = "branch updated successfully." });
            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "branch is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding branch.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("updatereport")]
        public async Task<ActionResult> UpdateReport(int userId, int updatedBy, string reportId, bool status, int company)
        {
            try
            {
                var report = await _userService.UpdateReportAsync(userId, updatedBy, reportId, status, company);

                _logger.LogInformation("report updated successfully.");
                return Ok(new { Success = true, Data = report, Message = "report updated successfully." });
            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "report is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding report.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("updatevariety")]
        public async Task<ActionResult> UpdateVariety(int userId, int updatedBy, string varietyId, bool status, int company)
        {
            try
            {
                var variety = await _userService.UpdateVarietyAsync(userId, updatedBy, varietyId, status, company);


                _logger.LogInformation("variety updated successfully.");
                return Ok(new { Success = true, Data = variety, Message = "variety updated successfully." });

            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "variety is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding variety.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("updateuserrole")]
        public async Task<ActionResult> UpdateUserRole(int userId, int roleId, int company)
        {
            try
            {
                var userrole = await _userService.UpdateUserRoleAsync(userId, roleId, company);

                //if (budget > 0)
                //{
                _logger.LogInformation("userrole updated successfully.");
                return Ok(new { Success = true, Data = userrole, Message = "User Role updated successfully." });
                //}
                /*
                                _logger.LogInformation("Failed to updated budget.");
                                return NotFound(new { Success = false, Message = "Failed to updated budget." });*/
            }
            catch (SqlException ex) when (ex.Number == 50000) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "userrole is already updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding userrole.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("getpendingbudgetwithdetails")]
        public async Task<ActionResult> GetPendingBudgetWithDetails(int userId, int company, string month)
        {
            try
            {
                var pendingbudget = await _userService.GetPendingBudgetWithDetailsAsync(userId, company, month); // Assuming _userService is your service to get query.

                if (!pendingbudget.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                // Attach NextApprover data
                foreach (var budget in pendingbudget)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("data retrieved successfully.");
                return Ok(new { Success = true, Data = pendingbudget });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getapprovedbudgetwithdetails")]
        public async Task<ActionResult> GetApprovedBudgetWithDetails(int userId, int company, string month)
        {
            try
            {
                var approvedBudgets = await _userService.GetApprovedBudgetWithDetailsAsync(userId, company, month);

                if (!approvedBudgets.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                // Attach NextApprover data
                foreach (var budget in approvedBudgets)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = approvedBudgets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getrejectedbudgetwithdetails")]
        public async Task<ActionResult> GetRejectedBudgetWithDetails(int userId, int company, string month)
        {
            try
            {
                var rejectedbudget = await _userService.GetRejectedBudgetWithDetailsAsync(userId, company, month); // Assuming _userService is your service to get query.

                if (!rejectedbudget.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                // Attach NextApprover data
                foreach (var budget in rejectedbudget)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("data retrieved successfully.");
                return Ok(new { Success = true, Data = rejectedbudget });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }


        [HttpGet("getpendingbudgetwithdetails2")]
        public async Task<ActionResult> GetPendingBudgetWithDetails2(int userId, int company, string month)
        {
            try
            {
                var pendingbudget = await _userService.GetPendingBudgetWithDetailsAsync2(userId, company, month); // Assuming _userService is your service to get query.

                if (!pendingbudget.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                // Attach NextApprover data
                foreach (var budget in pendingbudget)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("data retrieved successfully.");
                return Ok(new { Success = true, Data = pendingbudget });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getapprovedbudgetwithdetails2")]
        public async Task<ActionResult> GetApprovedBudgetWithDetails2(int userId, int company, string month)
        {
            try
            {
                var approvedBudgets = await _userService.GetApprovedBudgetWithDetailsAsync2(userId, company, month);

                if (!approvedBudgets.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                // Attach NextApprover data
                foreach (var budget in approvedBudgets)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = approvedBudgets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }


        [HttpGet("getrejectedbudgetwithdetails2")]
        public async Task<ActionResult> GetRejectedBudgetWithDetails2(int userId, int company, string month)
        {   
            try
            {
                var rejectedbudget = await _userService.GetRejectedBudgetWithDetailsAsync2(userId, company, month); // Assuming _userService is your service to get query.

                if (!rejectedbudget.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                // Attach NextApprover data
                foreach (var budget in rejectedbudget)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("data retrieved successfully.");
                return Ok(new { Success = true, Data = rejectedbudget });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }
        
        [HttpGet("getallbudgetwithdetails")]
        public async Task<ActionResult> GetBudgetDetails(int userId, int company, string month)
        {
            try
            {
                var budgets = await _userService.GetAllBudgetWithDetailsAsync(userId, company, month);

                if (!budgets.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                foreach (var budget in budgets)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("Budget data retrieved successfully.");
                return Ok(new { Success = true, Data = budgets }); // Data as an array of lists
            }
            catch (SqlException ex)
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Database Error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving budget data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getallbudgetwithdetails2")]
        public async Task<ActionResult> GetBudgetDetails2(int userId, int company, string month)
        {
            try
            {
                var budgets = await _userService.GetAllBudgetWithDetailsAsync(userId, company, month);

                if (!budgets.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                foreach (var budget in budgets)
                {
                    var nextApprovers = await _userService.GetNextApproverAsync(budget.BudgetId);
                    budget.NextApprover = nextApprovers;
                }

                _logger.LogInformation("Budget data retrieved successfully.");
                return Ok(new { Success = true, Data = budgets }); // Data as an array of lists
            }
            catch (SqlException ex)
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Database Error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving budget data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetUserBudgetSummaryByType")]
        public async Task<ActionResult> GetUserBudgetSummaryByType(int userId, int company, string budgetType)
        {
            try
            {
                var budgetsummary = await _userService.GetUserBudgetSummaryByTypeAsync(userId, company, budgetType); // Assuming _userService is your service to get query.

                if (!budgetsummary.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                _logger.LogInformation("data retrieved successfully.");
                return Ok(new { Success = true, Data = budgetsummary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getDocEntryData")]
        public async Task<ActionResult> GetDocEntryData(int docEntry, int userId, int company)
        {
            try
            {
                var docEntryData = await _userService.getDocEntryDataAsync(docEntry, userId, company);
                var remarksData = await _userService.getRemarksDataAsync(docEntry, company);


                if (!docEntryData.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                // Append a downloadable file link
                var updatedData = docEntryData.GroupBy(item => new
                {
                    item.Branch,
                    item.DocEntry,
                    item.ObjectName,
                    item.ObjType,
                    item.LineNum,
                    item.VisOrder,
                    item.AcctCode,
                    item.AcctName,
                    item.CardCode,
                    item.CardName,
                    item.EFFECTMONTH,
                    item.BUDGET,
                    item.SUB_BUDGET,
                    item.STATE,
                    item.DocDate,
                    item.CreateDate,
                    item.AMOUNT,
                    item.CURRENTMONTH,
                    item.Current_month_Posted_Amount,
                    item.AtcEntry,
                    item.Budget_Owner,
                    item.OwnerCode,
                    item.ApproverName,
                    item.ApprovalCode,
                    item.Current_month_Budget,
                    item.Status,
                    item.U_Name,
                    item.CreateTime,
                    item.LineRemarks,
                    item.Comments,
                    item.ProcesStat,
                    item.ACOMMENT,
                    item.VCOMMENT,
                    item.VerifiedStatus,
                    item.ApprovedStatus
                })
                 .Select(g => new
                 {
                     g.Key.Branch,
                     g.Key.DocEntry,
                     g.Key.ObjectName,
                     g.Key.ObjType,
                     g.Key.LineNum,
                     g.Key.VisOrder,
                     g.Key.AcctCode,
                     g.Key.AcctName,
                     g.Key.CardCode,
                     g.Key.CardName,
                     g.Key.EFFECTMONTH,
                     g.Key.BUDGET,
                     g.Key.SUB_BUDGET,
                     g.Key.STATE,
                     g.Key.DocDate,
                     g.Key.CreateDate,
                     /* g.Key.TrgtPath,
                      g.Key.FileName,
                      g.Key.FileExt,*/
                     g.Key.AMOUNT,
                     g.Key.CURRENTMONTH,
                     g.Key.Current_month_Posted_Amount,
                     g.Key.AtcEntry,
                     g.Key.Budget_Owner,
                     g.Key.OwnerCode,
                     g.Key.ApproverName,
                     g.Key.ApprovalCode,
                     g.Key.Current_month_Budget,
                     g.Key.Status,
                     g.Key.U_Name,
                     g.Key.CreateTime,
                     g.Key.LineRemarks,
                     g.Key.Comments,
                     g.Key.ProcesStat,
                     g.Key.ACOMMENT,
                     g.Key.VCOMMENT,
                     g.Key.VerifiedStatus,
                     g.Key.ApprovedStatus,
                     filesData = g.Select(f => new
                     {
                         f.FileName,
                         f.FileExt,
                         DownloadUrl = string.IsNullOrEmpty(f.TrgtPath) ? null : Url.Action("DownloadFile", "File", new
                         {
                             filePath = Uri.EscapeDataString(f.TrgtPath.Replace("\\", "/")),
                             fileName = Uri.EscapeDataString(f.FileName ?? ""),
                             fileExt = Uri.EscapeDataString(f.FileExt ?? "")
                         }, Request.Scheme)
                     }).ToList()  // Include all filenames
                 }).ToList();

                // _logger.LogInformation("Data retrieved successfully.");
                return Ok(new
                {
                    Success = true,
                    Data = updatedData,
                    RemarksData = remarksData // Add remarks data as a separate array
                });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("approvebudget")]
        public async Task<IActionResult> ApproveBudget([FromBody] BudgetRequest2 request)
        {
            var response = await _userService.ApproveBudgetAsync(request);
            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }

        /*[HttpPost("approvebudget2")]
        public async Task<IActionResult> ApproveBudget2([FromBody] BudgetRequest2 request)
        {
            var response = await _userService.ApproveBudgetAsync2(request);
            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }*/

        [HttpPost("rejectebudget")]
        public async Task<ActionResult> RejectBudget(BudgetRequest request)
        {
            Response response = await _userService.RejectBudgetAsync(request);
            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpGet("getallpermissionsofoneuser")]
        public async Task<ActionResult> GetAllPermissionsOfOneUser(int userId, int company)
        {
            var data = await _userService.GetAllPermissionsOfOneUserAsync(userId, company);
            return Ok(data);
        }

        [HttpGet("getuserbudgettypes")]
        public async Task<ActionResult> GetUserBudgetTypes(int userId, int company)
        {
            var data = await _userService.GetUserBudgetTypesAsync(userId, company); // Assuming _userService is your service to get query.
            return Ok(data);
        }

        [HttpGet("getTotalAmountOfOneDocEntry")]
        public async Task<ActionResult> AmountOfOneDocEntry(int docEntry, int userId, int company)
        {
            try
            {
                var OnedocEntry = await _userService.AmountOfOneDocEntryAsync(docEntry, userId, company); // Assuming _userService is your service to get query.

                if (!OnedocEntry.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                _logger.LogInformation("data retrieved successfully.");
                return Ok(new { Success = true, Data = OnedocEntry });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }



        [HttpGet("getbudgetallocation")]
        public async Task<ActionResult> GetBudgetAllocation(int userId, int company, string month)
        {
            try
            {
                var budgetallocation = await _userService.GetUserBudgetAllocationAsync(userId, company, month);

                if (!budgetallocation.Any())
                {
                    _logger.LogInformation("No budgetallocation found.");
                    return NotFound(new { Success = false, Message = "No budgetallocation found" });
                }

                _logger.LogInformation("budgetallocation retrieved successfully.");
                return Ok(new { Success = true, Data = budgetallocation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, inner = ex.InnerException?.Message });
            }
        }


        [HttpPost("addquery")]
        public async Task<ActionResult> AddQuery([FromBody] AddQueryModel request)
        {
            try
            {
                var addquery = await _userService.GetAddQueryAsync(request);


                _logger.LogInformation("Query added successfully.");
                return Ok(new { Success = true, Data = addquery });

            }
            catch (SqlException ex) when (ex.Number == 50004) // Assuming error number 50004 corresponds to your custom error
            {
                _logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "query is not found!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding query.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("validatequery")]
        public async Task<ActionResult> ValidateQuery(string query)
        {
            try
            {
                var validatequery = await _userService.GetValidateQueryAsync(query);

                _logger.LogInformation("Query validated successfully.");
                return Ok(new { Success = true, Data = validatequery });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while validating the query.");
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost("adminresetpassword")]
        public async Task<ActionResult> AdminResetPassword([FromBody] AdminResetPasswordModel request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                // Call the service to add the catalog item
                var result = await _userService.ResetAdminPasswordAsync(request);
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resetting password for userId: {UserId}.", request.userId);
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("gettemplatelist")]
        public async Task<ActionResult> GetTemplateList(int company)
        {
            try
            {
                var templateList = await _userService.GetTemplateListAsync(company);
                if (!templateList.Any())
                {
                    _logger.LogInformation("No template found.");
                    return NotFound(new { Success = false, Message = "No template found" });
                }
                _logger.LogInformation("Template retrieved successfully.");
                return Ok(new { Success = true, Data = templateList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving template.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getonetemplatedetail")]
        public async Task<ActionResult> GetOneTemplateDetail(int tempId, int company)
        {
            try
            {
                var templateDetail = await _userService.GetOneTemplateDetailAsync(tempId, company);
                if (templateDetail == null)
                {
                    _logger.LogInformation("No template found.");
                    return NotFound(new { Success = false, Message = "No template found" });
                }
                _logger.LogInformation("Template retrieved successfully.");
                return Ok(new { Success = true, Data = templateDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving template.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getstagelist")]
        public async Task<ActionResult> GetStageList(int company)
        {
            try
            {
                var stageList = await _userService.GetStageListAsync(company);
                if (!stageList.Any())
                {
                    _logger.LogInformation("No stage found.");
                    return NotFound(new { Success = false, Message = "No stage found" });
                }
                _logger.LogInformation("Stage retrieved successfully.");
                return Ok(new { Success = true, Data = stageList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving stage.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getonestagedetail")]
        public async Task<ActionResult> GetOneStageDetail(int stageId, int company)
        {
            try
            {
                var stageDetail = await _userService.GetOneStageDetailAsync(stageId, company);
                if (stageDetail == null)
                {
                    _logger.LogInformation("No stage found.");
                    return NotFound(new { Success = false, Message = "No stage found" });
                }
                _logger.LogInformation("Stage retrieved successfully.");
                return Ok(new { Success = true, Data = stageDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving stage.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getflowdocentry")]
        public async Task<ActionResult> GetFlowDocEntry(int docEntry)
        {
            try
            {
                var flowDocEntry = await _userService.GetFlowDocEntryAsync(docEntry);
                if (!flowDocEntry.Any())
                {
                    _logger.LogInformation("No flow found.");
                    return NotFound(new { Success = false, Message = "No flow found" });
                }
                _logger.LogInformation("Flow retrieved successfully.");
                return Ok(new { Success = true, Data = flowDocEntry });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flow.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("AddAlternativeUserToStages")]
        public async Task<ActionResult> AddAlternativeUserToStages([FromBody] AddAlternativeUserToStagesModel request)
        {
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request data." });
            }

            try
            {
                var result = await _userService.AddAlternativeUserToStagesAsync(request);

                if (result.Any(r => r.Success))  // Check if at least one response has Success = true
                {
                    return Ok(new { Success = true, Message = "Alternative user added to stages successfully.", Data = result });
                }

                return UnprocessableEntity(new { Success = false, Message = "Failed to add alternative user to stages.", Data = result });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding alternative user to stages.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("DeactivateDelegation")]
        public async Task<ActionResult> DeactivateDelegation([FromBody] DeactivateDelegationModel request)
        {
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request data." });
            }
            try
            {
                var result = await _userService.DeactivateDelegationAsync(request);
                if (result.Any(r => r.Success))  // Check if at least one response has Success = true
                {
                    return Ok(new { Success = true, Message = "Delegation deactivated successfully.", Data = result });
                }
                return UnprocessableEntity(new { Success = false, Message = "Failed to deactivate delegation.", Data = result });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deactivating delegation.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetFlowDocEntryTwo")]
        public async Task<ActionResult> GetFlowDocEntryTwo(int docEntry)
        {
            try
            {
                var flowDocEntry = await _userService.GetFlowDocEntryTwoAsync(docEntry);
                if (!flowDocEntry.Any())
                {
                    _logger.LogInformation("No flow found.");
                    return NotFound(new { Success = false, Message = "No flow found" });
                }
                _logger.LogInformation("Flow retrieved successfully.");
                return Ok(new { Success = true, Data = flowDocEntry });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flow.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetApprovalDelegationList")]
        public async Task<ActionResult> GetApprovalDelegationList()
        {
            try
            {
                var delegationList = await _userService.GetApprovalDelegationListAsync();
                if (!delegationList.Any())
                {
                    _logger.LogInformation("No delegation found.");
                    return NotFound(new { Success = false, Message = "No delegation found" });
                }
                _logger.LogInformation("Delegation retrieved successfully.");
                return Ok(new { Success = true, Data = delegationList });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving delegation.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetTemplateListAccordingToUser")]
        public async Task<ActionResult> GetTemplateListAccordingToUser(int userId, int company)
        {
            try
            {
                var templateList = await _userService.GetTemplateListAccordingToUserAsync(userId, company);
                if (!templateList.Any())
                {
                    _logger.LogInformation("No template found.");
                    return NotFound(new { Success = false, Message = "No template found" });
                }
                _logger.LogInformation("Template retrieved successfully.");
                return Ok(new { Success = true, Data = templateList });
            }
            catch (SqlException ex)
            {

                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving template.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("CreateCategoryMonthlyBudget")]
        public async Task<IActionResult> CreateCategoryMonthlyBudget([FromBody] MonthlyBudgetModel request)
        {
            Response response = await _userService.CreateCategoryMonthlyBudgetAsync(request);
            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpGet("GetBudgetCategorySummaryDashboard")]
        public async Task<ActionResult> GetBudgetCategorySummaryDashboard(string month, int company)
        {
            try
            {
                var budgetCategorySummary = await _userService.GetBudgetCategorySummaryDashboardAsync(month, company);
                if (!budgetCategorySummary.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = budgetCategorySummary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /*[HttpGet("GetBudgetDetailById")]
        public async Task<IActionResult> GetBudgetDetailById(int budgetId, int company)
        {
            var urlHelper = Url; // Get the IUrlHelper instance
            var result = await _userService.GetBudgetDetailByIdAsync(budgetId,company, urlHelper);
            return Ok(new { Success = true, Data = result });
        }*/

        [HttpGet("GetBudgetDetailById")]
        public async Task<IActionResult> GetBudgetDetailById(int budgetId)
        {
            var urlHelper = Url; // Get the IUrlHelper instance
            var result = await _userService.GetBudgetDetailByIdAsync(budgetId, urlHelper);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("GetBudgetDetailByIdv2")]
        public async Task<IActionResult> GetBudgetDetailByIdv2(int budgetId, int company)
        {
            var urlHelper = Url; // Get the IUrlHelper instance
            var result = await _userService.GetBudgetDetailByIdAsyncv2(budgetId, company, urlHelper);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("GetCategoryMonthlyBudget")]
        public async Task<ActionResult> GetCategoryMonthlyBudget(string budgetCategory, string subBudget, string month, int company)
        {
            try
            {
                var budgetCategoryMonthly = await _userService.GetCategoryMonthlyBudgetAsync(budgetCategory, subBudget, month, company);
                if (!budgetCategoryMonthly.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = budgetCategoryMonthly });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("BudgetCategoryDropdown")]
        public async Task<ActionResult> BudgetCategoryDropdown(int userId, int company)
        {
            try
            {
                var budgetCategoryDropdown = await _userService.BudgetCategoryDropdownAsync(userId, company);
                if (!budgetCategoryDropdown.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = budgetCategoryDropdown });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetBudgetApprovalFlow")]
        public async Task<ActionResult> GetBudgetApprovalFlow(int budgetId)
        {
            try
            {
                var budgetApprovalFlow = await _userService.GetBudgetApprovalFlowAsync(budgetId);
                if (!budgetApprovalFlow.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = budgetApprovalFlow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("DelegateApprovalStagesTwo")]
        public async Task<ActionResult> DelegateApprovalStagesTwo([FromBody] DelegateApprovalStagesTwoModel request)
        {
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request data." });
            }
            try
            {
                var result = await _userService.DelegateApprovalStagesTwoAsync(request);
                if (result.Any(r => r.Success))  // Check if at least one response has Success = true
                {
                    return Ok(new { Success = true, Message = "Delegation added successfully.", Data = result });
                }
                return UnprocessableEntity(new { Success = false, Message = "Failed to add delegation.", Data = result });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding delegation.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetDelegatedUserListTwo")]
        public async Task<ActionResult> GetDelegatedUserListTwo()
        {
            try
            {
                var DelegatedUserListTwo = await _userService.GetDelegatedUserListTwoAsync();
                if (!DelegatedUserListTwo.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = DelegatedUserListTwo });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data in Database.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }
        [HttpPost("UpdateDelegationDatesTwo")]
        public async Task<ActionResult> UpdateDelegationDatesTwo([FromBody] UpdateDelegationDatesTwoModel request)
        {
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request data." });
            }
            try
            {
                var result = await _userService.UpdateDelegationDatesTwoAsync(request);
                if (result.Any(r => r.Success))  // Check if at least one response has Success = true
                {
                    return Ok(new { Success = true, Message = "Delegation updated successfully.", Data = result });
                }
                return UnprocessableEntity(new { Success = false, Message = "Failed to update delegation.", Data = result });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating delegation.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("UpdateUserStageStatusTwo")]
        public async Task<ActionResult> UpdateUserStageStatusTwo([FromBody] UpdateUserStageStatusTwoModel request)
        {
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request data." });
            }
            try
            {
                var result = await _userService.UpdateUserStageStatusTwoAsync(request);
                if (result.Any(r => r.Success))  // Check if at least one response has Success = true
                {
                    return Ok(new { Success = true, Message = "Stage updated successfully.", Data = result });
                }
                return UnprocessableEntity(new { Success = false, Message = "Failed to update stage.", Data = result });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating staging.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetActiveTemplate")]
        public async Task<ActionResult> GetActiveTemplate(int company)
        {
            try
            {
                var ActiveTemplate = await _userService.GetActiveTemplateSync(company);
                if (!ActiveTemplate.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = ActiveTemplate });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data in Database.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetBudgetRelatedToTemplate")]
        public async Task<ActionResult> GetBudgetRelatedToTemplate(int templateId)
        {
            try
            {
                var BudgetRelatedToTemplate = await _userService.GetBudgetRelatedToTemplateAsync(templateId);
                if (!BudgetRelatedToTemplate.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = BudgetRelatedToTemplate });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data in Database.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetOneDelegatedUser")]
        public async Task<ActionResult> GetOneDelegatedUser(int stageId, int delegatedBy, int delegatedTo)
        {
            try
            {
                var DelegatedUserData = await _userService.GetOneDelegatedUserAsync(stageId, delegatedBy, delegatedTo);
                if (!DelegatedUserData.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = DelegatedUserData });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flow.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetAllBudgetInsight")]
        public async Task<ActionResult> GetBudgetInsight(int company, string month)
        {
            try
            {
                var AllBudgetInsightData = await _userService.GetBudgetInsightAsync(company, month);
                if (!AllBudgetInsightData.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = AllBudgetInsightData });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flow.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("sendpendingbudgetnotify")]
        public async Task<IActionResult> SendPendingCountNotification()
        {
            var response = await _userService.SendPendingCountNotificationAsync();
            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpGet("GetBudgetSummary")]
        public async Task<ActionResult> GetBudgetSummary(int userId, string budgetCategory, string subBudget, string month, int company)
        {
            try
            {
                var budgetSummary = await _userService.GetBudgetSummaryAsync(userId, budgetCategory, subBudget, month, company);
                if (!budgetSummary.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = budgetSummary });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flow.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }

        }

        [HttpGet("GetBudgetSummaryAmount")]
        public async Task<IActionResult> GetCombinedBudgets(int userId, int company, string month)
        {
            try
            {
                var data = await _userService.GetCombinedBudgetsAsync(userId, company, month);
                if (data == null || !data.BudgetData.Any())
                {
                    return NotFound(new { success = false, message = "No data found" });
                }

                var finalResult = new List<object>();

                foreach (var Budget in data.BudgetData)
                {
                    var budgetDetail = data.BudgetDetails.FirstOrDefault(x => x.BudgetHeader?.BudgetId == Budget.BudgetId);

                    var budgetData = new
                    {
                        budgetId = Budget.BudgetId,
                        objType = Budget.objType,
                        company = Budget.company,
                        docEntry = Budget.DocEntry,
                        objectName = Budget.ObjectName,
                        cardCode = Budget.CardCode,
                        cardName = Budget.CardName,
                        docDate = Budget.DocDate?.ToString(),
                        totalAmount = Budget.TotalAmount,
                        status = Budget.Status,
                        header = new
                        {
                            templateId = budgetDetail?.BudgetHeader?.TemplateId,
                            totalStage = budgetDetail?.BudgetHeader?.TotalStage,
                            currentStageId = budgetDetail?.BudgetHeader?.CurrentStageId,
                            currentStatus = budgetDetail?.BudgetHeader?.CurrentStatus
                        },
                        lines = budgetDetail?.BudgetLines?.Select(line => new
                        {
                            budget = line.Budget,
                            subBudget = line.SubBudget,
                            variety = line.variety,
                            acctCode = line.AcctCode,
                            acctName = line.AcctName,
                            lineNum = line.LineNum,
                            amount = line.Amount,
                            state = line.State,
                            EffectMonth = line.EffectMonth,
                            lineRemarks = line.LineRemarks,
                            comments = line.Comments
                        }).ToList()
                    };

                    finalResult.Add(budgetData);
                }

                return Ok(new { success = true, data = finalResult });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("GetAllBudgetSummaryAmount")]
        public async Task<ActionResult> GetAllBudgetSummaryAmount(int company, string month)
        {
            try
            {
                var allBudgetInsightData = await _userService.GetBudgetInsightAsync(company, month);
                if (!allBudgetInsightData.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                var finalResult = new List<object>();

                foreach (var user in allBudgetInsightData)
                {   
                    var combinedBudget = await _userService.GetCombinedBudgetsAsync(user.UserID, company, month);

                    var budgetArray = combinedBudget.BudgetData.Select(b =>
                    {
                        var detail = combinedBudget.BudgetDetails.FirstOrDefault(x => x.BudgetHeader?.BudgetId == b.BudgetId);

                        return new
                        {
                            budgetId = b.BudgetId,
                            objType = b.objType,
                            company = b.company,
                            docEntry = b.DocEntry,
                            objectName = b.ObjectName,
                            cardCode = b.CardCode,
                            cardName = b.CardName,
                            docDate = b.DocDate?.ToString(),
                            totalAmount = b.TotalAmount,
                            status = b.Status,
                            header = new
                            {
                                templateId = detail?.BudgetHeader?.TemplateId,
                                totalStage = detail?.BudgetHeader?.TotalStage,
                                currentStageId = detail?.BudgetHeader?.CurrentStageId,
                                currentStatus = detail?.BudgetHeader?.CurrentStatus
                            },
                            lines = detail?.BudgetLines?.Select(line => new
                            {
                                budget = line.Budget,
                                subBudget = line.SubBudget,
                                variety = line.variety,
                                acctCode = line.AcctCode,
                                acctName = line.AcctName,
                                lineNum = line.LineNum,
                                amount = line.Amount,
                                state = line.State,
                                EffectMonth = line.EffectMonth,
                                lineRemarks = line.LineRemarks,
                                comments = line.Comments
                            }).ToList()
                        };
                    }).ToList();

                    finalResult.Add(new
                    {
                        userId = user.UserID,
                        userName = user.UserName,
                        budget = budgetArray
                    });
                }

                _logger.LogInformation("Data retrieved and combined successfully.");
                return Ok(new { Success = true, Data = finalResult });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Success = false, Message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving budget insights.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetDocIdsUsingDocEntry")]
        public async Task<ActionResult> GetDocIdsUsingDocEntry(int docEntry)
        {
            try
            {
                var docIds = await _userService.GetDocIdsUsingDocEntryAsync(docEntry);
                if (docIds == null)
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = docIds });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flow.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetApprovedDocEntries")]
        public async Task<ActionResult> GetApprovedDocEntries(int company, int docEntry)
        {
            try
            {
                var docentry = await _userService.GetApprovedDocEntriesAsync(company, docEntry);
                if (!docentry.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = docentry });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data in Database.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetPendingDocEntries")]
        public async Task<ActionResult> GetPendingDocEntries(int company, int docEntry)
        {
            try
            {
                var docentry = await _userService.GetPendingDocEntriesAsync(company, docEntry);
                if (!docentry.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = docentry });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data in Database.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetRejectedDocEntries")]
        public async Task<ActionResult> GetRejectedDocEntries(int company, int docEntry)
        {
            try
            {
                var docentry = await _userService.GetRejectedDocEntriesAsync(company, docEntry);
                if (!docentry.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = docentry });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data in Database.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetAllDocEntries")]
        public async Task<ActionResult> GetAllDocEntries(int company, int docEntry)
        {
            try
            {
                var approved = await _userService.GetApprovedDocEntriesAsync(company, docEntry);
                var rejected = await _userService.GetRejectedDocEntriesAsync(company, docEntry);
                var pending = await _userService.GetPendingDocEntriesAsync(company, docEntry);

                var allData = new Dictionary<string, object>();

                if (approved != null && approved.Any())
                    allData["Approved"] = approved;

                if (rejected != null && rejected.Any())
                    allData["Rejected"] = rejected;

                if (pending != null && pending.Any())
                    allData["Pending"] = pending;

                if (!allData.Any())
                {
                    _logger.LogInformation("No data found.");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                _logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = allData });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data in Database.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" + ex });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" + ex });
            }
        }


        //for web
        [HttpPost("validateUsername")]
        public async Task<IActionResult> ValidateUsername(string userNAME)
        {
            var result = await _userService.ValidateUsernameAsync(userNAME);

            return Ok(result);
        }

        [HttpPost("UpdateUserInfo")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UserUpdateDTO request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(new { Success = false, Message = "Invalid request data." });
            }
            try
            {
                var result = await _userService.UpdateUserInfoAsync(request);
                if (result.Success)
                {
                    return Ok(new { Success = true, Message = "User information updated successfully." });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user information.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Optionally remove the session cookie
            if (Request.Cookies.ContainsKey(".AspNetCore.Session"))
            {
                Response.Cookies.Delete(".AspNetCore.Session");
            }

            // If you're using authentication (e.g., Identity or cookies):
            // await HttpContext.SignOutAsync();

            return Ok(new { success = true, message = "Successfully logged out" });
        }

        [HttpGet("getUsersByDepartment")]
        public async Task<ActionResult<ApiResponse<UserListResponseDto>>> GetUsersByDepartment(int deptId)
        {
            try
            {
                if (deptId <= 0)
                {
                    return BadRequest(new { Success = false, Message = "Invalid Dept Id" });
                }

                var result = await _userService.GetUsersByDepartmentId(deptId);

                if (result.Users == null || result.Users.Count == 0)
                {
                    return Ok(new { Success = true, Data = result });
                }

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}

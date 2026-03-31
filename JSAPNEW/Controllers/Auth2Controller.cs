using JSAPNEW.Data.Entities;
using System.ComponentModel.Design;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Auth2Controller : ControllerBase
    {
        private readonly IAuth2Service _auth2Service; //An interface for user-related operations
        private readonly ILogger<Auth2Controller> _logger; //for recording events or errors

        public Auth2Controller(IAuth2Service Auth2Service, ILogger<Auth2Controller> logger)
        {
            _auth2Service = Auth2Service;
            _logger = logger;
        }

        [HttpGet("getTemplateDataForCloning")]
        public async Task<IActionResult> GetTemplateDataForCloningAsync(int templateId, int company)
        {
            if (templateId <= 0)
                return BadRequest(new { Success = false, Message = "Invalid templateId" });

            if (company <= 0)
                return BadRequest(new { Success = false, Message = "Invalid company" });
            try
            {
                var result = await _auth2Service.GetTemplateDataForCloningAsync(templateId, company);

                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given templateId." });

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("CloneTemplateWithNewStages")]
        public async Task<IActionResult> CloneTemplateWithNewStages([FromBody] CloneTemplateModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _auth2Service.CloneTemplateWithNewStagesAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }

        }

        [HttpPost("CreateBudgetWithSubBudgets")]
        public async Task<IActionResult> CreateBudgetWithSubBudgets([FromBody] CreateBudget2Request request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request body" });
            try
            {
                var result = await _auth2Service.CreateBudgetWithSubBudgetsAsync(request);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("CreateMonthlyAllocations")]
        public async Task<IActionResult> CreateMonthlyAllocations([FromBody] CreateMonthlyAllocation2Request request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request body" });
            try
            {
                var result = await _auth2Service.CreateMonthlyAllocationsAsync(request);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetAllBudgets")]
        public async Task<IActionResult> GetAllBudgets(int company, bool isActive)
        {
            if (company <= 0)
                return BadRequest(new { Success = false, Message = "Invalid company" });
            try
            {
                var result = await _auth2Service.GetAllBudgetsAsync(company, isActive);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetBudgetAndSubBudgetDetails")]
        public async Task<IActionResult> GetBudgetAndSubBudgetDetails(int budgetId, int subBudgetId)
        {
            if (budgetId <= 0)
                return BadRequest(new { Success = false, Message = "Invalid budgetId" });
            if (subBudgetId <= 0)
                return BadRequest(new { Success = false, Message = "Invalid subBudgetId" });
            try
            {
                var result = await _auth2Service.GetBudgetAndSubBudgetDetailsAsync(budgetId, subBudgetId);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given budgetId and subBudgetId." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetBudgetWithSubBudgets")]
        public async Task<IActionResult> GetBudgetWithSubBudgets(int budgetId)
        {
            try
            {
                if (budgetId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid budgetId. It must be greater than zero."
                    });
                }

                var result = await _auth2Service.GetBudgetWithSubBudgetsAsync(budgetId);

                if (result == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bad Request"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Data successfully retrieve",
                    data = result
                });
            }
            catch (SqlException sqlEx)
            {
                // SQL-specific error handling
                return StatusCode(500, new
                {
                    success = false,
                    message = "A database error occurred while fetching the budget data.",
                    sqlError = sqlEx.Message,
                    sqlState = sqlEx.State,
                    sqlNumber = sqlEx.Number
                });
            }
            catch (Exception ex)
            {
                // General exception
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred while processing the request.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetDistinctBudgetAttributes")]
        public async Task<IActionResult> GetDistinctBudgetAttributes(string mode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Mode is required. Accepted values: BUDGET, SUB_BUDGET, BRANCH, PROCESSTAT, OBJTYPE, ACCTCODE, BUDGETDATE."
                    });
                }

                var result = await _auth2Service.GetDistinctBudgetAttributesAsync(mode.ToUpper());

                if (result == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bad Request"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Data successfully retrieve",
                    data = result.Data
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "A SQL database error occurred while fetching data.",
                    sqlError = sqlEx.Message,
                    sqlState = sqlEx.State,
                    sqlNumber = sqlEx.Number
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred while processing the request.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetSubBudgetsByBudgetId")]
        public async Task<IActionResult> GetSubBudgetsByBudgetId(int budgetId, bool? isActive)
        {
            try
            {
                if (budgetId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid budgetId. It must be greater than zero."
                    });
                }

                var result = await _auth2Service.GetSubBudgetsByBudgetIdAsync(budgetId, isActive);

                if (result == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bad Request"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Data successfully retrieve",
                    totalSubBudgets = result.TotalSubBudgets,
                    data = result.SubBudgets
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "A SQL database error occurred while fetching sub-budget data.",
                    sqlError = sqlEx.Message,
                    sqlState = sqlEx.State,
                    sqlNumber = sqlEx.Number
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred while processing the request.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetWorkflowActionSummary")]
        public async Task<IActionResult> GetWorkflowActionSummary(int templateId, int company)
        {
            if (templateId <= 0)
                return BadRequest(new { Success = false, Message = "Invalid templateId" });
            if (company <= 0)
                return BadRequest(new { Success = false, Message = "Invalid company" });
            try
            {
                var result = await _auth2Service.GetWorkflowActionSummaryAsync(templateId, company);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given templateId " });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("UpdateMonthlyAllocations")]
        public async Task<IActionResult> UpdateMonthlyAllocations([FromBody] UpdateMonthlyAllocationsRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request body" });
            try
            {
                var result = await _auth2Service.UpdateMonthlyAllocationsAsync(request);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetBudgetMonthlyAllocationView")]
        public async Task<IActionResult> GetBudgetMonthlyAllocationView(string budgetName, DateTime allocationMonth)
        {
            if (string.IsNullOrWhiteSpace(budgetName))
                return BadRequest(new { Success = false, Message = "Invalid budgetName" });
            try
            {
                var result = await _auth2Service.GetBudgetMonthlyAllocationViewAsync(budgetName, allocationMonth);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given budgetName and allocationMonth." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("getAllBudgetTypes")]
        public async Task<IActionResult> GetAllTypeBudgetAsync()
        {
            try
            {
                var budgets = await _auth2Service.GetAllTypeBudgetAsync();
                if (!budgets.Any())
                {
                    return NotFound(new { Success = false, message = "No data found" });
                }
                return Ok(new { Success = true, data = budgets });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }

        }

        [HttpGet("getSubBudgetByBudget")]
        public async Task<IActionResult> GetSubBudgetByBudgetAsync([FromQuery] string budget)
        {
            try
            {
                var subBudgets = await _auth2Service.GetSubBudgetByBudgetAsync(budget);

                if (!subBudgets.Any())
                {
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                return Ok(new { Success = true, Data = subBudgets });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetPendingBudgetAllocationRequests")]
        public async Task<IActionResult> GetPendingBudgetAllocationRequests(int userId, int companyId, string month)
        {
            try
            {
                var result = await _auth2Service.GetPendingBudgetAllocationRequestsAsync(userId, companyId, month);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given allocationMonth." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }


        [HttpGet("GetApprovedBudgetAllocationRequests")]
        public async Task<IActionResult> GetApprovedBudgetAllocationRequests(int userId, int companyId, string month)
        {
            try
            {
                var result = await _auth2Service.GetApprovedBudgetAllocationRequestsAsync(userId, companyId, month);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given allocationMonth." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetRejectedBudgetAllocationRequests")]
        public async Task<IActionResult> GetRejectedBudgetAllocationRequests(int userId, int companyId, string month)
        {
            try
            {
                var result = await _auth2Service.GetRejectedBudgetAllocationRequestsAsync(userId, companyId, month);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given allocationMonth." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("approveBudgetAllocation")]
        public async Task<IActionResult> ApproveBudgetAllocation([FromBody] ApproveBudgetAllocationRequest request)
        {
            if (request == null)
                return BadRequest(new { Success = false, Message = "Request cannot be null" });

          

            try
            {
                var result = await _auth2Service.ApproveBudgetAllocationRequestAsync(request);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given request." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }


        [HttpPost("rejectBudgetAllocation")]
        public async Task<IActionResult> RejectBudgetAllocation([FromBody] RejectBudgetAllocationRequest request)
        {
            if (request == null)
                return BadRequest(new { Success = false, Message = "Request cannot be null" });

           
            try
            {
                var result = await _auth2Service.RejectBudgetAllocationRequestAsync(request);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given request." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("createBudgetAllocation")]
        public async Task<IActionResult> CreateBudgetAllocationRequest([FromBody] CreateBudgetAllocationRequestModel request)
        {
            if (request == null)
                return BadRequest(new { Success = false, Message = "Request cannot be null" });

            if (request.BudgetAllocationId <= 0)
                return BadRequest(new { Success = false, Message = "BudgetAllocationId must be greater than 0" });

            if (request.NewAmount <= 0)
                return BadRequest(new { Success = false, Message = "NewAmount must be greater than 0" });

            if (request.CreatedBy <= 0)
                return BadRequest(new { Success = false, Message = "CreatedBy must be a valid user ID" });

            
            try
            {
                var result = await _auth2Service.CreateBudgetAllocationRequestAsync(request);
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found for given request." });
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetBudgetAllocationRequestDetail")]
        public async Task<IActionResult> GetBudgetAllocationRequestDetail([FromQuery] int requestId)
        {
            try
            {
                if (requestId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid request ID" });
                }

                var result = await _auth2Service.GetBudgetAllocationRequestDetail(requestId);

                if (result == null || result.RequestDetail == null)
                {
                    return NotFound(new { success = false, message = "Budget allocation request not found" });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching budget allocation request detail for RequestId: {RequestId}", requestId);

                // Return actual error for debugging (remove in production)
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("getMonthlyAllocationInsights")]
        public async Task<IActionResult> GetBudgetMonthlyAllocationInsights(int userId, int company, string month)
        {
            try
            {
                if (userId <= 0 || company <= 0 || string.IsNullOrEmpty(month))
                {
                    return BadRequest("Invalid parameters");
                }

                var result = _auth2Service.GetBudgetMonthlyAllocationInsights(userId, company, month);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = true, message = ex.Message });
            }
        }


        [HttpGet("GetAllBudgetAllocationRequests")]
        public async Task<IActionResult> GetAllBudgetAllocationRequests(int userId, int companyId, string month = null)
        {
            // Add this to see what parameters are received
            System.Diagnostics.Debug.WriteLine($"userId: {userId}, companyId: {companyId}, month: {month}");

            try
            {
                var result = await _auth2Service.GetAllBudgetAllocationRequestsAsync(userId, companyId, month);

                // Change this check - don't use Any() on potentially null
                if (result == null)
                    return NotFound(new { Success = false, Message = "No data found." });

                var resultList = result.ToList();
                if (resultList.Count == 0)
                    return Ok(new { Success = true, TotalCount = 0, Data = resultList, Message = "No records found" });

                return Ok(new
                {
                    Success = true,
                    TotalCount = resultList.Count,
                    Data = resultList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error: " + ex.Message
                });
            }
        }
        [HttpGet("GetBudgetAllocationFlow")]
        public async Task<IActionResult> GetBudgetAllocationFlow(int flowId)
        {
            try
            {
                if (flowId <= 0)
                    return BadRequest(new { Success = false, Message = "Invalid flowId" });

                var result = await _auth2Service.GetBudgetAllocationFlowAsync(flowId);

                if (result == null || !result.Any())
                    return NotFound(new { Success = false, Message = "No flow data found for given flowId." });

                return Ok(new
                {
                    Success = true,
                    TotalCount = result.Count(),
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Sql Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }
    }
}

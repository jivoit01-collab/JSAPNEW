using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IReportsService _reportsService; //An interface for bom-related operations
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ReportsController> _reportslogger; //for recording events or errors

        public ReportsController(IConfiguration configuration, IReportsService reportsService, ILogger<ReportsController> reportslogger)
        {
            _configuration = configuration;
            _reportsService = reportsService;
            _reportslogger = reportslogger;

        }

        [HttpGet("GetRealiseReport")]
        public async Task<ActionResult> GetRealiseReport(DateTime FROMDATE, DateTime TODATE)
        {
            try
            {
                var result = await _reportsService.GetRealiseReportAsync(FROMDATE, TODATE);
                if (!result.Any())
                {
                    _reportslogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _reportslogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _reportslogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _reportslogger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetVariety")]
        public async Task<ActionResult> GetVariety()
        {
            try
            {
                var result = await _reportsService.GetVarietyAsync();
                if (!result.Any())
                {
                    _reportslogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _reportslogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _reportslogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _reportslogger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }
        [HttpGet("GetBrand")]
        public async Task<ActionResult> GetBrand()
        {
            try
            {
                var result = await _reportsService.GetBrandAsync();
                if (!result.Any())
                {
                    _reportslogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _reportslogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _reportslogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _reportslogger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetApprovalStatusReport")]
        public async Task<ActionResult> GetApprovalStatusReport(int userId, int company, string month)
        {
            try
            {
                var result = await _reportsService.GetApprovalStatusReportAsync(userId, company, month);
                if (result == null || (!result.Advance.Any() && !result.BOMs.Any()))
                {
                    _reportslogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _reportslogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _reportslogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _reportslogger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetBudgetByCompany")]
        public async Task<IActionResult> GetBudgetByCompany(int company,int docEntry = 0,string? cardName = null,string? month = null,string? status = null)
        {
            try
            {
                if (company <= 0)
                {
                    return BadRequest(new { Success = false, message = "Company ID must be greater than 0" });
                }

                var result = await _reportsService.GetBudgetByCompanyAsync(
                    company,
                    docEntry,
                    cardName,
                    month,
                    status);



                return Ok(new { Success = true, data = result });
            }
            catch (Exception ex)
            {

                return BadRequest(new { Success = false, message = "An error occurred while retrieving budget data" });
            }
        }

    }
}

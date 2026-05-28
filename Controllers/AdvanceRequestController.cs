using Microsoft.AspNetCore.Mvc;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using ServiceStack.Text;
using Microsoft.Extensions.Hosting.Internal;
using System.Linq;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvanceRequestController : ControllerBase
    {
        private readonly IAdvanceRequestService _advanceService; //An interface for user-related operations
        private readonly ILogger<AdvanceRequestController> _advancelogger; //for recording events or errors
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IBom2Service _bom2Service;
        public AdvanceRequestController(IAdvanceRequestService advanceService, ILogger<AdvanceRequestController> advancelogger, IWebHostEnvironment hostingEnvironment, IBom2Service bom2Service)
        {
            _advanceService = advanceService;
            _advancelogger = advancelogger;
            _hostingEnvironment = hostingEnvironment;
            _bom2Service = bom2Service;

        }


        [HttpGet("GetAdvancePaymentRequest")]
       /// [CheckUserPermission("ADV", "view")]
        public async Task<ActionResult> AdvancePaymentRequest(string IN_BRANCH, string IN_TYPE)
        {
            try
            {
                var result = await _advanceService.AdvancePaymentRequestAsync(IN_BRANCH, IN_TYPE);
                if (!result.Any())
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("InsertVendorExpense")]
       // [CheckUserPermission("ADV", "create")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> InsertVendorExpenseForm([FromForm] VendorExpenseRequest request, [FromForm] List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            List<FileDetails> fileDetailsList = new();

            if (files != null && files.Any())
            {
                string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", "Advancepayment");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var file in files)
                {
                    string uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    string relativeFolderPath = Path.Combine("Uploads", "Advancepayment").Replace("\\", "/");

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    fileDetailsList.Add(new FileDetails
                    {
                        AttachmentName = uniqueFileName,
                        AttachmentExtension = Path.GetExtension(file.FileName),
                        AttachmentPath = "/" + relativeFolderPath,
                        AttachmentSize = file.Length // ✅ use bytes
                    });
                }
            }

            try
            {
                var result = await _advanceService.InsertVendorExpenseAsync(request, fileDetailsList);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        /* public async Task<IActionResult> InsertVendorExpense([FromForm] VendorExpenseRequest request, [FromForm] List<IFormFile> files)
         {
             if (!ModelState.IsValid)
             {
                 return BadRequest(new
                 {
                     Success = false,
                     Message = "Invalid request data.",
                     Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                 });
             }

             List<FileDetails> fileDetailsList = new List<FileDetails>();

             if (files != null && files.Any())
             {
                 string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", "Advancepayment");
                 if (!Directory.Exists(uploadsFolder))
                     Directory.CreateDirectory(uploadsFolder);

                 foreach (var file in files)
                 {
                     string uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                     string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                     string relativeFolderPath = Path.Combine("Uploads", "Advancepayment").Replace("\\", "/");

                     using (var stream = new FileStream(filePath, FileMode.Create))
                     {
                         await file.CopyToAsync(stream);
                     }

                     fileDetailsList.Add(new FileDetails
                     {
                         AttachmentName = uniqueFileName,
                         AttachmentExtension = Path.GetExtension(file.FileName),
                         AttachmentPath = "/" + relativeFolderPath,
                         AttachmentSize = Math.Round((decimal)file.Length / 1024, 2)
                     });
                 }
             }

             try
             {
                 var result = await _advanceService.InsertVendorExpenseAsync(request, fileDetailsList);
                 return Ok(result);
             }
             catch (Exception ex)
             {
                 return BadRequest(new
                 {
                     Success = false,
                     Message = ex.InnerException?.Message ?? ex.Message
                 });
             }
         }

 */

        [HttpGet("GetPendingExpenses")]
        //[CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetPendingExpenses(int userId, int companyId)
        {
            try
            {
                var result = await _advanceService.GetPendingExpensesAsync(userId, companyId);
                if (!result.Any())
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }
        [HttpGet("GetApprovedExpenses")]
       // [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetApprovedExpenses(int userId, int companyId)
        {
            try
            {
                var result = await _advanceService.GetApprovedExpensesAsync(userId, companyId);
                if (!result.Any())
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }
        [HttpGet("GetRejectedExpenses")]
       // [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetRejectedExpenses(int userId, int companyId)
        {
            try
            {
                var result = await _advanceService.GetRejectedExpensesAsync(userId, companyId);
                if (!result.Any())
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }
        [HttpGet("GetExpenseInsights")]
       // [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetExpenseInsights(int userId, int companyId)
        {
            try
            {
                var result = await _advanceService.GetExpenseInsightsAsync(userId, companyId);
                if (!result.Any())
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }
        [HttpGet("GetTotalExpenses")]
       // [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetTotalExpenses(int userId, int companyId)
        {
            try
            {
                var result = await _advanceService.GetTotalExpensesAsync(userId, companyId);
                if (!result.Any())
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetExpenseDetailsByFlowId")]
       // [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetExpenseDetailsByFlowId(int flowId)
        {
            try
            {
                var urlHelper = Url; // Get the IUrlHelper instance
                var result = await _advanceService.GetExpenseDetailsByFlowIdAsync(flowId, urlHelper);
                if (result == null)
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("ApproveAdvPay")]
      //  [CheckUserPermission("ADV", "approver")]
        public async Task<IActionResult> ApproveAdvancePayment([FromBody] ApproveAdvPayRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Invalid request payload." });
            }

            if (string.IsNullOrWhiteSpace(request.action) ||
                !(request.action.Equals("Approve", StringComparison.OrdinalIgnoreCase) ||
                  request.action.Equals("Revoke", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { success = false, message = "Action must be 'Approve' or 'Revoke'." });
            }

            var (isSuccess, message) = await _advanceService.ApproveAdvancePaymentAsync(request);

            if (isSuccess)
            {
                return Ok(new { success = true, message });
            }
            else
            {
                return StatusCode(500, new { success = false, message });
            }
        }

        [HttpPost("RejectedAdvPay")]
      //  [CheckUserPermission("ADV", "approver")]
        public async Task<IActionResult> RejectAdvancePayment([FromBody] RejectAdvPayRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request" });

            try
            {
                var result = await _advanceService.RejectAdvancePaymentAsync(request);
                return Ok(new { success = result.Success, message = result.Message });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("DeleteVendorExpense")]
      //  [CheckUserPermission("ADV", "delete")]
        public async Task<IActionResult> DeleteVendorExpense(int id, int deletedBy)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AdvanceResponse
                {
                    Message = "Invalid request.",
                    Success = false
                });

            AdvanceResponse result = await _advanceService.DeleteVendorExpenseAsync(id, deletedBy);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }

        [HttpPost("UpdateVendorExpense")]
       // [CheckUserPermission("ADV", "update")]
        public async Task<IActionResult> UpdateVendorExpense([FromBody] VendorExpenseUpdateModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AdvanceResponse
                {
                    Message = "Invalid request data.",
                    Success = false
                });
            }

            AdvanceResponse result = await _advanceService.UpdateVendorExpenseAsync(request);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }

        [HttpGet("GetDepartments")]
      //  [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var result = await _advanceService.GetDepartmentsAsync();
                if (result == null)
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetCustomerBalanceByBranch")]
     //   [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetCustomerBalanceByBranch(string IN_BRANCH, string IN_CARDCODE)
        {
            try
            {
                var result = await _advanceService.GetCustomerBalanceByBranchAsync(IN_BRANCH, IN_CARDCODE);
                if (result == null)
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetExpenseByUserId")]
      //  [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetExpenseByUserId(int userId, int company,string month)
        {
            try
            {
                var result = await _advanceService.GetExpenseByUserIdAsync(userId, company,month);
                if (result == null)
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("UpdateAmount")]
      //  [CheckUserPermission("ADV", "update")]
        public async Task<IActionResult> UpdateAmount(int userId, int expenseId, float amount)
        {
            if (userId <= 0 || expenseId <= 0 || amount <= 0)
            {
                return BadRequest(new AdvanceResponse
                {
                    Message = "Invalid request data.",
                    Success = false
                });
            }

            var result = await _advanceService.UpdateAmountAsync(userId, expenseId, amount);
            return Ok(new
            {
                success = result.Success,
                message = result.Message
            });
        }

        [HttpGet("GetExpenseApprovalFlow")]
      //  [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetExpenseApprovalFlow(int flowId)
        {
            if (flowId <= 0)
            {
                return BadRequest(new { Success = false, Message = "Invalid flow ID." });
            }
            try
            {
                var result = await _advanceService.GetExpenseApprovalFlowAsync(flowId);
                if (result == null || !result.Any())
                {
                    _advancelogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _advancelogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _advancelogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _advancelogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("SendPendingPaymentNotify")]
     //   [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> SendPendingCountNotification()
        {
            var response = await _advanceService.SendPaymentPendingCountNotificationAsync();
            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpGet("GetBusinessPartners")]
      //  [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetBusinessPartnersAsync(int company, string type)
        {
            try
            {
                if (company == 1)
                {
                    var result = await _advanceService.GetOilBusinessPartnersAsync(type);
                    return Ok(result); // Strongly typed: List<OilBusinessPartnerModel>
                }
                else if (company == 2)
                {
                    var result = await _advanceService.GetBevBusinessPartnersAsync(type);
                    return Ok(result); // Strongly typed: List<BevBusinessPartnerModel>
                }
                else
                {
                    return BadRequest("Invalid company value.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("GetPurchaseOrders")]
      //  [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetOpenOrders([FromQuery] int company, string cardCode)
        {
            if (string.IsNullOrEmpty(cardCode))
                return BadRequest("CardCode is required.");

            // 1. Fetch SAP POs
            var sapPoList = await _advanceService.GetOpenOrdersByVendorAsync(company, cardCode);

            // 2. Fetch used DocNums from your database
            var usedPoList = await _advanceService.GetPoListAsync(company, cardCode); // returns List<PoListModel>

            // 3. Extract only PO numbers (DocNum) from DB
            // Safely parse used POs from DB into int HashSet
            var usedDocNums = new HashSet<int>(
                usedPoList
                    .Select(p => p.Po)
                    .Where(po => int.TryParse(po, out _))
                    .Select(po => int.Parse(po))
            );

            // Now filter SAP list using integer comparison
            var filteredSapPoList = sapPoList
                .Where(po => !usedDocNums.Contains(po.DocNum))
                .ToList();

            return Ok(filteredSapPoList);
        }

        [HttpGet("GetCreatedPurchaseOrders")]
      //  [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetCreatedOpenOrders([FromQuery] int company, string cardCode)
        {
            if (company <= 0)
                return BadRequest(new GetApiResponse<object>
                {
                    Success = false,
                    Message = "Company is required."
                });

            if (string.IsNullOrWhiteSpace(cardCode))
                return BadRequest(new GetApiResponse<object>
                {
                    Success = false,
                    Message = "CardCode is required."
                });

            // 1. Fetch SAP POs
            var sapPoList = await _advanceService.GetCreatedOpenOrdersByVendorAsync(company, cardCode);

            // 2. Fetch used DocNums from your database (use correct company)
            var usedPoList = await _advanceService.GetPoListAsync(company, cardCode);

            // 3. Extract PO numbers from DB and convert to HashSet<int>
            var usedDocNums = new HashSet<int>(
                usedPoList
                    .Select(p => p.Po)
                    .Where(po => int.TryParse(po, out _))
                    .Select(po => int.Parse(po))
            );

            // 4. Return only SAP POs that are in the used list
            var filteredSapPoList = sapPoList
                .Where(po => usedDocNums.Contains(po.DocNum))
                .ToList();

            return Ok(filteredSapPoList);
        }

        [HttpGet("GetPoList")]
      //  [CheckUserPermission("ADV", "view")]
        public async Task<IActionResult> GetPoList(int company, string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Code is required.");

            var result = await _advanceService.GetPoListAsync(company, code);
            return Ok(result);
        }
    }
}

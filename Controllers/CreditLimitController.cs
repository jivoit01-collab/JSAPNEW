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

    public class CreditLimitController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ICreditLimitService _CreditLimitService;
        private readonly ILogger<CreditLimitController> _CreditLimitlogger;
        public CreditLimitController(IConfiguration configuration, ICreditLimitService creditLimitService, ILogger<CreditLimitController> creditLimitlogger)
        {
            _configuration = configuration;
            _CreditLimitService = creditLimitService;
            _CreditLimitlogger = creditLimitlogger;
        }

        [HttpPost("OpenCslm")]
        public async Task<ActionResult<OpenCslmResponse>> OpenCslm([FromBody] OpenCslmRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new OpenCslmResponse { Success = false, Message = "Invalid request" });
                }
                var result = await _CreditLimitService.OpenCslmAsync(request);
                if (result == null)
                {
                    _CreditLimitlogger.LogInformation("Failed to open CSLM");
                    return NotFound(new OpenCslmResponse { Success = false, Message = "Failed to open CSLM" });
                }
                _CreditLimitlogger.LogInformation("CSLM opened successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while opening CSLM.");
                return StatusCode(500, new OpenCslmResponse { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetCustomerCards")]
        public async Task<ActionResult> GetCustomerCards(int company)
        {
            try
            {
                var customerCards = await _CreditLimitService.GetCustomerCardsAsync(company);
                if (customerCards == null || !customerCards.Any())
                {
                    _CreditLimitlogger.LogInformation("No customer cards found");
                    return NotFound(new { Success = false, Message = "No customer cards found" });
                }
                _CreditLimitlogger.LogInformation("Customer cards retrieved successfully.");
                return Ok(new { Success = true, Data = customerCards });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving customer cards.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("CreateCLDocument")]
        public async Task<ActionResult<CreateDocumentResult>> CreateDocument([FromBody] CreateDocumentDto request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new CreateDocumentResult { Success = false, Message = "Invalid request" });
                }
                var result = await _CreditLimitService.CreateDocumentAsync(request);
                if (result == null)
                {
                    _CreditLimitlogger.LogInformation("Failed to create document");
                    return NotFound(new CreateDocumentResult { Success = false, Message = "Failed to create document" });
                }
                _CreditLimitlogger.LogInformation("Document created successfully.");
                return Ok(result);
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while creating document.");
                return StatusCode(500, new CreateDocumentResult { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while creating document.");
                return StatusCode(500, new CreateDocumentResult { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("CreateCLDocumentV2")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CreateDocumentResult>> CreateDocumentV2([FromForm] string documentData, [FromForm] IFormFile? attachment)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<CreateDocumentDtoV2>(documentData);
                if (request == null)
                {
                    return BadRequest(new CreateDocumentResult
                    {
                        Success = false,
                        Message = "Invalid request"
                    });
                }

                // If only one credit limit entry, attachment is mandatory
                if (request.TotalEntries == 1 && attachment == null)
                {
                    return BadRequest(new CreateDocumentResult
                    {
                        Success = false,
                        Message = "Attachment is required when submitting a single credit limit entry."
                    });
                }

                var result = await _CreditLimitService.CreateDocumentWithAttachmentAsyncV2(
                    request,
                    attachment
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error creating credit limit document");
                return StatusCode(500, new CreateDocumentResult
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }


        [HttpPost("GetApprovedDocuments")]
        public async Task<ActionResult> GetApprovedDocuments([FromBody] CLDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var documents = await _CreditLimitService.GetApprovedDocumentsAsync(request);
                if (documents == null || !documents.Any())
                {
                    _CreditLimitlogger.LogInformation("No approved documents found");
                    return NotFound(new { Success = false, Message = "No approved documents found" });
                }
                _CreditLimitlogger.LogInformation("Approved documents retrieved successfully.");
                return Ok(new { Success = true, Data = documents });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving approved documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving approved documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }


        [HttpPost("GetPendingDocuments")]
        public async Task<ActionResult> GetPendingDocuments([FromBody] CLDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var documents = await _CreditLimitService.GetPendingDocumentsAsync(request);
                if (documents == null || !documents.Any())
                {
                    _CreditLimitlogger.LogInformation("No pending documents found");
                    return NotFound(new { Success = false, Message = "No pending documents found" });
                }
                _CreditLimitlogger.LogInformation("Pending documents retrieved successfully.");
                return Ok(new { Success = true, Data = documents });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving pending documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving pending documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpPost("GetRejectedDocuments")]
        public async Task<ActionResult> GetRejectedDocuments([FromBody] CLDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var documents = await _CreditLimitService.GetRejectedDocumentsAsync(request);
                if (documents == null || !documents.Any())
                {
                    _CreditLimitlogger.LogInformation("No rejected documents found");
                    return NotFound(new { Success = false, Message = "No rejected documents found" });
                }
                _CreditLimitlogger.LogInformation("Rejected documents retrieved successfully.");
                return Ok(new { Success = true, Data = documents });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving rejected documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving rejected documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpPost("GetAllDocuments")]
        public async Task<ActionResult> GetAllDocuments([FromBody] CLDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var documents = await _CreditLimitService.GetAllDocumentsAsync(request);
                if (documents == null || !documents.Any())
                {
                    _CreditLimitlogger.LogInformation("No documents found");
                    return NotFound(new { Success = false, Message = "No documents found" });
                }
                _CreditLimitlogger.LogInformation("All documents retrieved successfully.");
                return Ok(new { Success = true, Data = documents });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving all documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving all documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("GetCreditDocumentInsight")]
        public async Task<ActionResult> GetCreditDocumentInsight([FromBody] CLDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var insights = await _CreditLimitService.GetCreditDocumentInsightAsync(request);
                if (insights == null || !insights.Any())
                {
                    _CreditLimitlogger.LogInformation("No credit document insights found");
                    return NotFound(new { Success = false, Message = "No credit document insights found" });
                }
                _CreditLimitlogger.LogInformation("Credit document insights retrieved successfully.");
                return Ok(new { Success = true, Data = insights });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving credit document insights.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving credit document insights.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("GetUserDocumentInsights")]
        public async Task<ActionResult> GetUserDocumentInsights([FromBody] UserDocumentInsightsRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var insights = await _CreditLimitService.GetUserDocumentInsightsAsync(request);
                if (insights == null || !insights.Any())
                {
                    _CreditLimitlogger.LogInformation("No user document insights found");
                    return NotFound(new { Success = false, Message = "No user document insights found" });
                }
                _CreditLimitlogger.LogInformation("User document insights retrieved successfully.");
                return Ok(new { Success = true, Data = insights });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving user document insights.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving user document insights.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        /// below code will be comment
        [HttpGet("GetDocumentDetail")]
        public async Task<ActionResult> GetDocumentDetail(int documentId)
        {
            try
            {
                if (documentId <= 0)
                {
                    _CreditLimitlogger.LogWarning("Invalid document ID");
                    return BadRequest(new { Success = false, Message = "Invalid document ID" });
                }
                var detail = await _CreditLimitService.GetDocumentDetailAsync(documentId);
                if (detail == null)
                {
                    _CreditLimitlogger.LogInformation("Document detail not found");
                    return NotFound(new { Success = false, Message = "Document detail not found" });
                }
                _CreditLimitlogger.LogInformation("Document detail retrieved successfully.");
                return Ok(new { Success = true, Data = detail });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving document detail.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving document detail.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpGet("GetDocumentDetailV2")]
        public async Task<IActionResult> GetCreditDocumentDetailV2(int documentId)
        {
            try
            {
                var data = await _CreditLimitService.GetCreditDocumentDetailAsyncV2(documentId, Url);

                if (data == null)
                    return NotFound(new { Success = false, Message = "Document not found" });

                return Ok(new
                {
                    Success = true,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error fetching credit document detail");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetApprovalFlow")]
        public async Task<ActionResult> GetApprovalFlow(long flowId)
        {
            try
            {
                if (flowId <= 0)
                {
                    _CreditLimitlogger.LogWarning("Invalid flow ID");
                    return BadRequest(new { Success = false, Message = "Invalid flow ID" });
                }
                var flow = await _CreditLimitService.GetApprovalFlowAsync(flowId);
                if (flow == null || !flow.Any())
                {
                    _CreditLimitlogger.LogInformation("Approval flow not found");
                    return NotFound(new { Success = false, Message = "Approval flow not found" });
                }
                _CreditLimitlogger.LogInformation("Approval flow retrieved successfully.");
                return Ok(new { Success = true, Data = flow });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving approval flow.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving approval flow.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("GetUserDocumentsByCreatedByAndMonth")]
        public async Task<ActionResult> GetUserDocumentsByCreatedByAndMonth([FromBody] UserDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var documents = await _CreditLimitService.GetUserDocumentsAsync(request);
                if (documents == null || !documents.Any())
                {
                    _CreditLimitlogger.LogInformation("No user documents found");
                    return NotFound(new { Success = false, Message = "No user documents found" });
                }
                _CreditLimitlogger.LogInformation("User documents retrieved successfully.");
                return Ok(new { Success = true, Data = documents });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving user documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving user documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("ApproveDocument")]
        public async Task<IActionResult> ApproveDocument([FromBody] ApproveDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new CreditLimitApiResponse
                    {
                        Success = false,
                        Message = "Invalid request"
                    });
                }

                // Step 1: Approve document
                var result = await _CreditLimitService.ApproveDocumentAsync(request);
                if (result == null || !result.Success)
                {
                    _CreditLimitlogger.LogInformation("Failed to approve document");
                    return NotFound(new CreditLimitApiResponse
                    {
                        Success = false,
                        Message = "Failed to approve document"
                    });
                }

                _CreditLimitlogger.LogInformation("Document approved successfully.");

                // Step 2: Trigger HANA update after approval
                string hanaUpdateResult = "";
                if (result.Success)
                {
                    _CreditLimitlogger.LogInformation($"Triggering HANA credit limit update for FlowId: {request.FlowId}");

                    hanaUpdateResult = await _CreditLimitService.UpdateCreditLimitAsync(request.FlowId);

                    if (!hanaUpdateResult.StartsWith("Credit Limit updated"))
                    {
                        _CreditLimitlogger.LogError($"HANA update failed: {hanaUpdateResult}");
                    }
                    else
                    {
                        _CreditLimitlogger.LogInformation($"HANA update successful: {hanaUpdateResult}");
                    }
                }

                // Step 3: Return consolidated response
                return Ok(new
                {
                    Success = true,
                    FlowId = request.FlowId,
                    Message = "Flow approved successfully.",
                    HanaStatusText = string.IsNullOrEmpty(hanaUpdateResult) ? "HANA not triggered" : hanaUpdateResult
                });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "SQL error occurred while approving document.");
                return StatusCode(500, new CreditLimitApiResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Unexpected error occurred while approving document.");
                return StatusCode(500, new CreditLimitApiResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("RejectDocument")]
        public async Task<ActionResult<CreditLimitApiResponse>> RejectDocument([FromBody] RejectDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new CreditLimitApiResponse { Success = false, Message = "Invalid request" });
                }
                var result = await _CreditLimitService.RejectDocumentAsync(request);
                if (result == null)
                {
                    _CreditLimitlogger.LogInformation("Failed to reject document");
                    return NotFound(new CreditLimitApiResponse { Success = false, Message = "Failed to reject document" });
                }
                _CreditLimitlogger.LogInformation("Document rejected successfully.");
                return Ok(result);
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while rejecting document.");
                return StatusCode(500, new CreditLimitApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while rejecting document.");
                return StatusCode(500, new CreditLimitApiResponse { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetDocumentDetailUsingFlowId")]
        public async Task<ActionResult> GetDocumentDetailUsingFlowId(int flowId)
        {
            try
            {
                if (flowId <= 0)
                {
                    _CreditLimitlogger.LogWarning("Invalid flowId");
                    return BadRequest(new { Success = false, Message = "Invalid flowId" });
                }
                var detail = await _CreditLimitService.GetDocumentDetailUsingFlowIdAsync(flowId);
                if (detail == null)
                {
                    _CreditLimitlogger.LogInformation("Document detail not found");
                    return NotFound(new { Success = false, Message = "Document detail not found" });
                }
                _CreditLimitlogger.LogInformation("Document detail retrieved successfully.");
                return Ok(new { Success = true, Data = detail });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving document detail.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving document detail.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetFlowStatus")]
        public async Task<ActionResult> GetFlowStatus(int flowId)
        {
            try
            {
                if (flowId <= 0)
                {
                    _CreditLimitlogger.LogWarning("Invalid flow ID");
                    return BadRequest(new { Success = false, Message = "Invalid flow ID" });
                }
                var status = await _CreditLimitService.GetFlowStatusAsync(flowId);
                if (status == null)
                {
                    _CreditLimitlogger.LogInformation("Flow status not found");
                    return NotFound(new { Success = false, Message = "Flow status not found" });
                }
                _CreditLimitlogger.LogInformation("Flow status retrieved successfully.");
                return Ok(new { Success = true, Data = status });
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving flow status.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving flow status.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("UpdateHanaStatus")]
        public async Task<ActionResult<CreditLimitApiResponse>> UpdateHanaStatus([FromBody] CreditLimitUpdateHanaStatus request)
        {
            try
            {
                if (request == null)
                {
                    _CreditLimitlogger.LogWarning("Request is null");
                    return BadRequest(new CreditLimitApiResponse { Success = false, Message = "Invalid request" });
                }
                var result = await _CreditLimitService.UpdateHanaStatusAsync(request);
                if (result == null)
                {
                    _CreditLimitlogger.LogInformation("Failed to update HANA status");
                    return NotFound(new CreditLimitApiResponse { Success = false, Message = "Failed to update HANA status" });
                }
                _CreditLimitlogger.LogInformation("HANA status updated successfully.");
                return Ok(result);
            }
            catch (SqlException ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while updating HANA status.");
                return StatusCode(500, new CreditLimitApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while updating HANA status.");
                return StatusCode(500, new CreditLimitApiResponse { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("UpdateCreditLimitInHana")]
        public async Task<IActionResult> UpdateCreditLimit(int flowId)
        {
            try
            {
                var result = await _CreditLimitService.UpdateCreditLimitAsync(flowId);
                if (result.StartsWith("Credit Limit updated"))
                    return Ok(new { Success = true, Message = result });
                return BadRequest(new { Success = false, Message = result });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error updating Credit Limit in HANA");
                return StatusCode(500, new { Success = false, Message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("GetCLUserIdsSendNotifications")]
        public async Task<ActionResult> GetCLUserIdsSendNotifications(int flowId)
        {
            try
            {
                var userIds = await _CreditLimitService.GetCLUserIdsSendNotificatiosAsync(flowId);
                if (userIds == null || !userIds.Any())
                {
                    _CreditLimitlogger.LogInformation("No user IDs found for notifications");
                    return NotFound(new { Success = false, Message = "No user IDs found for notifications" });
                }
                _CreditLimitlogger.LogInformation("User IDs for notifications retrieved successfully.");
                return Ok(new { Success = true, Data = userIds });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving user IDs for notifications.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpGet("SendPendingCLCountNotification")]
        public async Task<ActionResult<CreditLimitApiResponse>> SendPendingCLCountNotification()
        {
            try
            {
                var result = await _CreditLimitService.SendPendingCLCountNotificationAsync();
                if (result == null)
                {
                    _CreditLimitlogger.LogInformation("Failed to send pending CL count notification");
                    return NotFound(new CreditLimitApiResponse { Success = false, Message = "Failed to send pending CL count notification" });
                }
                _CreditLimitlogger.LogInformation("Pending CL count notification sent successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while sending pending CL count notification.");
                return StatusCode(500, new CreditLimitApiResponse { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetCurrentUsersSendNotification")]
        public async Task<ActionResult> GetCurrentUsersSendNotification(int userDocumentId)
        {
            try
            {
                var userIds = await _CreditLimitService.GetCurrentUsersSendNotificationAsync(userDocumentId);
                if (userIds == null || !userIds.Any())
                {
                    _CreditLimitlogger.LogInformation("No user IDs found for notifications");
                    return NotFound(new { Success = false, Message = "No user IDs found for notifications" });
                }
                _CreditLimitlogger.LogInformation("User IDs for notifications retrieved successfully.");
                return Ok(new { Success = true, Data = userIds });
            }
            catch (Exception ex)
            {
                _CreditLimitlogger.LogError(ex, "Error occurred while retrieving user IDs for notifications.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}

using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Sap.Data.Hana;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QcController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QcController> _logger;
        private readonly IQcService _QcService;

        public QcController(IConfiguration configuration, IQcService QcService, ILogger<QcController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _QcService = QcService;
        }

        [HttpPost("CreateForm")]
        public async Task<IActionResult> CreateForm([FromBody] CreateFormRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is null.");
            }
            try
            {
                var result = await _QcService.CreateFormAsync(request);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while creating QC form.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating QC form.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("CreateParameter")]
        public async Task<IActionResult> CreateParameter([FromBody] CreateParameterRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is null.");
            }
            try
            {
                var result = await _QcService.CreateParameterAsync(request);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while creating QC parameter.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating QC parameter.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("CreateSubParameter")]
        public async Task<IActionResult> CreateSubParameter([FromBody] CreateSubParameterRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is null.");
            }
            try
            {
                var result = await _QcService.CreateSubParameterAsync(request);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while creating QC sub-parameter.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating QC sub-parameter.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetFormDataUsingDocEntry")]
        public async Task<IActionResult> GetFormDataUsingDocEntry(int docEntry, int company)
        {
            try
            {
                var result = await _QcService.GetFormDataUsingDocEntryAsync(docEntry, company, Url);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving form data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving form data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetFormStructure")]
        public async Task<IActionResult> GetFormStructure(int formId)
        {
            try
            {
                var result = await _QcService.GetFormStructureAsync(formId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving form structure.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving form structure.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetFormStructureV2")]
        public async Task<IActionResult> GetFormStructureV2()
        {
            try
            {
                var result = await _QcService.GetFormStructureAsyncV2();
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving form structure.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving form structure.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetFormUsingCreatedBy")]
        public async Task<IActionResult> GetFormUsingCreatedBy(string userId)
        {
            try
            {
                var result = await _QcService.GetFormUsingCreatedByAsync(userId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving forms.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving forms.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("InsertItemData")]
        public async Task<IActionResult> InsertItemData([FromBody] InsertItemDataRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is null.");
            }
            try
            {
                var result = await _QcService.InsertItemDataAsync(request);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while inserting item data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting item data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("InsertItemParameterData")]
        public async Task<IActionResult> InsertItemParameterData([FromBody] InsertItemParameterDataRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is null.");
            }
            try
            {
                var result = await _QcService.InsertItemParameterDataAsync(request);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while inserting item parameter data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting item parameter data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("InsertItemSubParameterData")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SaveSubParameterData()
        {
            try
            {
                var form = await Request.ReadFormAsync();

                // 1️⃣ Read and deserialize JSON data for subparameters
                var json = form["SubParamListJson"];
                if (string.IsNullOrWhiteSpace(json))
                {
                    return BadRequest(new { Success = false, Message = "Missing SubParamListJson data." });
                }

                var subParameters = JsonConvert.DeserializeObject<List<SubParameterRequest>>(json);
                if (subParameters == null || subParameters.Count == 0)
                {
                    return BadRequest(new { Success = false, Message = "Invalid subParameters format." });
                }

                // 2️⃣ Read uploaded files
                var files = form.Files;

                // 3️⃣ Call service (send list + single files collection)
                var result = await _QcService.SaveSubParameterDataAsync(subParameters, files);

                // 4️⃣ Return response
                if (result.Success)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }


        [HttpGet("GetProductionDocNum")]
        public async Task<IActionResult> GetProductionDocNum(int company, int DocId)
        {
            try
            {
                var result = await _QcService.GetProductionDocNumAsync(company, DocId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (HanaException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "HANA database error occurred while retrieving production data.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving production data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetProductionData")]
        public async Task<IActionResult> GetProductionData(int DocNum, int docId, int company)
        {
            try
            {
                var result = await _QcService.GetProductionDataAsync(DocNum, docId, company);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (HanaException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "HANA database error occurred while retrieving production data.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." + ex });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving production data.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }

        [HttpGet("GetDocumentInsights")]
        public async Task<IActionResult> GetDocumentInsights(int userId, int companyId, string month)
        {
            try
            {
                var result = await _QcService.GetDocumentInsightsAsync(userId, companyId, month);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "SQL database error occurred while retrieving document insights.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving document insights.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }

        [HttpGet("GetPendingDocuments")]
        public async Task<IActionResult> GetPendingDocuments(int userId, int companyId, string month)
        {
            try
            {
                var result = await _QcService.GetPendingDocumentsAsync(userId, companyId, month);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "SQL database error occurred while retrieving pending documents.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving pending documents.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }
        [HttpGet("GetApprovedDocuments")]
        public async Task<IActionResult> GetApprovedDocuments(int userId, int companyId, string month)
        {
            try
            {
                var result = await _QcService.GetApprovedDocumentsAsync(userId, companyId, month);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "SQL database error occurred while retrieving approved documents.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving approved documents.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }
        [HttpGet("GetRejectedDocuments")]
        public async Task<IActionResult> GetRejectedDocuments(int userId, int companyId, string month)
        {
            try
            {
                var result = await _QcService.GetRejectedDocumentsAsync(userId, companyId, month);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "SQL database error occurred while retrieving rejected documents.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving rejected documents.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }

        [HttpGet("GetTotalQcDocuments")]
        public async Task<IActionResult> GetTotalQcDocuments(int userId, int companyId, string month)
        {
            try
            {
                var result = await _QcService.GetAllQcDocumentAsync(userId, companyId, month);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "SQL database error occurred while retrieving rejected documents.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving rejected documents.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }

        [HttpGet("GetQCApprovalFlow")]
        public async Task<IActionResult> GetQCApprovalFlow(int flowId)
        {
            try
            {
                var result = await _QcService.GetQCApprovalFlowAsync(flowId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "SQL database error occurred while retrieving QC approval flow.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving QC approval flow.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }

        [HttpPost("ApproveDocument")]
        public async Task<IActionResult> ApproveDocument([FromBody] QcApprovalRequest request)
        {
            if (request == null)
                return BadRequest(new QcResponse { Success = false, Message = "Invalid request data" });

            try
            {
                var response = await _QcService.ApproveDocumentAsync(request);
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                return BadRequest(new QcResponse
                {
                    Success = false,
                    Message = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new QcResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("RejectDocument")]
        public async Task<IActionResult> RejectDocument([FromBody] QcRejectRequest request)
        {
            if (request == null)
                return BadRequest(new QcResponse { Success = false, Message = "Invalid request data" });

            try
            {
                var response = await _QcService.RejectDocumentAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new QcResponse { Success = false, Message = ex.Message });
            }
            catch (SqlException sqlEx)
            {
                return BadRequest(new QcResponse { Success = false, Message = sqlEx.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new QcResponse { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetFormsWithUsers")]
        public async Task<IActionResult> GetFormsWithUsers()
        {
            try
            {
                var result = await _QcService.GetFormsWithUsersAsync();
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving forms with users.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving forms with users.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("getItemDataId")]
        public async Task<IActionResult> GetItemDataIdAsync(int docEntry, int lineNum)
        {
            try
            {
                var result = await _QcService.GetItemDataIdAsync(docEntry, lineNum);
                if (result == null || !result.Any())
                {
                    return NotFound(new
                    {
                        Sucess = false,
                        Message = "no data Found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetQcUserIdsSendNotifications")]
        public async Task<IActionResult> GetQcUserIdsSendNotifications(int userDocumentId)
        {
            try
            {
                var result = await _QcService.GetQcUserIdsSendNotificatiosAsync(userDocumentId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving QC user IDs for notifications.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving QC user IDs for notifications.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("SendPendingQcCountNotification")]
        public async Task<IActionResult> SendPendingQcCountNotification()
        {
            try
            {
                var result = await _QcService.SendPendingQcCountNotificationAsync();
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving QC user IDs for notifications.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving QC user IDs for notifications.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetProductionDataUsingLine")]
        public async Task<IActionResult> GetProductionDataUsingLine(int DocEntry, int docId, int LineNum, int company)
        {
            try
            {
                var result = await _QcService.GetProductionDataUsingLineAsync(DocEntry, docId, LineNum, company);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (HanaException ex)  // ✅ Correct exception for HANA
            {
                _logger.LogError(ex, "HANA database error occurred while retrieving production data.");
                return StatusCode(500, new { Success = false, Message = "Database error occurred while processing your request." + ex });
            }
            catch (ArgumentException ex)  // ✅ Catch the company validation error
            {
                _logger.LogError(ex, "Invalid company ID provided.");
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving production data.");
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }

        [HttpGet("GetQcCurrentUsersSendNotification")]
        public async Task<IActionResult> GetQcCurrentUsersSendNotification(int userDocumentId)
        {
            try
            {
                var result = await _QcService.GetQcCurrentUsersSendNotificationAsync(userDocumentId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving QC  notifications.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving QC  notifications.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("UpdateForm")]
        public async Task<IActionResult> UpdateForm([FromBody] UpdateFormModel model)
        {
            if (model == null)
            {
                return BadRequest("Request body is null.");
            }
            try
            {
                var result = await _QcService.UpdateFormAsync(model);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error occurred while updating QC form.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating QC form.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}

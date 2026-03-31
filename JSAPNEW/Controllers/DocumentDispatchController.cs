using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ServiceStack.Text;
using System.Configuration;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentDispatchController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDocumentDispatchService _dispatchService;
        private readonly ILogger<DocumentDispatchController> _dispatchlogger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public DocumentDispatchController(IConfiguration configuration, IDocumentDispatchService DocumentDispatchService, ILogger<DocumentDispatchController> dispatchlogger, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _dispatchService = DocumentDispatchService;
            _dispatchlogger = dispatchlogger;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet("GetGRPO")]
        public async Task<IActionResult> GetGRPO()
        {
            try
            {
                var result = await _dispatchService.GetGRPOAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching GRPO data.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetLastBundleId")]
        public async Task<IActionResult> GetLastBundleId(int lastBundleId, string mode)
        {
            try
            {
                var result = await _dispatchService.GetLastBundleIdAsync(lastBundleId, mode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching last bundle ID.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("SaveAllAttachments")]
        public async Task<IActionResult> SaveAllAttachments([FromBody] List<SaveDocumentAttachmentModel> attachments)
        {
            if (attachments == null || attachments.Count == 0)
                return BadRequest("No attachments received.");

            var result = await _dispatchService.SaveDocumentAttachmentsAsync(attachments);

            if (result)
                return Ok(new { message = "Attachments saved successfully." });
            else
                return StatusCode(500, "Failed to save attachments.");
        }


        [HttpPost("UploadSingleFile")]
        public async Task<IActionResult> UploadSingleFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", "DispatchDocuments");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var filePath = Path.Combine(uploadFolder, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { filePath = $"/Uploads/DispatchDocuments/{file.FileName}" });
        }

        [HttpPost("SaveDocumentAttachmentsInHana")]
        public async Task<IActionResult> SaveDocumentAttachmentsInHana([FromBody] HanaDocumentDispatchModels request)
        {
            if (request == null)
                return BadRequest("Invalid request data.");
            try
            {
                var result = await _dispatchService.SaveDocumentAttachmentsInHanaAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while saving document attachments in Hana.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetPO")]
        public async Task<IActionResult> GetPO()
        {
            try
            {
                var result = await _dispatchService.GetPOAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching PO data.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetApDraft")]
        public async Task<IActionResult> GetApDraft()
        {
            try
            {
                var result = await _dispatchService.GetAPdraftAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching AP Draft data.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetGR")]
        public async Task<IActionResult> GetGR()
        {
            try
            {
                var result = await _dispatchService.GetGoodReturnAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching Good Returns data.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetDocumentByBundleId")]
        public async Task<IActionResult> GetDocumentByBundleId(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId))
                return BadRequest("Bundle ID cannot be null or empty.");
            try
            {
                var result = await _dispatchService.GetDocumentByBundleIdAsync(bundleId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching documents by bundle ID.");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost("UpdateDocumentStatus")]
        public async Task<IActionResult> UpdateDocumentStatus([FromBody] List<UpdateDocumentModel> request)
        {
            if (request == null)
                return BadRequest("Invalid request data.");
            try
            {
                var result = await _dispatchService.UpdateDocumentStatusAsync(request);

                _dispatchlogger.LogInformation("Update successfully.");
                return Ok(new { Success = true, Message = "Data Update successfully." });
                
            }           

            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while updating document status.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("SaveBundleStatus")]
        public async Task<IActionResult> SaveBundleStatus([FromBody] SaveBundleStatusModel request)
        {
            if (request == null || string.IsNullOrEmpty(request.bundleId))
                return BadRequest("Invalid request data.");
            try
            {
                var result = await _dispatchService.SaveBundleStatusModelAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while saving bundle status.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("SapDownloadFile")]
        public async Task<IActionResult> DownloadFile(string fileName, int company)
        {
            var client = new HttpClient();
            var remoteUrl = $"http://files.jivocanola.com/files/{fileName}?company={company}";
            var response = await client.GetAsync(remoteUrl);

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return File(stream, contentType, fileName);
        }

        [HttpGet("GetRejectedDocuments")]
        public async Task<IActionResult> GetRejectedDocuments(int createdBy)
        {
            if (createdBy == 0)
                return BadRequest("Bundle ID cannot be null or empty.");
            try
            {
                var result = await _dispatchService.GetRejectedDocumentsAsync(createdBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching documents by bundle ID.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetUserDocuments")]
        public async Task<IActionResult> GetUserDocuments(int userId)
        {
            if (userId == 0)
                return BadRequest("User ID cannot be null or empty.");
            try
            {
                var result = await _dispatchService.GetUserDocumentsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching user documents.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetRecieverPendingData")]
        public async Task<IActionResult> GetRecieverPendingData(int company, string status)
        {
            if (company == 0 || string.IsNullOrEmpty(status))
                return BadRequest("Company ID and status cannot be null or empty.");
            try
            {
                var result = await _dispatchService.GetRecieverPendingDataAsync(company, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching receiver pending data.");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("GetRecieverActionData")]
        public async Task<IActionResult> GetRecieverActionData(int company)
        {
            if (company == 0)
                return BadRequest("Company ID cannot be null or empty.");
            try
            {
                var result = await _dispatchService.GetRecieverActionDataAsync(company);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching receiver action data.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetRejectedDataByUserId")]
        public async Task<IActionResult> GetRejectedData(int userId)
        {
            if (userId == 0)
                return BadRequest("User ID cannot be null or empty.");
            try
            {
                var result = await _dispatchService.GetRejectedDataAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while fetching rejected data.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("UpdateNotRecievedStatus")]
        public async Task<IActionResult> UpdateNotRecievedStatus([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest("No IDs provided.");

            if (ids.Any(id => id <= 0))
                return BadRequest("Invalid ID(s) provided.");

            try
            {
                var results = new List<DispatchResponse>();

                foreach (var id in ids)
                {
                    var result = await _dispatchService.UpdateNotRecievedStatusAsync(id);
                    results.AddRange(result);
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _dispatchlogger.LogError(ex, "Error occurred while updating not received status.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

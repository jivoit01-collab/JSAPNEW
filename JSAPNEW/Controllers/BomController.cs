using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BomController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IBomService _bomService; //An interface for bom-related operations
        private readonly IBom2Service _bom2Service;

        private readonly ILogger<BomController> _bomlogger; //for recording events or errors
        public BomController(IConfiguration configuration, IBomService bomService, ILogger<BomController> logger, IBom2Service bom2Service)
        {
            _configuration = configuration;
            _bomService = bomService;
            _bomlogger = logger;
            _bom2Service = bom2Service;
        }

        [HttpGet("getwarehouse")]
        public async Task<ActionResult> GetWarehouse(int company)
        {
            try
            {
                var name = await _bomService.GetWarehouseAsync(company);

                if (!name.Any())
                {
                    _bomlogger.LogInformation("No name found");
                    return NotFound(new { Success = false, Message = "No name found" });
                }

                _bomlogger.LogInformation("name retrieved successfully.");
                return Ok(new { Success = true, Data = name });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving name.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getbomtype")]
        public async Task<ActionResult> GetBomType()
        {
            try
            {
                var type = await _bomService.GetBomTypeAsync();

                if (!type.Any())
                {
                    _bomlogger.LogInformation("No type found");
                    return NotFound(new { Success = false, Message = "No type found" });
                }

                _bomlogger.LogInformation("type retrieved successfully.");
                return Ok(new { Success = true, Data = type });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving type.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getchildtype")]
        public async Task<ActionResult> GetChildType()
        {
            try
            {
                var childtype = await _bomService.GetChildTypeAsync();

                if (!childtype.Any())
                {
                    _bomlogger.LogInformation("No childtype found");
                    return NotFound(new { Success = false, Message = "No childtype found" });
                }

                _bomlogger.LogInformation("childtype retrieved successfully.");
                return Ok(new { Success = true, Data = childtype });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving childtype.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getmaterial")]
        public async Task<ActionResult> GetMaterial(string parentCode, int company)
        {
            try
            {
                var material = await _bomService.GetMaterialAsync(parentCode, company);

                if (!material.Any())
                {
                    _bomlogger.LogInformation("No material found");
                    return NotFound(new { Success = false, Message = "No material found" });
                }

                _bomlogger.LogInformation("material retrieved successfully.");
                return Ok(new { Success = true, Data = material });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving material.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getfathermaterial")]
        public async Task<ActionResult> GetFatherMaterial(int company, string type)
        {
            try
            {
                var fathermaterial = await _bomService.GetFatherMaterialAsync(company, type);

                if (!fathermaterial.Any())
                {
                    _bomlogger.LogInformation("No fathermaterial found");
                    return NotFound(new { Success = false, Message = "No fathermaterial found" });
                }

                _bomlogger.LogInformation("fathermaterial retrieved successfully.");
                return Ok(new { Success = true, Data = fathermaterial });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving fathermaterial.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getresources")]
        public async Task<ActionResult> GetResources(int company)
        {
            try
            {
                var resources = await _bomService.GetResourcesAsync(company);

                if (!resources.Any())
                {
                    _bomlogger.LogInformation("No resources found");
                    return NotFound(new { Success = false, Message = "No resources found" });
                }

                _bomlogger.LogInformation("resources retrieved successfully.");
                return Ok(new { Success = true, Data = resources });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving resources.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("addbomcomponent")]
        public async Task<ActionResult> AddBomComponent(AddBomComponentModel request)
        {
            try
            {
                if (request == null)
                {
                    _bomlogger.LogWarning("Create Bom Component request is null.");
                    return BadRequest(new { Success = false, Message = "Invalid request data." });
                }

                var result = await _bomService.AddBomComponentAsync(request);

                if (result.Any())
                {
                    _bomlogger.LogWarning("Failed to create BOM Component.");
                    return BadRequest(new { Success = false, Message = "Failed to create BOM Component. Please try again." });
                }

                _bomlogger.LogInformation($"BOM Component created successfully with ID: {result}");
                //return Ok(new { Success = true, BOMID = result, Message = "BOM created successfully." });
                return Ok(new { Success = true, Message = "BOM Component created successfully." });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while creating BOM Component.");
                return StatusCode(500, new { Success = false, Message = "An internal error occurred. Please try again later." });
            }
        }

        [HttpPost("removebomcomponent")]
        public async Task<ActionResult> RemoveBomComponent(RemoveBomComponentModel request)
        {
            try
            {
                if (request == null)
                {
                    _bomlogger.LogWarning("Remove Bom request is null.");
                    return BadRequest(new { Success = false, Message = "Invalid request data." });
                }
                var result = await _bomService.RemoveBomComponentAsync(request);

                if (result.Any())
                {
                    _bomlogger.LogInformation("Failed to remove bom component");
                    return BadRequest(new { Success = false, Message = "Failed to remove bom component" });
                }

                _bomlogger.LogInformation("Bom component removed successfully.");
                return Ok(new { Success = true, Message = "Bom component removed successfully." });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while removing bom component.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("createbom")]
        public async Task<ActionResult> CreateBom([FromBody] CreateBomModel request)
        {
            try
            {
                if (request == null)
                {
                    _bomlogger.LogWarning("Create Bom request is null.");
                    return BadRequest(new { Success = false, Message = "Invalid request data." });
                }

                var result = await _bomService.CreateBomAsync(request);

                if (result.Any())
                {
                    _bomlogger.LogWarning("Failed to create BOM.");
                    return BadRequest(new { Success = false, Message = "Failed to create BOM. Please try again." });
                }

                _bomlogger.LogInformation($"BOM created successfully with ID: {result}");
                //return Ok(new { Success = true, BOMID = result, Message = "BOM created successfully." });
                return Ok(new { Success = true, Message = "BOM created successfully." });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while creating BOM.");
                return StatusCode(500, new { Success = false, Message = "An internal error occurred. Please try again later." });
            }
        }

        [HttpGet("getpendingboms")]
        public async Task<ActionResult> GetPendingBOMs(int userId, int companyId)
        {
            try
            {
                var pendingbom = await _bomService.PendingBOMsAsync(userId, companyId);

                if (!pendingbom.Any())
                {
                    _bomlogger.LogInformation("No pending bom found");
                    return NotFound(new { Success = false, Message = "No pending bom found" });
                }

                _bomlogger.LogInformation("pending bom retrieved successfully.");
                return Ok(new { Success = true, Data = pendingbom });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving pending bom.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getdetailofbommaterial")]
        public async Task<ActionResult> BomMaterial(int bomId, int company)
        {
            try
            {
                var bommaterial = await _bomService.BomMaterialAsync(bomId, company);

                if (!bommaterial.Any())
                {
                    _bomlogger.LogInformation("No bom material found");
                    return NotFound(new { Success = false, Message = "No bom material found" });
                }

                _bomlogger.LogInformation("bom material retrieved successfully.");
                return Ok(new { Success = true, Data = bommaterial });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving bom material.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getdetailofbomresource")]
        public async Task<ActionResult> BomResource(int bomId, int company)
        {
            try
            {
                var bomresource = await _bomService.BomResourceAsync(bomId, company);

                if (!bomresource.Any())
                {
                    _bomlogger.LogInformation("No bom resource found");
                    return NotFound(new { Success = false, Message = "No bom resource found" });
                }

                _bomlogger.LogInformation("bom resource retrieved successfully.");
                return Ok(new { Success = true, Data = bomresource });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving bom resource.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getapprovedboms")]
        public async Task<ActionResult> GetApprovedBOMs(int userId, int company)
        {
            try
            {
                var approvedbom = await _bomService.GetApprovedBOMsAsync(userId, company);

                if (!approvedbom.Any())
                {
                    _bomlogger.LogInformation("No approved bom found");
                    return NotFound(new { Success = false, Message = "No approved bom found" });
                }

                _bomlogger.LogInformation("approved bom retrieved successfully.");
                return Ok(new { Success = true, Data = approvedbom });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving approved bom.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getrejectedboms")]
        public async Task<ActionResult> GetRejectedBOMs(int userId, int company)
        {
            try
            {
                var rejectedbom = await _bomService.GetRejectedBOMsAsync(userId, company);

                if (!rejectedbom.Any())
                {
                    _bomlogger.LogInformation("No rejected bom found");
                    return NotFound(new { Success = false, Message = "No rejected bom found" });
                }

                _bomlogger.LogInformation("rejected bom retrieved successfully.");
                return Ok(new { Success = true, Data = rejectedbom });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving rejected bom.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("approvebom")]
        public async Task<ActionResult> BomApprove(int bomId, int userId, string description = null)
        {
            try
            {
                var result = await _bomService.BomApproveAsync(bomId, userId, description);

                // Check the result for success
                if (result == null)
                {
                    _bomlogger.LogWarning("No response received from the database for userId: {UserId} and bomId: {bomId}.", userId, bomId);
                    return BadRequest(new { Success = false, Message = "Unable to approve the bom." });
                }

                _bomlogger.LogInformation("Bom approved successfully for userId: {UserId} and bomId: {bomId}.", userId, bomId);
                return Ok(new { Success = true, Data = result, Message = "Bom approved successfully." });
            }
            catch (SqlException ex) when (ex.Number == 50001) // Custom SQL error for unauthorized user
            {
                _bomlogger.LogWarning("SQL error (50001): {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "User is not authorized to approve this bom at the current stage." });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "An error occurred while approving the bom for userId: {UserId} and bomId: {bomId}.", userId, bomId);
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("rejectbom")]
        public async Task<ActionResult> BomReject(int bomId, int userId, string description)
        {
            try
            {
                var result = await _bomService.BomRejectAsync(bomId, userId, description);

                // Check the result for success
                if (result == null)
                {
                    _bomlogger.LogWarning("No response received from the database for userId: {UserId} and bomId: {bomId}.", userId, bomId);
                    return BadRequest(new { Success = false, Message = "Unable to reject the bom." });
                }

                _bomlogger.LogInformation("Bom rejected successfully for userId: {UserId} and bomId: {bomId}.", userId, bomId);
                return Ok(new { Success = true, Data = result, Message = "Bom rejected successfully." });
            }
            catch (SqlException ex) when (ex.Number == 50001) // Custom SQL error for unauthorized user
            {
                _bomlogger.LogWarning("SQL error (50001): {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "User is not authorized to reject this bom at the current stage." });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "An error occurred while approving the bom for userId: {UserId} and bomId: {bomId}.", userId, bomId);
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("CreateBomWithComponents")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateBomWithComponents([FromForm] string bomRequestJson, [FromForm] List<IFormFile>? files)
        {
            try
            {
                Console.WriteLine("🔹 Step 1: API called successfully");

                if (string.IsNullOrEmpty(bomRequestJson))
                {
                    Console.WriteLine("❌ Error: BOM request data is missing.");
                    return BadRequest(new { Message = "BOM request data is missing.", Success = false });
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<BomRequest>(bomRequestJson, options);

                if (request == null)
                {
                    Console.WriteLine("❌ Error: Invalid BOM request format.");
                    return BadRequest(new { Message = "Invalid BOM request format.", Success = false });
                }

                Console.WriteLine("🔹 Step 2: JSON request deserialized successfully.");
                Console.WriteLine($"📌 ParentCode: {request.ParentCode}");
                Console.WriteLine($"📌 Type: {request.Type}");
                Console.WriteLine($"📌 Qty: {request.Qty}");
                Console.WriteLine($"📌 Company: {request.Company}");
                Console.WriteLine($"📌 CreatedBy: {request.CreatedBy}");
                Console.WriteLine($"📌 WareHouseCode: {request.WareHouseCode}");

                if (files == null || files.Count == 0)
                {
                    Console.WriteLine("ℹ️ Info: No files uploaded.");
                }
                else
                {
                    Console.WriteLine($"🔹 Step 3: {files.Count} files received.");
                }

                var result = await _bomService.CreateBomWithComponents(request, files ?? new List<IFormFile>());

                Console.WriteLine("✅ Step 4: BOM creation completed.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Step 5: Error occurred - {ex.Message}");
                return StatusCode(500, new { Message = $"Internal server error: {ex.Message}", Success = false });
            }
        }


        [HttpGet("gettotalbominsights")]
        public async Task<ActionResult> TotalBomInsights(int userId, int companyId)
        {
            try
            {
                var bominsights = await _bomService.TotalBomInsightsAsync(userId, companyId);

                if (!bominsights.Any())
                {
                    _bomlogger.LogInformation("No bom found");
                    return NotFound(new { Success = false, Message = "No bom found" });
                }

                _bomlogger.LogInformation("bom retrieved successfully.");
                return Ok(new { Success = true, Data = bominsights });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving bom.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("fullheaderdetail")]
        public async Task<ActionResult> FullHeaderDetailModel(int bomId, int company)
        {
            try
            {
                var headerDetails = await _bomService.FullHeaderDetailModelAsync(bomId, company);

                if (!headerDetails.Any())
                {
                    _bomlogger.LogInformation("No Header Details found");
                    return NotFound(new { Success = false, Message = "No Header Details found" });
                }

                _bomlogger.LogInformation("Header Details retrieved successfully.");
                return Ok(new { Success = true, Data = headerDetails });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving Header Details.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("updatebomheader")]
        public async Task<IActionResult> UpdateBomHeader(int bomId, int qty, string wareHouse, int updatedBy)
        {
            try
            {
                var updateResult = await _bomService.UpdateBomHeaderAsync(bomId, qty, wareHouse, updatedBy);

                if (updateResult == 0)
                {
                    _bomlogger.LogInformation("Header updated successfully.");
                    return Ok(new
                    {
                        Success = true,
                        Message = "Header updated successfully.",
                        Data = updateResult
                    });
                }
                else
                {
                    _bomlogger.LogInformation("No changes were made.");
                    return Ok(new
                    {
                        Success = false,
                        Message = "No changes were made to the header.",
                        Data = updateResult
                    });
                }
            }
            catch (Exception ex) // Catch all errors, including SQL errors
            {
                if (ex.Message == "Bom not exists")
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Bom does not exist. Please check the provided BomId."
                    });
                }

                _bomlogger.LogError(ex, "Error occurred while updating Header.");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while processing your request."
                });
            }
        }

        [HttpPost("updatechild")]
        public async Task<IActionResult> UpdateChild(int bomComId, int qty, string wareHouse, int updatedBy)
        {
            try
            {
                var updateResult = await _bomService.UpdateChildAsync(bomComId, qty, wareHouse, updatedBy);

                if (updateResult == 0)
                {
                    _bomlogger.LogInformation("Bom Child updated successfully.");
                    return Ok(new
                    {
                        Success = true,
                        Message = "Bom Child updated successfully.",
                        Data = updateResult
                    });
                }
                else
                {
                    _bomlogger.LogInformation("No changes were made.");
                    return Ok(new
                    {
                        Success = false,
                        Message = "No changes were made to the bom child.",
                        Data = updateResult
                    });
                }
            }
            catch (Exception ex) // Catch all errors, including SQL errors
            {
                if (ex.Message == "Bom Child not exists")
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Bom Child does not exist. Please check the provided BomComId."
                    });
                }

                _bomlogger.LogError(ex, "Error occurred while updating bom child.");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while processing your request."
                });
            }
        }

        [HttpGet("getallbomwithdetails")]
        public async Task<ActionResult> GetAllBomWithDetails(int userId, int company)
        {
            var boms = await _bomService.GetAllBomWithDetailsAsync(userId, company);
            return Ok(boms);
        }

        [HttpGet("getBomFiles")]
        public async Task<ActionResult> GetBomFilesData(int bomId)
        {
            try
            {
                // ✅ Pass Url to the service
                var result = await _bomService.GetBomFilesDataAsync(bomId, Url);

                if (!result.Any())
                {
                    _bomlogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                _bomlogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bomlogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetBomApprovalFlow")]
        public async Task<ActionResult> GetBomApprovalFlow(int bomId)
        {
            try
            {
                var result = await _bomService.GetBomApprovalFlowAsync(bomId);
                if (result == null || !result.Any())
                {
                    _bomlogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No approval flow found for this BOM." });
                }
                _bomlogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bomlogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetPendingInsertions")]
        public async Task<IActionResult> GetPendingInsertions(int bomid, string action)
        {
            try
            {
                var pendingInsertions = await _bomService.GetPendingInsertionsAsync(bomid, action);
                if (pendingInsertions == null || !pendingInsertions.Any())
                {
                    _bomlogger.LogInformation("No pending insertions found");
                    return NotFound(new { Success = false, Message = "No pending insertions found" });
                }
                _bomlogger.LogInformation("Pending insertions retrieved successfully.");
                return Ok(new { Success = true, Data = pendingInsertions });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving pending insertions.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("ProductTrees")]
        // action Creation or update
        public async Task<IActionResult> PostBomsToSAP(int bomid, string action)
        {
            try
            {
                var pendingInsertions = await _bomService.GetPendingInsertionsAsync(bomid, action);
                if (pendingInsertions == null || !pendingInsertions.Any())
                    return BadRequest(new { Success = false, Message = "No pending BOMs to post" });

                var syncResults = await _bom2Service.PostProductTreesToSAPAsync(pendingInsertions);
                return Ok(new { Success = true, Data = syncResults });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error posting BOMs to SAP");
                return StatusCode(500, new { Success = false, Message = $"Error posting BOMs to SAP: {ex.Message}" });
            }
        }

        [HttpGet("GetCreatedByBomApprovalFlow")]
        public async Task<ActionResult> GetCreatedByBomApprovalFlow(int createdBy, int companyId)
        {
            try
            {
                var result = await _bomService.GetCreatedByBomApprovalFlowAsync(createdBy, companyId);
                if (result == null || !result.Any())
                {
                    _bomlogger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No approval flow found for this BOM." });
                }
                _bomlogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bomlogger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetBomByUserId")]
        public async Task<ActionResult> GetBomByUserId(int userId, int company,string month)
        {
            try
            {
                var result = await _bomService.GetBomByUserIdAsync(userId, company,month);
                if (result == null || !result.Any())
                {
                    _bomlogger.LogInformation("No BOMs found for the user");
                    return NotFound(new { Success = false, Message = "No BOMs found for the user" });
                }
                _bomlogger.LogInformation("BOMs retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving BOMs by user ID.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("FetchBomDetails")]
        public async Task<ActionResult> FetchBomDetails(string code, int company)
        {
            try
            {
                var result = await _bomService.FetchBomDetailsAsync(code, company);
                if (result == null || !result.Any())
                {
                    _bomlogger.LogInformation("No BOM details found for the code");
                    return NotFound(new { Success = false, Message = "No BOM details found for the code" });
                }
                _bomlogger.LogInformation("BOM details retrieved successfully.");
                return Ok(new { Success = true, Data = result });

            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving BOM details.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /*[HttpPost("UpdateBomWithFiles")]
        public async Task<IActionResult> UpdateBomWithFiles([FromForm] string request, [FromForm] List<IFormFile> files)
        {
            if (string.IsNullOrWhiteSpace(request))
            {
                return BadRequest(new BomResponse
                {
                    Success = false,
                    Message = "Request data is missing."
                });
            }

            UpdateBomRequestModel model;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                model = JsonSerializer.Deserialize<UpdateBomRequestModel>(request, options);
            }
            catch (Exception ex)
            {
                return BadRequest(new BomResponse
                {
                    Success = false,
                    Message = $"Invalid JSON format in 'request': {ex.Message}"
                });
            }

            if (model == null)
            {
                return BadRequest(new BomResponse
                {
                    Success = false,
                    Message = "Parsed request object is null."
                });
            }

            var result = await _bomService.UpdateBomAsync(model, files);
            return Ok(result);
        }*/

        [HttpPost("UpdateBom")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBom()
        {
            try
            {
                var form = await Request.ReadFormAsync();

                // Get the JSON payload
                var requestsString = form["requests"].ToString();
                if (string.IsNullOrEmpty(requestsString))
                {
                    return BadRequest(new BomResponse
                    {
                        Success = false,
                        Message = "Missing 'requests' parameter"
                    });
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<UpdateBomRequest>(requestsString, options);
                if (request == null)
                {
                    return BadRequest(new BomResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                // File processing
                var uploadedFiles = new List<UpdateBomFile>();
                var files = form.Files;

                if (files != null && files.Any())
                {
                    var uploadPath = Path.Combine("wwwroot", "Uploads", "BOM");
                    Directory.CreateDirectory(uploadPath);

                    foreach (var file in files)
                    {
                        if (file.Length <= 0) continue;

                        var ext = Path.GetExtension(file.FileName);
                        var newFileName = $"{Guid.NewGuid()}{ext}";
                        var savePath = Path.Combine(uploadPath, newFileName);

                        using var stream = new FileStream(savePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        uploadedFiles.Add(new UpdateBomFile
                        {
                            FileName = newFileName,
                            FileExt = ext,
                            FileSize = file.Length,
                            Path = "/Uploads/BOM",
                            Description = "Uploaded via API"
                            // You can also set UploadedBy = request.CreatedBy here if needed
                        });
                    }
                }

                // Assign to request
                request.Files = uploadedFiles;

                // Call service
                var response = await _bomService.UpdateBomAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new BomResponse
                {
                    Success = false,
                    Message = "Error: " + ex.Message
                });
            }
        }

        [HttpGet("GetDistinctBOM")]
        public async Task<ActionResult> GetDistinctBOM(int company)
        {
            try
            {
                var result = await _bomService.GetDistinctBomAsync(company);
                if (result == null || !result.Any())
                {
                    _bomlogger.LogInformation("No BOM details ");
                    return NotFound(new { Success = false, Message = "No BOM details found" });
                }
                _bomlogger.LogInformation("BOM details retrieved successfully.");
                return Ok(new { Success = true, Data = result });

            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving BOM details.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }
        [HttpGet("GetOldBomPreview")]
        public async Task<IActionResult> GetOldBomPreview([FromQuery] int newBomId)
        {
            if (newBomId <= 0)
                return BadRequest(new { Success = false, Message = "Invalid BOM ID." });

            try
            {
                var result = await _bomService.GetOldBomPreviewAsync(newBomId);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        [HttpPost("UpdateProductTrees")]
        // action Creation or update
        public async Task<IActionResult> UpdateBomsToSAP(int bomid, string action)
        {
            try
            {
                var pendingInsertions = await _bomService.GetPendingInsertionsAsync(bomid, action);
                if (pendingInsertions == null || !pendingInsertions.Any())
                    return BadRequest(new { Success = false, Message = "No pending BOMs to post" });

                var syncResults = await _bomService.PatchProductTreesToSAPAsync(pendingInsertions);
                return Ok(new { Success = true, Data = syncResults });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error posting BOMs to SAP");
                return StatusCode(500, new { Success = false, Message = $"Error posting BOMs to SAP: {ex.Message}" });
            }
        }

        [HttpGet("SendPendingBomCountNotification")]
        public async Task<ActionResult<CreditLimitApiResponse>> SendPendingCLCountNotification()
        {
            try
            {
                var result = await _bomService.SendPendingBomCountNotificationAsync();
                if (result == null)
                {
                    _bomlogger.LogInformation("Failed to send pending Bom count notification");
                    return NotFound(new CreditLimitApiResponse { Success = false, Message = "Failed to send pending Bom count notification" });
                }
                _bomlogger.LogInformation("Pending Bom count notification sent successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while sending pending Bom count notification.");
                return StatusCode(500, new CreditLimitApiResponse { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBomCurrentUsersSendNotification")]
        public async Task<ActionResult> GetCurrentUsersSendNotification(int bomId)
        {
            try
            {
                var userIds = await _bomService.GetBomCurrentUsersSendNotificationAsync(bomId);
                if (userIds == null || !userIds.Any())
                {
                    _bomlogger.LogInformation("No user IDs found for notifications");
                    return NotFound(new { Success = false, Message = "No user IDs found for notifications" });
                }
                _bomlogger.LogInformation("User IDs for notifications retrieved successfully.");
                return Ok(new { Success = true, Data = userIds });
            }
            catch (Exception ex)
            {
                _bomlogger.LogError(ex, "Error occurred while retrieving user IDs for notifications.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}



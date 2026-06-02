using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Bom2Controller : ControllerBase
    {


        private readonly IConfiguration _configuration;
        private readonly IBom2Service _bom2Service; //An interface for bom-related operations

        private readonly ILogger<Bom2Controller> _bom2logger; //for recording events or errors
        public Bom2Controller(IConfiguration configuration, IBom2Service bom2Service, ILogger<Bom2Controller> bom2logger)
        {
            _configuration = configuration;
            _bom2Service = bom2Service;
            _bom2logger = bom2logger;
        }


        [HttpGet("SapOilSession")]
        public async Task<ActionResult> GetSAPSessionOil()
        {
            try
            {
                var sapSession = await _bom2Service.GetSAPSessionOilAsync();
                if (sapSession == null)
                {
                    _bom2logger.LogInformation("No SAP session found");
                    return NotFound(new { Success = false, Message = "No SAP session found" });
                }
                _bom2logger.LogInformation("SAP session retrieved successfully.");
                return Ok(new { Success = true, Data = sapSession });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while retrieving SAP session.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("SapBevSession")]
        public async Task<ActionResult> GetSAPSessionBev()
        {
            try
            {
                var sapSession = await _bom2Service.GetSAPSessionBevAsync();
                if (sapSession == null)
                {
                    _bom2logger.LogInformation("No SAP session found");
                    return NotFound(new { Success = false, Message = "No SAP session found" });
                }
                _bom2logger.LogInformation("SAP session retrieved successfully.");
                return Ok(new { Success = true, Data = sapSession });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while retrieving SAP session.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("SapMartSession")]
        public async Task<ActionResult> GetSAPSessionMart()
        {
            try
            {
                var sapSession = await _bom2Service.GetSAPSessionMartAsync();
                if (sapSession == null)
                {
                    _bom2logger.LogInformation("No SAP session found");
                    return NotFound(new { Success = false, Message = "No SAP session found" });
                }
                _bom2logger.LogInformation("SAP session retrieved successfully.");
                return Ok(new { Success = true, Data = sapSession });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while retrieving SAP session.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("GetPendingInsertions")]
        public async Task<IActionResult> GetPendingInsertions(int bomid)
        {
            try
            {
                var pendingInsertions = await _bom2Service.GetPendingInsertionsAsync2(bomid);
                if (pendingInsertions == null || !pendingInsertions.Any())
                {
                    _bom2logger.LogInformation("No pending insertions found");
                    return NotFound(new { Success = false, Message = "No pending insertions found" });
                }
                _bom2logger.LogInformation("Pending insertions retrieved successfully.");
                return Ok(new { Success = true, Data = pendingInsertions });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while retrieving pending insertions.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("ProductTrees")]
        public async Task<IActionResult> PostBomsToSAP(int bomid)
        {
            try
            {
                var pendingInsertions = await _bom2Service.GetPendingInsertionsAsync2(bomid);
                if (pendingInsertions == null || !pendingInsertions.Any())
                    return BadRequest(new { Success = false, Message = "No pending BOMs to post" });

                var syncResults = await _bom2Service.PostProductTreesToSAPAsync(pendingInsertions);
                return Ok(new { Success = true, Data = syncResults });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error posting BOMs to SAP");
                return StatusCode(500, new { Success = false, Message = $"Error posting BOMs to SAP: {ex.Message}" });
            }
        }


        [HttpPost("BomApprove2")]
        public async Task<ActionResult> BomApprove2([FromBody] ApproveModel2 request)
        {
            try
            {
                var result = await _bom2Service.BomApproveAsync(request);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Message = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetApprovedBOMs2")]
        public async Task<ActionResult> GetApprovedBOMs2(int userId, int company)
        {
            try
            {
                var result = await _bom2Service.GetApprovedBOMsAsync(userId, company);
                if (!result.Any())
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }

                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "Data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("TotalBomInsights2")]
        public async Task<ActionResult> GetTotalBomInsights2(int userId, int companyId)
        {
            try
            {
                var BomInsights = await _bom2Service.TotalBomInsightsAsync(userId, companyId);


                _bom2logger.LogInformation("Data retrieve successfully.");
                return Ok(new { Success = true, Data = BomInsights });

            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetPendingBOMs2")]
        public async Task<IActionResult> GetPendingBOMs2(int userId, int company)
        {
            try
            {
                _bom2logger.LogInformation("Fetching pending BOMs for UserId: {UserId}, Company: {Company}", userId, company);

                var result = await _bom2Service.GetPendingBOMsAsync(userId, company);

                // Filter out dummy fallback row
                var realResults = result.Where(x => x.bomId != 0).ToList();

                if (!realResults.Any())
                {
                    _bom2logger.LogInformation("No pending BOMs found for user {UserId} in company {Company}.", userId, company);
                    return NotFound(new { Success = false, Message = "No pending BOMs found for this user." });
                }

                _bom2logger.LogInformation("Found {Count} pending BOM(s).", realResults.Count);
                return Ok(new { Success = true, Data = realResults });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "An error occurred while fetching pending BOMs.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetRejectedBOMs2")]
        public async Task<ActionResult> GetRejectedBOMs2(int userId, int company)
        {
            try
            {
                var result = await _bom2Service.GetRejectedBOMsAsync(userId, company);
                if (!result.Any())
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No rejected BOMs found for this user." });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("BomReject2")]
        public async Task<ActionResult> BomReject2([FromBody] RejectModel2 request)
        {
            try
            {
                var result = await _bom2Service.BomRejectAsync(request);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Message = "result" });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetBomFiles2")]
        public async Task<ActionResult> GetBomFiles2(int bomId)
        {
            try
            {
                var result = await _bom2Service.GetBomFilesAsync(bomId);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetBomHeadersByIds2")]
        public async Task<ActionResult> GetBomHeadersByIds2(int company)
        {
            try
            {
                var result = await _bom2Service.GetBomHeadersByIdsAsync(company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetBomType2")]
        public async Task<ActionResult> GetBomType2()
        {
            try
            {
                var result = await _bom2Service.GetBomTypeAsync();
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetChildType2")]
        public async Task<ActionResult> GetChildType2()
        {
            try
            {
                var result = await _bom2Service.GetChildTypeAsync();
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetChildTypeById2")]
        public async Task<ActionResult> GetChildTypeById2(int childId)
        {
            try
            {
                var result = await _bom2Service.GetChildTypeById(childId);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("BomMaterial2")]
        public async Task<ActionResult> BomMaterial2(int bomId, int company)
        {
            try
            {
                var result = await _bom2Service.BomMaterialAsync(bomId, company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("BomResource2")]
        public async Task<ActionResult> BomResource2(int bomId, int company)
        {
            try
            {
                var result = await _bom2Service.BomResourceAsync(bomId, company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetFatherMaterial2")]
        public async Task<ActionResult> GetFatherMaterial2(int company, string type)
        {
            try
            {
                var result = await _bom2Service.GetFatherMaterialAsync(company, type);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("FullHeaderDetail2")]
        public async Task<ActionResult> FullHeaderDetail2(int bomId, int company)
        {
            try
            {
                var result = await _bom2Service.FullHeaderDetailAsync(bomId, company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetMaterial2")]
        public async Task<ActionResult> GetMaterial2(string parentCode, int company)
        {
            try
            {
                var result = await _bom2Service.GetMaterialAsync(parentCode, company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetResources2")]
        public async Task<ActionResult> GetResources2(int company)
        {
            try
            {
                var result = await _bom2Service.GetResourcesAsync(company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetWarehouse2")]
        public async Task<ActionResult> GetWarehouse2(int company)
        {
            try
            {
                var result = await _bom2Service.GetWarehouseAsync(company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("RemoveBomComponent2")]
        public async Task<ActionResult> RemoveBomComponent2([FromBody] RemoveBomComponentModel2 request)
        {
            try
            {
                var result = await _bom2Service.RemoveBomComponentAsync(request);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Message = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("UpdateBomHeader2")]
        public async Task<ActionResult> UpdateBomHeader2(int bomId, int qty, string wareHouse, int updatedBy)
        {
            try
            {
                var result = await _bom2Service.UpdateBomHeaderAsync(bomId, qty, wareHouse, updatedBy);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Message = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("CreateBomWithComponents2")]
        public async Task<ActionResult> CreateBomWithComponents2([FromForm] string requestData, List<IFormFile> files)
        {
            try
            {
                if (string.IsNullOrEmpty(requestData))
                {
                    _bom2logger.LogWarning("Create Bom request is null.");
                    return BadRequest(new { success = false, message = "Invalid request data." });
                }

                var request = JsonSerializer.Deserialize<BomRequest2>(requestData);
                var result = await _bom2Service.CreateBomWithComponentsAsync(request, files);

                if (result == null || !result.Any())
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { success = false, message = "No data found" });
                }

                // If any item has success == false, return BadRequest
                if (result.Any(r => r.Success == false))
                {
                    _bom2logger.LogWarning("BOM creation failed: {@Result}", result);
                    return BadRequest(new
                    {
                        success = false,
                        message = result.First().Message,
                        // data = result
                    });
                }

                _bom2logger.LogInformation("BOM created successfully.");
                return Ok(new
                {
                    success = true,
                    message = result
                });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while running this API.");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("BOMGetBomHeaderByIds2")]
        public async Task<ActionResult> BOMGetBomHeadersByIds2(string IdsList, int company)
        {
            try
            {
                var result = await _bom2Service.BOMGetBomHeadersByIdsAsync(IdsList, company);
                if (result == null)
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No data found" });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetTotalBomInDetails2")]
        public async Task<ActionResult> GetTotalBomInDetails2(int userId, int company)
        {
            try
            {
                var result = await _bom2Service.GetTotalBomInDetailsAsync(userId, company);
                if (!result.Any())
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No BOMs found." });
                }

                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetBomApprovalFlow2")]
        public async Task<ActionResult> GetBomApprovalFlow2(int bomId)
        {
            try
            {
                var result = await _bom2Service.GetBomApprovalFlowAsync(bomId);
                if (result == null || !result.Any())
                {
                    _bom2logger.LogInformation("No data found");
                    return NotFound(new { Success = false, Message = "No approval flow found for this BOM." });
                }
                _bom2logger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _bom2logger.LogWarning("Custom SQL error: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "data is not found!" });
            }
            catch (Exception ex)
            {
                _bom2logger.LogError(ex, "Error occurred while run this api.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }
    }
}

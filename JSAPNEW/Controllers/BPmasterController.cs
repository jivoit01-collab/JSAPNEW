using JSAPNEW.Services.Interfaces;
using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Data.Entities;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel.Design;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BPmasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IBPmasterService _BPService;
        private readonly IItemMasterService _itemMasterService;
        private readonly ILogger<BPmasterController> _BPlogger;

        public BPmasterController(IConfiguration configuration, IBPmasterService bpService, ILogger<BPmasterController> logger, IItemMasterService itemMasterService)
        {
            _configuration = configuration;
            _BPService = bpService;
            _BPlogger = logger;
            _itemMasterService = itemMasterService;
        }

        [HttpPost("InsertBPmasterData")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BPMasterResponse>> InsertBPMaster()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var requestJson = form["requests"];
                if (string.IsNullOrWhiteSpace(requestJson))
                {
                    return BadRequest(new BPMasterResponse { Success = false, Message = "Missing request data", GeneratedCode = 0 });
                }

                var model = JsonConvert.DeserializeObject<InsertBPMasterDataModel>(requestJson);

                // Attachments from files
                var files = form.Files;
                model.Attachments = new List<BPAttachment>();

                // 2. Parse fileTypes
                var fileTypeList = form["fileTypes"].ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

                if (files.Count != fileTypeList.Count)
                {
                    return BadRequest(new BPMasterResponse
                    {
                        Success = false,
                        Message = "The number of fileTypes does not match the number of attachments.",
                        GeneratedCode = 0
                    });
                }
                var uploadPath = Path.Combine("wwwroot", "Uploads", "BPmaster");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileType = fileTypeList[i];

                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName);
                        var newFileName = $"{Guid.NewGuid()}{ext}";
                        var savePath = Path.Combine(uploadPath, newFileName);

                        using var stream = new FileStream(savePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        model.Attachments.Add(new BPAttachment
                        {
                            FileName = newFileName,
                            FilePath = "/Uploads/BPmaster", // Use relative path for UI
                            FileSize = file.Length,
                            ContentType = file.ContentType,
                            fileType = fileType
                        });
                    }
                }

                // Call service
                var result = await _BPService.InsertBPMasterAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error saving BP Master data.");
                return BadRequest(new BPMasterResponse
                {
                    Success = false,
                    Message = ex.Message,
                    GeneratedCode = 0
                });
            }
        }

        [HttpGet("GetDistinctBankName")]
        public async Task<IActionResult> GetDistinctBankName(int company)
        {
            try
            {
                var result = await _BPService.GetDistinctBankNameAsync(company);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching distinct bank names.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpGet("GetSLPname")]
        public async Task<IActionResult> GetSLPname(int company)
        {
            try
            {
                var result = await _BPService.GetSLPnameAsync(company);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching SLP names.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
        [HttpGet("GetChain")]
        public async Task<IActionResult> GetChain(int company, string BPType, string IsStaff)
        {
            try
            {
                var result = await _BPService.GetChainAsync(company, BPType, IsStaff);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching chains.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpGet("GetCountry")]
        public async Task<IActionResult> GetCountry(int company)
        {
            try
            {
                var result = await _BPService.GetCountryAsync(company);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching countries.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
        [HttpGet("GetMaingroup")]
        public async Task<IActionResult> GetMaingroup(int company, string BPType, string IsStaff)
        {
            try
            {
                var result = await _BPService.GetMaingroupAsync(company, BPType, IsStaff);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching main groups.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
        [HttpGet("GetMSMEtype")]
        public async Task<IActionResult> GetMSMEtype(int company)
        {
            try
            {
                var result = await _BPService.GetMSMEtypeAsync(company);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching MSME types.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
        [HttpGet("GetGroupNameByBPType")]
        public async Task<IActionResult> GetGroupNameByBPType(int company, string bpType, string isStaff)
        {
            try
            {
                var result = await _BPService.GetGroupNameByBPTypeAsync(company, bpType, isStaff);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error in GetGroupNameByBPType");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
        [HttpGet("GetDistinctPaymentGroups")]
        public async Task<IActionResult> GetDistinctPaymentGroups(int company)
        {
            try
            {
                var result = await _BPService.GetDistinctPaymentGroupsAsync(company);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching payment groups.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
        [HttpGet("GetDistinctStates")]
        public async Task<IActionResult> GetDistinctStates(int company, string CountryCode)
        {
            try
            {
                var result = await _BPService.GetDistinctStatesAsync(company, CountryCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching distinct states.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpGet("GetApprovedBP")]
        public async Task<IActionResult> GetApprovedBPs(int userId, int companyId, string month = null)
        {
            try
            {
                var result = await _BPService.GetApprovedBPsAsync(userId, companyId, month);

                if (result == null || !result.Any())
                {
                    return NotFound(new { Success = false, Message = "No approved BPs found." });
                }

                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error while fetching approved BPs");
                return StatusCode(500, new { Success = false, Message = "An internal error occurred." });
            }
        }

        [HttpGet("GetPendingBP")]
        public async Task<IActionResult> GetPendingBp(int userId, int companyId, string month = null)
        {
            try
            {
                var result = await _BPService.GetPendingBpAsync(userId, companyId, month);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching pending BPs.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetRejectedBP")]
        public async Task<IActionResult> GetRejectedBp(int userId, int companyId, string month = null)
        {
            try
            {
                var result = await _BPService.GetRejectedBpAsync(userId, companyId, month);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching rejected BPs.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetSingleBPData")]
        public async Task<IActionResult> GetSingleBPData(int bpCode)
        {
            try
            {
                var result = await _BPService.GetSingleBPDataAsync(bpCode, Url);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { message = "SQL error", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", error = ex.Message });
            }
        }


        [HttpPost("ApproveBP")]
        public async Task<IActionResult> ApproveBP([FromBody] ApproveOrRejectBpRequest request)
        {
            try
            {
                var result = await _BPService.ApproveBPAsync(request);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error during BP approval.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpPost("RejectBP")]
        public async Task<IActionResult> RejectBP([FromBody] ApproveOrRejectBpRequest request)
        {
            try
            {
                var result = await _BPService.RejectBPAsync(request);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error during BP rejection.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("sp_GetSingleBPData")]
        public async Task<IActionResult> GetSingleBP(int bpCode)
        {
            try
            {
                var result = await _BPService.GetSingleBPDataAsync(bpCode,Url);
                if (result == null)
                    return NotFound(new { Success = false, Message = "BP not found" });

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching single BP data.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }


        [HttpGet("BPGetCardInfo")]
        public async Task<IActionResult> BPGetCardInfo(int company, string BPType, string IsStaff)
        {
            try
            {
                var result = await _BPService.BPGetCardInfoAsync(company, BPType, IsStaff);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching BP card info.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetUniquePANs")]
        public async Task<IActionResult> GetUniquePANs(int company)
        {
            try
            {
                var result = await _BPService.GetUniquePANsAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, message = "Error retrieving unique PANs", error = ex.Message });
            }
        }

        [HttpGet("GetGSTMismatchByState")]
        public async Task<IActionResult> GetGSTMismatchByState(int company, string stateCode)
        {
            try
            {
                var result = await _BPService.GetGSTMismatchByStateAsync(company, stateCode);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching BP card info.");
                return StatusCode(500, new { Success = false, message = "Error fetching GST mismatch data", error = ex.Message });
            }
        }

        [HttpGet("GetBPCounts")]
        public async Task<IActionResult> GetBPCounts(string month, int userId)
        {
            try
            {
                var result = await _BPService.GetBPCountsAsync(month, userId);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { message = "SQL error occurred", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred", error = ex.Message });
            }
        }

        [HttpGet("GetPricelist")]
        public async Task<IActionResult> GetPricelist(int company)
        {
            try
            {
                var result = await _BPService.GetPricelistAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching price list.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("CheckAddressUid")]
        public async Task<IActionResult> CheckAddressUid(string addressUid)
        {
            if (string.IsNullOrWhiteSpace(addressUid))
                return BadRequest(new { Success = false, Message = "AddressUid is required" });

            try
            {
                var result = await _BPService.CheckAddressUidAsync(addressUid);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error checking address UID.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
        [HttpGet("CheckContactUid")]
        public async Task<IActionResult> CheckContactUid(string contactUid)
        {
            if (string.IsNullOrWhiteSpace(contactUid))
                return BadRequest(new { Success = false, Message = "ContactUid is required" });

            try
            {
                var result = await _BPService.CheckContactUidAsync(contactUid);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error checking contact UID.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetBpPANByBranch")]
        public async Task<IActionResult> GetBpPANByBranch(string Branch, int company)
        {
            if (string.IsNullOrWhiteSpace(Branch))
                return BadRequest(new { Success = false, Message = "Branch is required" });
            try
            {
                var result = await _BPService.GetBpPANByBranchAsync(Branch, company);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching BP PAN by branch.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
        [HttpGet("GetSPAData")]
        public async Task<IActionResult> GetSPAData(int masterId)
        {
            try
            {
                var result = await _BPService.GetSPADataAsync(masterId);
                if (result == null)
                {
                    return NotFound($"No SPA data found for masterId: {masterId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 50000)
                {
                    return BadRequest(sqlEx.Message);
                }
                return StatusCode(500, "A database error occurred.");
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching SPA data.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }


        [HttpGet("GetTotalBPData")]
        public async Task<IActionResult> GetTotalBPData(int userId, int companyId, string month = null)
        {
            var result = await _BPService.GetMergeBpModelAsync(userId, companyId, month);
            try
            {
                if (result == null)
                {
                    return NotFound($"No MergeBP data found:{userId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                return BadRequest(sqlEx.Message);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching total BP data");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }


        /*Master Approval*/
        [HttpGet("GetAllBpPendingApproval")]
        public async Task<IActionResult> GetAllBpPendingApproval(int userId, int companyId, string month = null)
        {
            try
            {
                var bpPending = await _BPService.GetPendingBpAsync(userId, companyId, month);
                var itemPending = await _itemMasterService.GetPendingItemsAsync(userId, companyId);

                var result = new
                {
                    BpPending = bpPending,
                    ItemPending = itemPending
                };

                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching pending BPs.");
                return StatusCode(500, new { Success = false, Message = "Internal server error: " + ex.Message });
            }
        }

        [HttpGet("GetAllBpApprovedApproval")]
        public async Task<IActionResult> GetAllApprovedBp(int userId, int companyId, string month = null)
        {
            try
            {
                var ApprovedBP = await _BPService.GetApprovedBPsAsync(userId, companyId, month);
                var ApprovedItem = await _itemMasterService.GetApprovedItemsAsync(userId, companyId);
                var result = new
                {
                    BPApproved = ApprovedBP,
                    ItemApproved = ApprovedItem
                };
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "error fetching Approved BPs");
                return StatusCode(500, new { Success = false, Message = "Internal Sever Error:" + ex.Message });
            }
        }

        [HttpGet("GetAllBpRejectedApproval")]
        public async Task<IActionResult> GetAllBpRejectedApproval(int userId, int companyId, string month = null)
        {
            try
            {
                var RejectedBP = await _BPService.GetRejectedBpAsync(userId, companyId, month);
                var RejectedItem = await _itemMasterService.GetRejectedItemsAsync(userId, companyId);
                var result = new
                {
                    BPRejected = RejectedBP,
                    ItemRejected = RejectedItem
                };
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "error fetching rejected BPs");
                return StatusCode(500, new { Success = false, Message = "Internal Sever Error:" + ex.Message });
            }
        }

        [HttpGet("GetAllBpTotalApproval")]
        public async Task<IActionResult> GetAllBpTotalApproval(int userId, int companyId, string month = null)
        {
            try
            {
                var TotalBP = await _BPService.GetMergeBpModelAsync(userId, companyId, month);
                var TotalItem = await _itemMasterService.GetAllItemsAsync(userId, companyId);
                var result = new
                {
                    BPTotal = TotalBP,
                    ItemTotal = TotalItem
                };
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "error fetching total BPs");
                return StatusCode(500, new { Success = false, Message = "Internal Sever Error:" + ex.Message });
            }
        }

        [HttpPost("UpdateBPMaster")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BPmasterModels>> UpdateBPMaster()
        {
            try
            {
                var form = await Request.ReadFormAsync();

                // Read and deserialize the main JSON data
                var requestJson = form["requests"];
                if (string.IsNullOrWhiteSpace(requestJson))
                {
                    return BadRequest(new BPmasterModels { Success = false, Message = "Missing request data" });
                }

                var model = JsonConvert.DeserializeObject<BPMasterUpdateRequest>(requestJson);

                // Attachments from files
                var files = form.Files;
                model.Attachments = new List<BPAttachment>();

                // 2. Parse fileTypes
                var fileTypeList = form["fileTypes"].ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

                if (files.Count != fileTypeList.Count)
                {
                    return BadRequest(new BPmasterModels
                    {
                        Success = false,
                        Message = "The number of fileTypes does not match the number of attachments.",
                    });
                }
                var uploadPath = Path.Combine("wwwroot", "Uploads", "BPmaster");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileType = fileTypeList[i];

                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName);
                        var newFileName = $"{Guid.NewGuid()}{ext}";
                        var savePath = Path.Combine(uploadPath, newFileName);

                        using var stream = new FileStream(savePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        model.Attachments.Add(new BPAttachment
                        {
                            FileName = newFileName,
                            FilePath = "/Uploads/BPmaster", // Use relative path for UI
                            FileSize = file.Length,
                            ContentType = file.ContentType,
                            fileType = fileType
                        });
                    }
                }

                // Call service
                var result = await _BPService.UpdateBPMasterAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error updating BP Master data.");
                return BadRequest(new BPmasterModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("UpdateSapData")]
        public async Task<ActionResult<BPmasterModels>> UpdateSapData([FromBody] BpSapDataUpdateRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new BPmasterModels { Success = false, Message = "Invalid request data" });
                }
                var result = await _BPService.UpdateSapDataAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error updating SAP data.");
                return BadRequest(new BPmasterModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetBPInsights")]
        public async Task<ActionResult<BPinsightsModel>> GetBPInsights(int userId, int companyId, string? month = null)
        {
            var result = await _BPService.GetBPInsightsAsync(userId, companyId, month);
            try
            {
                if (result == null)
                {
                    return NotFound($"No BP data found:{userId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                return BadRequest(sqlEx.Message);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching  BP data");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetBPInsightsByCreator")]
        public async Task<ActionResult<BPinsightsModel>> GetBPInsightsByCreator(int userId, int companyId, string? month = null)
        {
            var result = await _BPService.GetBPInsightsByCreatorAsync(userId, companyId, month);
            try
            {
                if (result == null)
                {
                    return NotFound($"No BP data found:{userId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                return BadRequest(sqlEx.Message);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching  BP data");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetBPApprovalFlow")]
        public async Task<ActionResult<BPApprovalFlowModel>> GetBPApprovalFlow (int flowId)
        {
            var result = await _BPService.GetBPApprovalFlowAsync(flowId);
            try
            {
                if (result == null)
                {
                    return NotFound($"No BP data found:{flowId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                return BadRequest(sqlEx.Message);
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching  BP data");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
    }
}

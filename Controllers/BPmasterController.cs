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
        private readonly ILogger<BPmasterController> _BPlogger;

        public BPmasterController(IConfiguration configuration, IBPmasterService bpService, ILogger<BPmasterController> logger)
        {
            _configuration = configuration;
            _BPService = bpService;
            _BPlogger = logger;
        }

        private static object BuildSapQueryErrorResponse(Exception ex)
        {
            var sapError = ExtractSapQueryError(ex);
            return new
            {
                Success = false,
                Message = "SAP query failed",
                ErrorCode = "SAP_QUERY_FAILED",
                SapError = new
                {
                    code = sapError.SapCode,
                    message = sapError.Message
                }
            };
        }

        private static BpSapError ExtractSapQueryError(Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                if (current is BpSapException sapException)
                    return sapException.SapError;

                current = current.InnerException;
            }

            return BpSapErrorMapper.ExtractSapError(ex);
        }

        [HttpPost("InsertBPmasterData")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> InsertBPMaster()
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
                return result.Success
                    ? Ok(new
                    {
                        success = true,
                        message = result.Message,
                        generatedCode = result.GeneratedCode,
                        masterId = result.MasterId,
                        sapDataId = result.SapDataId,
                        flowId = result.FlowId,
                        status = result.Status
                    })
                    : BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        generatedCode = result.GeneratedCode,
                        masterId = result.MasterId,
                        sapDataId = result.SapDataId,
                        flowId = result.FlowId,
                        status = result.Status
                    });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error saving BP Master data.");
                return BadRequest(new
                {
                    success = false,
                    message = "BP Master insert failed.",
                    generatedCode = 0
                });
            }
        }

        [HttpGet("GetOptions")]
        [HttpGet("Options")]
        public async Task<IActionResult> GetOptions(int company, string bpType = "C", string isStaff = "false", string countryCode = "IN")
        {
            if (company <= 0)
                return BadRequest(new { Success = false, Message = "Company is required." });

            try
            {
                var result = await _BPService.GetOptionsAsync(company, bpType, isStaff, countryCode);
                return Ok(new
                {
                    Success = result.Errors == null || !result.Errors.Any(),
                    Data = result,
                    Message = result.Errors != null && result.Errors.Any()
                        ? "Some BP options could not be loaded. Check Data.Errors for details."
                        : "BP options loaded successfully."
                });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching BP options.");
                return StatusCode(500, new { Success = false, Message = "BP options could not be loaded." });
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
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
            }
        }

        [HttpGet("GetBankCodes")]
        public async Task<IActionResult> GetBankCodes(int company, string countryCode = "IN")
        {
            try
            {
                var result = await _BPService.GetBankCodesAsync(company, countryCode);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching SAP bank codes. Company={Company}, CountryCode={CountryCode}", company, countryCode);
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
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

        [HttpGet("GetBPGroups")]
        public async Task<IActionResult> GetBPGroups(int company, string bpType = "C")
        {
            try
            {
                var result = await _BPService.GetBPGroupsAsync(company, bpType);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching BP groups. Company={Company}, BpType={BpType}", company, bpType);
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
            }
        }

        [HttpGet("GetARAccounts")]
        public async Task<IActionResult> GetARAccounts(int company)
        {
            try
            {
                var result = await _BPService.GetARAccountsAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching AR accounts. Company={Company}", company);
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
            }
        }

        [HttpGet("GetAPAccounts")]
        public async Task<IActionResult> GetAPAccounts(int company)
        {
            try
            {
                var result = await _BPService.GetAPAccountsAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching AP accounts. Company={Company}", company);
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
            }
        }

        [HttpGet("GetPaymentTerms")]
        public async Task<IActionResult> GetPaymentTerms(int company)
        {
            try
            {
                var result = await _BPService.GetPaymentTermsAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching payment terms. Company={Company}", company);
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
            }
        }

        [HttpGet("GetSalesEmployees")]
        public async Task<IActionResult> GetSalesEmployees(int company)
        {
            try
            {
                var result = await _BPService.GetSalesEmployeesAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching sales employees. Company={Company}", company);
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
            }
        }

        [HttpGet("GetTerritories")]
        public async Task<IActionResult> GetTerritories(int company)
        {
            try
            {
                var result = await _BPService.GetTerritoriesAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching territories. Company={Company}", company);
                return StatusCode(500, BuildSapQueryErrorResponse(ex));
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

                return Ok(new { Success = true, Data = ToBpListResponse(result) });
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(ex, "SQL error while fetching approved BPs.");
                return BadRequest(new { Success = false, Message = "Approved BP list could not be loaded." });
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
                return Ok(new { Success = true, Data = ToBpListResponse(result) });
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(ex, "SQL error while fetching pending BPs.");
                return BadRequest(new { Success = false, Message = "Pending BP list could not be loaded." });
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
                return Ok(new { Success = true, Data = ToBpListResponse(result) });
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
                _BPlogger.LogError(ex, "SQL error while fetching BP details. BpCode={BpCode}", bpCode);
                return StatusCode(500, new { Success = false, Message = "BP details could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Unexpected error while fetching BP details. BpCode={BpCode}", bpCode);
                return StatusCode(500, new { Success = false, Message = "BP details could not be loaded." });
            }
        }


        [HttpPost("ApproveBP")]
        public async Task<IActionResult> ApproveBP([FromBody] ApproveOrRejectBpRequest request)
        {
            try
            {
                var result = await _BPService.ApproveBPAsync(request);
                if (!result.Success)
                    return BpFailure(result.ResultMessage, result.ErrorCode, sapError: result.SapError);

                return BpSuccess(result.ResultMessage, ToApprovalResponseData(result, request.FlowId));
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(
                    ex,
                    "SQL error during BP approval. FlowId={FlowId}, Company={Company}, UserId={UserId}",
                    request.FlowId,
                    request.Company,
                    request.UserId);
                return BpFailure(MapSqlExceptionMessage(ex), MapSqlExceptionCode(ex));
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(
                    ex,
                    "Error during BP approval. FlowId={FlowId}, Company={Company}, UserId={UserId}",
                    request.FlowId,
                    request.Company,
                    request.UserId);
                return BpFailure("BP approval failed because of an internal server error.", "BP_INTERNAL_ERROR", 500);
            }
        }

        [HttpPost("RejectBP")]
        public async Task<IActionResult> RejectBP([FromBody] ApproveOrRejectBpRequest request)
        {
            try
            {
                var result = await _BPService.RejectBPAsync(request);
                if (!result.Success)
                    return BpFailure(result.ResultMessage, result.ErrorCode, sapError: result.SapError);

                return BpSuccess(result.ResultMessage, ToApprovalResponseData(result, request.FlowId));
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(
                    ex,
                    "SQL error during BP rejection. FlowId={FlowId}, Company={Company}, UserId={UserId}",
                    request.FlowId,
                    request.Company,
                    request.UserId);
                return BpFailure(MapSqlExceptionMessage(ex), MapSqlExceptionCode(ex));
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(
                    ex,
                    "Error during BP rejection. FlowId={FlowId}, Company={Company}, UserId={UserId}",
                    request.FlowId,
                    request.Company,
                    request.UserId);
                return BpFailure("BP rejection failed because of an internal server error.", "BP_INTERNAL_ERROR", 500);
            }
        }

        [HttpPost("RetrySapPost")]
        public async Task<IActionResult> RetrySapPost([FromBody] ApproveOrRejectBpRequest request)
        {
            try
            {
                request.Action = "Approve";
                var result = await _BPService.RetrySapPostAsync(request);
                if (!result.Success)
                    return BpFailure(result.ResultMessage, result.ErrorCode, sapError: result.SapError);

                return BpSuccess(result.ResultMessage, ToApprovalResponseData(result, request.FlowId));
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(
                    ex,
                    "SQL error during BP SAP retry. FlowId={FlowId}, Company={Company}, UserId={UserId}",
                    request.FlowId,
                    request.Company,
                    request.UserId);
                return BpFailure(MapSqlExceptionMessage(ex), MapSqlExceptionCode(ex));
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(
                    ex,
                    "Error during BP SAP retry. FlowId={FlowId}, Company={Company}, UserId={UserId}",
                    request.FlowId,
                    request.Company,
                    request.UserId);
                return BpFailure("BP SAP retry failed because of an internal server error.", "BP_INTERNAL_ERROR", 500);
            }
        }

        private IActionResult BpSuccess(string message, object? data)
        {
            return Ok(new
            {
                success = true,
                message,
                data
            });
        }

        private static List<BpListResponseModel> ToBpListResponse<T>(IEnumerable<T> rows)
            where T : BpListModel
        {
            return rows.Select(row => new BpListResponseModel
            {
                Workflow = new BpWorkflowModel
                {
                    FlowId = row.flowId,
                    SapStatus = row.SapStatus ?? string.Empty,
                    ApiMessage = row.ApiMessage ?? string.Empty,
                    SapCardCode = row.SapCardCode ?? string.Empty
                },
                Master = row.Master,
                TaxDetails = row.TaxDetails,
                BillingAddresses = row.BillingAddresses,
                ShippingAddresses = row.ShippingAddresses,
                BankDetails = row.BankDetails,
                ContactPersons = row.ContactPersons,
                Attachments = row.Attachments
            }).ToList();
        }

        private IActionResult BpFailure(string message, string errorCode, int statusCode = 400, BpSapErrorInfo? sapError = null)
        {
            var sapMessage = sapError == null || string.IsNullOrWhiteSpace(sapError.message)
                ? string.Empty
                : sapError.message;
            var responseMessage = !string.IsNullOrWhiteSpace(sapMessage)
                ? sapMessage
                : string.IsNullOrWhiteSpace(message) ? "BP workflow operation failed." : message;
            responseMessage = FormatClientFailureMessage(responseMessage, sapError);
            var responseSapStatus = $"Failed: {responseMessage}";

            var response = new Dictionary<string, object?>
            {
                ["success"] = false,
                ["approvalStatus"] = "Blocked",
                ["sapStatus"] = responseSapStatus,
                ["message"] = responseMessage
            };

            return statusCode == 400 ? BadRequest(response) : StatusCode(statusCode, response);
        }

        private static string FormatClientFailureMessage(string message, BpSapErrorInfo? sapError)
        {
            if (sapError?.code is not int sapCode)
                return message;

            return MessageAlreadyContainsSapCode(message, sapCode)
                ? message
                : $"{message} (SAP Error Code: {sapCode})";
        }

        private static bool MessageAlreadyContainsSapCode(string message, int sapCode)
        {
            return message.Contains($"SAP Error Code: {sapCode}", StringComparison.OrdinalIgnoreCase)
                || message.Contains($"({sapCode})", StringComparison.OrdinalIgnoreCase);
        }

        private static BpApprovalResponseData ToApprovalResponseData(ApproveOrRejectBpResponse result, int fallbackFlowId)
        {
            return new BpApprovalResponseData
            {
                approvalStatus = result.ApprovalStatus,
                sapStatus = result.SapStatus,
                sapCardCode = result.SapCardCode
            };
        }

        private static string MapSqlExceptionCode(SqlException ex)
        {
            return ex.Number switch
            {
                50006 => "BP_UNAUTHORIZED_APPROVER",
                50020 => "BP_DUPLICATE_APPROVAL",
                50110 => "BP_SAP_NOT_SUCCESSFUL",
                50111 => "BP_SAP_ALREADY_PROCESSING",
                50112 => "BP_SAP_ALREADY_POSTED",
                _ => "BP_SQL_ERROR"
            };
        }

        private static string MapSqlExceptionMessage(SqlException ex)
        {
            return ex.Number switch
            {
                50006 => "User is not authorized for the current BP approval stage.",
                50020 => "This BP approval action has already been processed.",
                50110 => "BP cannot be approved until SAP posting succeeds.",
                50111 => "SAP BP creation is already processing. Please retry shortly.",
                50112 => "BP has already been posted successfully to SAP.",
                _ => "BP workflow operation failed."
            };
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
                _BPlogger.LogError(ex, "Error retrieving unique PANs.");
                return StatusCode(500, new { Success = false, message = "Unique PAN list could not be loaded." });
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
                return StatusCode(500, new { Success = false, message = "GST mismatch data could not be loaded." });
            }
        }

        [HttpGet("GetBPCounts")]
        public async Task<IActionResult> GetBPCounts(string month, int userId, int companyId = 0)
        {
            try
            {
                var result = await _BPService.GetBPCountsAsync(month, userId, companyId);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(ex, "SQL error while fetching BP counts.");
                return StatusCode(500, new { Success = false, Message = "BP counts could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Unexpected error while fetching BP counts.");
                return StatusCode(500, new { Success = false, Message = "BP counts could not be loaded." });
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
                _BPlogger.LogError(ex, "SQL error fetching BP PAN by branch. Branch={Branch}, Company={Company}", Branch, company);
                return BadRequest(new { Success = false, Message = "BP PAN by branch could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching BP PAN by branch.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
        [HttpGet("GetSPAData")]
        [HttpGet("GetSAPData")]
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
                _BPlogger.LogError(sqlEx, "SQL error while fetching SPA data. MasterId={MasterId}", masterId);
                return BadRequest(new { Success = false, Message = "SPA data could not be loaded." });
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
            try
            {
                var result = await _BPService.GetMergeBpModelAsync(userId, companyId, month);
                if (result == null)
                {
                    return NotFound($"No MergeBP data found:{userId}");
                }
                return Ok(new { Success = true, Data = ToBpListResponse(result) });
            }
            catch (SqlException sqlEx)
            {
                _BPlogger.LogError(sqlEx, "SQL error while fetching total BP data. UserId={UserId}, CompanyId={CompanyId}", userId, companyId);
                return BadRequest(new { Success = false, Message = "Total BP data could not be loaded." });
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

                var result = new
                {
                    BpPending = ToBpListResponse(bpPending)
                };

                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(ex, "SQL error while fetching all BP pending approvals.");
                return BadRequest(new { Success = false, Message = "Pending approval list could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching pending BPs.");
                return StatusCode(500, new { Success = false, Message = "Pending approval list could not be loaded." });
            }
        }

        [HttpGet("GetAllBpApprovedApproval")]
        public async Task<IActionResult> GetAllApprovedBp(int userId, int companyId, string month = null)
        {
            try
            {
                var ApprovedBP = await _BPService.GetApprovedBPsAsync(userId, companyId, month);
                var result = new
                {
                    BPApproved = ToBpListResponse(ApprovedBP)
                };
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(ex, "SQL error while fetching all approved BP approvals.");
                return BadRequest(new { Success = false, Message = "Approved approval list could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "error fetching Approved BPs");
                return StatusCode(500, new { Success = false, Message = "Approved approval list could not be loaded." });
            }
        }

        [HttpGet("GetAllBpRejectedApproval")]
        public async Task<IActionResult> GetAllBpRejectedApproval(int userId, int companyId, string month = null)
        {
            try
            {
                var RejectedBP = await _BPService.GetRejectedBpAsync(userId, companyId, month);
                var result = new
                {
                    BPRejected = ToBpListResponse(RejectedBP)
                };
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(ex, "SQL error while fetching all rejected BP approvals.");
                return BadRequest(new { Success = false, Message = "Rejected approval list could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "error fetching rejected BPs");
                return StatusCode(500, new { Success = false, Message = "Rejected approval list could not be loaded." });
            }
        }

        [HttpGet("GetAllBpTotalApproval")]
        public async Task<IActionResult> GetAllBpTotalApproval(int userId, int companyId, string month = null)
        {
            try
            {
                var TotalBP = await _BPService.GetMergeBpModelAsync(userId, companyId, month);
                var result = new
                {
                    BPTotal = ToBpListResponse(TotalBP)
                };
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                _BPlogger.LogError(ex, "SQL error while fetching all BP total approvals.");
                return BadRequest(new { Success = false, Message = "Total approval list could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "error fetching total BPs");
                return StatusCode(500, new { Success = false, Message = "Total approval list could not be loaded." });
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
                    Message = "BP Master update failed."
                });
            }
        }

        [HttpPost("UpdateSapData")]
        [HttpPut("UpdateSAPData")]
        [HttpPost("UpdateSAPData")]
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
                    Message = "SAP metadata update failed."
                });
            }
        }

        [HttpGet("GetBPInsights")]
        public async Task<ActionResult<BPinsightsModel>> GetBPInsights(int userId, int companyId, string? month = null)
        {
            try
            {
                var result = await _BPService.GetBPInsightsAsync(userId, companyId, month);
                if (result == null)
                {
                    return NotFound($"No BP data found:{userId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                _BPlogger.LogError(sqlEx, "SQL error while fetching BP insights.");
                return BadRequest(new { Success = false, Message = "BP insights could not be loaded." });
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
            try
            {
                var result = await _BPService.GetBPInsightsByCreatorAsync(userId, companyId, month);
                if (result == null)
                {
                    return NotFound($"No BP data found:{userId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                _BPlogger.LogError(sqlEx, "SQL error while fetching BP insights by creator.");
                return BadRequest(new { Success = false, Message = "BP insights by creator could not be loaded." });
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
            try
            {
                var result = await _BPService.GetBPApprovalFlowAsync(flowId);
                if (result == null)
                {
                    return NotFound($"No BP data found:{flowId}");
                }
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException sqlEx)
            {
                _BPlogger.LogError(sqlEx, "SQL error while fetching BP approval flow. FlowId={FlowId}", flowId);
                return BadRequest(new { Success = false, Message = "BP approval flow could not be loaded." });
            }
            catch (Exception ex)
            {
                _BPlogger.LogError(ex, "Error fetching  BP data");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
    }
}

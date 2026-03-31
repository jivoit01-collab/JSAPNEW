using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ItemMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IItemMasterService _ItemMasterService;
        private readonly ILogger<ItemMasterController> _Itemmasterlogger;

        public ItemMasterController(IConfiguration configuration, IItemMasterService ItemMasterService, ILogger<ItemMasterController> Itemmasterlogger)
        {
            _configuration = configuration;
            _ItemMasterService = ItemMasterService;
            _Itemmasterlogger = Itemmasterlogger;
        }

        [HttpGet("GetHSN")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetHSN(int company)
        {
            try
            {
                var HSN = await _ItemMasterService.GetHSNAsync(company);
                if (HSN == null)
                {
                    _Itemmasterlogger.LogInformation("No HSN found");
                    return NotFound(new { Success = false, Message = "No HSN found" });
                }
                _Itemmasterlogger.LogInformation("HSN retrieved successfully.");
                return Ok(new { Success = true, Data = HSN });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving HSN.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetTaxRate")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetTaxRate(int company)
        {
            try
            {
                var TaxRate = await _ItemMasterService.GetTaxRateAsync(company);
                if (TaxRate == null)
                {
                    _Itemmasterlogger.LogInformation("No TaxRate found");
                    return NotFound(new { Success = false, Message = "No TaxRate found" });
                }
                _Itemmasterlogger.LogInformation("TaxRate retrieved successfully.");
                return Ok(new { Success = true, Data = TaxRate });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving TaxRate.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetInventoryUOM")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetInventoryUOM(int company)
        {
            try
            {
                var InventoryUOM = await _ItemMasterService.GetInventoryUOMAsync(company);
                if (InventoryUOM == null)
                {
                    _Itemmasterlogger.LogInformation("No InventoryUOM found");
                    return NotFound(new { Success = false, Message = "No InventoryUOM found" });
                }
                _Itemmasterlogger.LogInformation("InventoryUOM retrieved successfully.");
                return Ok(new { Success = true, Data = InventoryUOM });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving InventoryUOM.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetPackingType")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetPackingType(int GroupCode, int company)
        {
            try
            {
                var PackingType = await _ItemMasterService.GetPackingTypeAsync(GroupCode, company);
                if (PackingType == null)
                {
                    _Itemmasterlogger.LogInformation("No PackingType found");
                    return NotFound(new { Success = false, Message = "No PackingType found" });
                }
                _Itemmasterlogger.LogInformation("PackingType retrieved successfully.");
                return Ok(new { Success = true, Data = PackingType });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving PackingType.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetPackType")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetPackType(int GroupCode, int company)
        {
            try
            {
                var PackType = await _ItemMasterService.GetPackTypeAsync(GroupCode, company);
                if (PackType == null)
                {
                    _Itemmasterlogger.LogInformation("No PackType found");
                    return NotFound(new { Success = false, Message = "No PackType found" });
                }
                _Itemmasterlogger.LogInformation("PackType retrieved successfully.");
                return Ok(new { Success = true, Data = PackType });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving PackType.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetPurPackType")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetPurPackType(int company)
        {
            try
            {
                var PurPack = await _ItemMasterService.GetPurPackAsync(company);
                if (PurPack == null)
                {
                    _Itemmasterlogger.LogInformation("No PurPack found");
                    return NotFound(new { Success = false, Message = "No PurPack found" });
                }
                _Itemmasterlogger.LogInformation("PurPack retrieved successfully.");
                return Ok(new { Success = true, Data = PurPack });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving PurPack.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetSalPackType")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetSalPackType(int company)
        {
            try
            {
                var SalPack = await _ItemMasterService.GetSalPackAsync(company);
                if (SalPack == null)
                {
                    _Itemmasterlogger.LogInformation("No SalPack found");
                    return NotFound(new { Success = false, Message = "No SalPack found" });
                }
                _Itemmasterlogger.LogInformation("SalPack retrieved successfully.");
                return Ok(new { Success = true, Data = SalPack });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving SalPack.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetSalUnitType")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetSalUnitType(int GroupCode, int company)
        {
            try
            {
                var SalUnit = await _ItemMasterService.GetSalUnitAsync(GroupCode, company);
                if (SalUnit == null)
                {
                    _Itemmasterlogger.LogInformation("No SalUnit found");
                    return NotFound(new { Success = false, Message = "No SalUnit found" });
                }
                _Itemmasterlogger.LogInformation("SalUnit retrieved successfully.");
                return Ok(new { Success = true, Data = SalUnit });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving SalUnit.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetSKU")]
        public async Task<ActionResult> GetSKUType(int GroupCode, int company)
        {
            try
            {
                var SKU = await _ItemMasterService.GetSKUAsync(GroupCode, company);
                if (SKU == null)
                {
                    _Itemmasterlogger.LogInformation("No SKU found");
                    return NotFound(new { Success = false, Message = "No SKU found" });
                }
                _Itemmasterlogger.LogInformation("SKU retrieved successfully.");
                return Ok(new { Success = true, Data = SKU });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving SKU.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }


        [HttpGet("GetVariety")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GeVarietyType(string BRAND, int GroupCode, int company)
        {
            try
            {
                var variety = await _ItemMasterService.GetVarietyAsync(BRAND, GroupCode, company);
                if (variety == null)
                {
                    _Itemmasterlogger.LogInformation("No variety found");
                    return NotFound(new { Success = false, Message = "No variety found" });
                }
                _Itemmasterlogger.LogInformation("variety retrieved successfully.");
                return Ok(new { Success = true, Data = variety });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving variety.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetSubGroup")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetSubGroup(string BRAND, string VARIETY, int GroupCode, int company)
        {
            try
            {
                var group = await _ItemMasterService.GetSubGroupAsync(BRAND, VARIETY, GroupCode, company);
                if (group == null)
                {
                    _Itemmasterlogger.LogInformation("No group found");
                    return NotFound(new { Success = false, Message = "No group found" });
                }
                _Itemmasterlogger.LogInformation("group retrieved successfully.");
                return Ok(new { Success = true, Data = group });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving group.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetUnit")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetUnitType(int GroupCode, int company)
        {
            try
            {
                var Unit = await _ItemMasterService.GetUnitAsync(GroupCode, company);
                if (Unit == null)
                {
                    _Itemmasterlogger.LogInformation("No Unit found");
                    return NotFound(new { Success = false, Message = "No Unit found" });
                }
                _Itemmasterlogger.LogInformation("Unit retrieved successfully.");
                return Ok(new { Success = true, Data = Unit });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving Unit.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetFA")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetFAType(int GroupCode, int company)
        {
            try
            {
                var FAType = await _ItemMasterService.GetFaAsync(GroupCode, company);
                if (FAType == null)
                {
                    _Itemmasterlogger.LogInformation("No FAType found");
                    return NotFound(new { Success = false, Message = "No FAType found" });
                }
                _Itemmasterlogger.LogInformation("FAType retrieved successfully.");
                return Ok(new { Success = true, Data = FAType });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving FAType.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBuyUnit")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetBuyUnitType(int company)
        {
            try
            {
                var BuyUniyUOM = await _ItemMasterService.GetBuyUnitAsync(company);
                if (BuyUniyUOM == null)
                {
                    _Itemmasterlogger.LogInformation("No BuyUniyUOM found");
                    return NotFound(new { Success = false, Message = "No BuyUniyUOM found" });
                }
                _Itemmasterlogger.LogInformation("BuyUniyUOM retrieved successfully.");
                return Ok(new { Success = true, Data = BuyUniyUOM });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving BuyUniyUOM.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetGroup")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetGroupType(int company)
        {
            try
            {
                var Group = await _ItemMasterService.GetGroupAsync(company);
                if (Group == null)
                {
                    _Itemmasterlogger.LogInformation("No Group found");
                    return NotFound(new { Success = false, Message = "No Group found" });
                }
                _Itemmasterlogger.LogInformation("Group retrieved successfully.");
                return Ok(new { Success = true, Data = Group });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving Group.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBrand")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetBrandType(int GroupCode, int company)
        {
            try
            {
                var Brand = await _ItemMasterService.GetBrandAsync(GroupCode, company);
                if (Brand == null)
                {
                    _Itemmasterlogger.LogInformation("No Brand found");
                    return NotFound(new { Success = false, Message = "No Brand found" });
                }
                _Itemmasterlogger.LogInformation("Brand retrieved successfully.");
                return Ok(new { Success = true, Data = Brand });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving Brand.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("ApproveItem")]
       // [CheckUserPermission("item_master_creation", "approve")]
        public async Task<ActionResult> ApproveItem([FromBody] ApproveItemModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.ApproveItemAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("No items found for approval");
                    return NotFound(new { Success = false, Message = "No items found for approval" });
                }
                _Itemmasterlogger.LogInformation("Items approved. Result: {Message}", result.Message);
                return Ok(new
                {
                    Success        = result.Success,
                    ApprovalStatus = result.ApprovalStatus,
                    SapStatus      = result.SapStatus,
                    MartStatus     = result.MartStatus,
                    Message        = result.Message
                });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while approving items.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetApprovedItems")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetApprovedItems(int userId, int company)
        {
            try
            {
                var approvedItems = await _ItemMasterService.GetApprovedItemsAsync(userId, company);
                if (approvedItems == null || !approvedItems.Any())
                {
                    _Itemmasterlogger.LogInformation("No approved items found");
                    return NotFound(new { Success = false, Message = "No approved items found" });
                }
                _Itemmasterlogger.LogInformation("Approved items retrieved successfully.");
                return Ok(new { Success = true, Data = approvedItems });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving approved items.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpGet("GetFullItemDetails")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetFullItemDetails(int itemId)
        {
            try
            {
                var itemDetails = await _ItemMasterService.GetFullItemDetailsAsync(itemId);
                if (itemDetails == null)
                {
                    _Itemmasterlogger.LogInformation("No item details found for the given ID");
                    return NotFound(new { Success = false, Message = "No item details found for the given ID" });
                }
                _Itemmasterlogger.LogInformation("Item details retrieved successfully.");
                return Ok(new { Success = true, Data = itemDetails });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving item details.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetPendingItems")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetPendingItems(long userId, int company)
        {
            try
            {
                var pendingItems = await _ItemMasterService.GetPendingItemsAsync(userId, company);
                if (pendingItems == null || !pendingItems.Any())
                {
                    _Itemmasterlogger.LogInformation("No pending items found");
                    return NotFound(new { Success = false, Message = "No pending items found" });
                }
                _Itemmasterlogger.LogInformation("Pending items retrieved successfully.");
                return Ok(new { Success = true, Data = pendingItems });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving pending items.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetRejectedItems")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetRejectedItems(long userId, int company)
        {
            try
            {
                var rejectedItems = await _ItemMasterService.GetRejectedItemsAsync(userId, company);
                if (rejectedItems == null || !rejectedItems.Any())
                {
                    _Itemmasterlogger.LogInformation("No rejected items found");
                    return NotFound(new { Success = false, Message = "No rejected items found" });
                }
                _Itemmasterlogger.LogInformation("Rejected items retrieved successfully.");
                return Ok(new { Success = true, Data = rejectedItems });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving rejected items.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetWorkflowInsights")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetWorkflowInsights(int userId, int companyId, string? month=null)
        {
            try
            {
                var insights = await _ItemMasterService.GetWorkflowInsightsAsync(userId, companyId, month);
                if (insights == null)
                {
                    _Itemmasterlogger.LogInformation("No workflow insights found");
                    return NotFound(new { Success = false, Message = "No workflow insights found" });
                }
                _Itemmasterlogger.LogInformation("Workflow insights retrieved successfully.");
                return Ok(new { Success = true, Data = insights });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving workflow insights.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("InsertInitData")]
        //[CheckUserPermission("item_master_creation", "create")]
        public async Task<ActionResult> InsertInitData([FromBody] InsertInitDataModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.InsertInitDataAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to insert init data");
                    return NotFound(new { Success = false, Message = "Failed to insert init data" });
                }
                _Itemmasterlogger.LogInformation("Init data inserted successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while inserting init data.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("InsertSAPData")]
        //[CheckUserPermission("item_master_creation", "update_sapdata")]
        public async Task<ActionResult> InsertSAPData([FromBody] InsertSAPDataModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.InsertSAPDataAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to insert SAP data");
                    return NotFound(new { Success = false, Message = "Failed to insert SAP data" });
                }
                _Itemmasterlogger.LogInformation("SAP data inserted successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while inserting SAP data.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("RejectItem")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> RejectItem([FromBody] RejectItemModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.RejectItemAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to reject item");
                    return NotFound(new { Success = false, Message = "Failed to reject item" });
                }
                _Itemmasterlogger.LogInformation("Item rejected successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while rejecting item.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("UpdateInitData")]
        // [CheckUserPermission("item_master_creation", "update_initdata")]
        public async Task<ActionResult<ItemMasterModel>> UpdateInitData([FromBody] UpdateInitDataModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new ItemMasterModel { Success = false, Message = "Invalid request" });
                }

                var result = await _ItemMasterService.UpdateInitDataAsync(request);

                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to update init data");
                    return NotFound(new ItemMasterModel { Success = false, Message = "Failed to update init data" });
                }

                _Itemmasterlogger.LogInformation("Init data updated successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while updating init data.");
                return StatusCode(500, new ItemMasterModel { Success = false, Message = ex.Message });
            }
        }
        


        [HttpPost("UpdateSAPData")]
            //[CheckUserPermission("item_master_creation", "update_sapdata")]

           public async Task<ActionResult> UpdateSAPData([FromBody] UpdateSAPDataModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.UpdateSAPDataAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to update SAP data");
                    return NotFound(new { Success = false, Message = "Failed to update SAP data" });
                }
                _Itemmasterlogger.LogInformation("SAP data updated successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while updating SAP data.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetAllItems")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetAllItems(int userId, int companyId)
        {
            try
            {
                var items = await _ItemMasterService.GetAllItemsAsync(userId, companyId);
                if (items == null || !items.Any())
                {
                    _Itemmasterlogger.LogInformation("No items found");
                    return NotFound(new { Success = false, Message = "No items found" });
                }
                _Itemmasterlogger.LogInformation("Items retrieved successfully.");
                return Ok(new { Success = true, Data = items });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving items.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("InsertFullItem")]
        //[CheckUserPermission("item_master_creation", "create")]
        public async Task<ActionResult<ItemMasterModel>> InsertFullItem([FromBody] InsertFullItemDataModel request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ItemMasterModel { Success = false, Message = "Request is null" });

                var result = await _ItemMasterService.InsertFullItemDataAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error in InsertFullItemData");
                return StatusCode(500, new ItemMasterModel { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBuyUnitMsr")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetBuyUnitMsr(int GroupCode, int company)
        {
            try
            {
                var buyUnitMsr = await _ItemMasterService.GetBuyUnitMsrAsync(GroupCode, company);
                if (buyUnitMsr == null)
                {
                    _Itemmasterlogger.LogInformation("No Buy Unit Msrs found");
                    return NotFound(new { Success = false, Message = "No Buy Unit Msrs found" });
                }
                _Itemmasterlogger.LogInformation("Buy Unit Msrs retrieved successfully.");
                return Ok(new { Success = true, Data = buyUnitMsr });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving Buy Unit Msrs.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetInvUnitMsr")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetInvUnitMsr(int GroupCode, int company)
        {
            try
            {
                var invUnitMsr = await _ItemMasterService.GetInvUnitMsrAsync(GroupCode, company);
                if (invUnitMsr == null)
                {
                    _Itemmasterlogger.LogInformation("No Inventory Unit Msrs found");
                    return NotFound(new { Success = false, Message = "No Inventory Unit Msrs found" });
                }
                _Itemmasterlogger.LogInformation("Inventory Unit Msrs retrieved successfully.");
                return Ok(new { Success = true, Data = invUnitMsr });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving Inventory Unit Msrs.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("JsGetUOMGroup")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> JsGetUOMGroup(int GroupCode, int company)
        {
            try
            {
                var uomGroup = await _ItemMasterService.JsGetUOMGroupAsync(GroupCode, company);
                if (uomGroup == null)
                {
                    _Itemmasterlogger.LogInformation("No UOM Group found");
                    return NotFound(new { Success = false, Message = "No UOM Group found" });
                }
                _Itemmasterlogger.LogInformation("UOM Group retrieved successfully.");
                return Ok(new { Success = true, Data = uomGroup });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving UOM Group.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetDistinctItemName")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<IActionResult> GetDistinctItemName(int company)
        {

            try
            {
                var result = await _ItemMasterService.GetDistinctItemNameAsync(company);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error fetching Distinct Item Name.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }


        [HttpGet("GetPendingItemApiInsertions")]
        public async Task<ActionResult> GetPendingItemApiInsertions(int ItemId)
        {
            try
            {
                var pendingItems = await _ItemMasterService.GetPendingItemApiInsertionsAsync(ItemId);
                if (pendingItems == null || !pendingItems.Any())
                {
                    _Itemmasterlogger.LogInformation("No pending item API insertions found");
                    return NotFound(new { Success = false, Message = "No pending item API insertions found" });
                }
                _Itemmasterlogger.LogInformation("Pending item API insertions retrieved successfully.");
                return Ok(new { Success = true, Data = pendingItems });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving pending item API insertions.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("Items")]
        public async Task<IActionResult> PostBomsToSAP(int ItemId)
        {
            try
            {
                var ItemInsertions = await _ItemMasterService.GetPendingItemApiInsertionsAsync(ItemId);
                if (ItemInsertions == null || !ItemInsertions.Any())
                    return BadRequest(new { Success = false, Message = "No pending Items to post" });

                var syncResults = await _ItemMasterService.PostItemsToSAPAsync(ItemInsertions);
                return Ok(new { Success = true, Data = syncResults });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error posting Items to SAP");
                return StatusCode(500, new { Success = false, Message = $"Error posting Items to SAP: {ex.Message}" });
            }
        }

        [HttpGet("GetIMCApprovalFlow")]
        // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetIMCApprovalFlow(int flowId)
        {
            try
            {
                var approvalFlow = await _ItemMasterService.GetIMCApprovalFlowAsync(flowId);
                if (approvalFlow == null || !approvalFlow.Any())
                {
                    _Itemmasterlogger.LogInformation("No IMC approval flow found");
                    return NotFound(new { Success = false, Message = "No IMC approval flow found" });
                }
                _Itemmasterlogger.LogInformation("IMC approval flow retrieved successfully.");
                return Ok(new { Success = true, Data = approvalFlow });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving IMC approval flow.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetCreatedByDetail")]
        public async Task<ActionResult> GetCreatedByDetail(int userId, int companyId)
        {
            try
            {
                var createdByDetails = await _ItemMasterService.GetCreatedByDetailAsync(userId, companyId);
                if (createdByDetails == null)
                {
                    _Itemmasterlogger.LogInformation("No user details found for the given ID");
                    return NotFound(new { Success = false, Message = "No user details found for the given ID" });
                }
                _Itemmasterlogger.LogInformation("User details retrieved successfully.");
                return Ok(new { Success = true, Data = createdByDetails });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving user details.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        // --------------------------- BKDT start --------------------------------

        [HttpGet("GetUserDetails")]
       // [CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetUserDetails(int company)
        {
            try
            {
                var userDetails = await _ItemMasterService.GetUserDetailsAsync(company);
                if (userDetails == null || !userDetails.Any())
                {
                    _Itemmasterlogger.LogInformation("No user details found");
                    return NotFound(new { Success = false, Message = "No user details found" });
                }
                _Itemmasterlogger.LogInformation("User details retrieved successfully.");
                return Ok(new { Success = true, Data = userDetails });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving user details.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetMobjDetails")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetMobjDetails(int company)
        {
            try
            {
                var mobjDetails = await _ItemMasterService.GetMobjDetailsAsync(company);
                if (mobjDetails == null || !mobjDetails.Any())
                {
                    _Itemmasterlogger.LogInformation("No Mobj details found");
                    return NotFound(new { Success = false, Message = "No Mobj details found" });
                }
                _Itemmasterlogger.LogInformation("Mobj details retrieved successfully.");
                return Ok(new { Success = true, Data = mobjDetails });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving Mobj details.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("SaveBKDT")]
        //[CheckUserPermission("item_master_creation", "create")]
        public async Task<ActionResult> SaveBKDT([FromBody] BKDTModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.SaveBKDTAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to save BKDT data");
                    return NotFound(new { Success = false, Message = "Failed to save BKDT data" });
                }
                _Itemmasterlogger.LogInformation("BKDT data saved successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while saving BKDT data.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBKDTinsights")]
        //[CheckUserPermission("item_master_creation", "view")]
        public async Task<ActionResult> GetBKDTinsights(int userId, int company, string month)
        {
            try
            {
                var bkdtDetails = await _ItemMasterService.GetBKDTinsightsAsync(userId, company, month);
                if (bkdtDetails == null || !bkdtDetails.Any())
                {
                    _Itemmasterlogger.LogInformation("No Insights data found");
                    return NotFound(new { Success = false, Message = "No Insights data found" });
                }
                _Itemmasterlogger.LogInformation("Insights data retrieved successfully.");
                return Ok(new { Success = true, Data = bkdtDetails });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving Insights data.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBKDTPendingDoc")]
        public async Task<ActionResult> GetBKDTPendingDoc(int userId, int company, string month)
        {
            try
            {
                var pendingDocs = await _ItemMasterService.GetBKDTPendingDocAsync(userId, company, month);

                if (pendingDocs == null || !pendingDocs.Any())
                {
                    _Itemmasterlogger.LogInformation("No pending BKDT documents found");
                    return NotFound(new { Success = false, Message = "No pending BKDT documents found" });
                }

                var transformed = await TransformDocsAsync(pendingDocs, company);

                _Itemmasterlogger.LogInformation("Pending BKDT documents retrieved successfully.");
                return Ok(new { Success = true, Data = transformed });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving pending BKDT documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBKDTApprovedDoc")]
        public async Task<ActionResult> GetBKDTApprovedDoc(int userId, int company, string month)
        {
            try
            {
                var approvedDocs = await _ItemMasterService.GetBKDTApprovedDocAsync(userId, company, month);
                if (approvedDocs == null || !approvedDocs.Any())
                {
                    _Itemmasterlogger.LogInformation("No approved BKDT documents found");
                    return NotFound(new { Success = false, Message = "No approved BKDT documents found" });
                }

                var transformed = await TransformDocsAsync(approvedDocs, company);

                _Itemmasterlogger.LogInformation("Approved BKDT documents retrieved successfully.");
                return Ok(new { Success = true, Data = transformed });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving approved BKDT documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBKDTRejectedDoc")]
        public async Task<ActionResult> GetBKDTRejectedDoc(int userId, int company, string month)
        {
            try
            {
                var rejectedDocs = await _ItemMasterService.GetBKDTRejectedDocAsync(userId, company, month);
                if (rejectedDocs == null || !rejectedDocs.Any())
                {
                    _Itemmasterlogger.LogInformation("No rejected BKDT documents found");
                    return NotFound(new { Success = false, Message = "No rejected BKDT documents found" });
                }

                var transformed = await TransformDocsAsync(rejectedDocs, company);

                _Itemmasterlogger.LogInformation("Rejected BKDT documents retrieved successfully.");
                return Ok(new { Success = true, Data = transformed });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving rejected BKDT documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBKDTFullDetails")]
        public async Task<ActionResult> GetBKDTFullDetails(int userId, int company, string month)
        {
            try
            {
                var bkdtDetails = await _ItemMasterService.GetBKDTFullDetailsAsync(userId, company, month);
                if (bkdtDetails == null || !bkdtDetails.Any())
                {
                    _Itemmasterlogger.LogInformation("No BKDT details found for the given Details");
                    return NotFound(new { Success = false, Message = "No BKDT details found for the given Details" });
                }

                var transformed = await TransformDocsAsync(bkdtDetails, company);

                _Itemmasterlogger.LogInformation("BKDT details retrieved successfully.");
                return Ok(new { Success = true, Data = transformed });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving BKDT details.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        private async Task<List<BKDTGetDocumentsModels>> TransformDocsAsync(IEnumerable<BKDTGetDocumentsModels> docs, int company)
        {
            // ✅ Get document type list from service
            var mobjDetails = await _ItemMasterService.GetMobjDetailsAsync(company);
            var docTypeMap = mobjDetails?.ToDictionary(d => d.ObjType, d => d.ObjName)
                             ?? new Dictionary<int, string>();

            // Branch & Action maps
            var branchMap = new Dictionary<string, string>
            {
                { "1", "OIL" },
                { "2", "BEVERAGES" },
                { "3", "MART" }
            };

            var actionMap = new Dictionary<string, string>
            {
                { "A", "ADD" },
                { "U", "UPDATE" }
            };

            return docs.Select(doc => new BKDTGetDocumentsModels
            {
                id = doc.id,
                companyId = doc.companyId,

                // Map documentType using objType -> objName
                documentType = int.TryParse(doc.documentType, out var objType) &&
                               docTypeMap.TryGetValue(objType, out var objName)
                               ? objName
                               : doc.documentType,

                // Map branch (handles "1,2")
                branch = string.Join(", ", doc.branch.Split(',')
                                    .Select(b => branchMap.ContainsKey(b.Trim()) ? branchMap[b.Trim()] : b)),

                // Map action (handles "A,U")
                action = string.Join(", ", doc.action.Split(',')
                                    .Select(a => actionMap.ContainsKey(a.Trim()) ? actionMap[a.Trim()] : a)),

                username = doc.username,
                fromDate = doc.fromDate,
                toDate = doc.toDate,
                timeLimit = doc.timeLimit,
                createdById = doc.createdById,
                createdBy = doc.createdBy,
                createdOn = doc.createdOn,
                flowId = doc.flowId,
                Status = doc.Status,
                HanaStatus = doc.HanaStatus,
            }).ToList();
        }


        [HttpGet("GetBKDTDocumentDetail")]
        public async Task<ActionResult> GetBKDTDocumentDetail(int documentId)
        {
            try
            {
                var bkdtDocDetails = await _ItemMasterService.GetBKDTDocumentDetailAsync(documentId);
                if (bkdtDocDetails == null || !bkdtDocDetails.Any())
                {
                    _Itemmasterlogger.LogInformation("No BKDT document details found for the given DocEntry");
                    return NotFound(new { Success = false, Message = "No BKDT document details found for the given DocEntry" });
                }

                // ✅ get companyId from first record (all details belong to same company)
                int companyId = bkdtDocDetails.First().companyId;

                // ✅ fetch Mobj details for documentType mapping
                var mobjDetails = await _ItemMasterService.GetMobjDetailsAsync(companyId);
                var docTypeMap = mobjDetails?.ToDictionary(d => d.ObjType, d => d.ObjName)
                                 ?? new Dictionary<int, string>();

                // branch & action maps
                var branchMap = new Dictionary<string, string>
                {
                    { "1", "OIL" },
                    { "2", "BEVERAGES" },
                    { "3", "MART" }
                };

                var actionMap = new Dictionary<string, string>
                {
                    { "A", "ADD" },
                    { "U", "UPDATE" }
                };

                // ✅ transform
                var transformed = bkdtDocDetails.Select(doc => new BKDTDocumentDetailModels
                {
                    id = doc.id,
                    companyId = doc.companyId,

                    branch = string.Join(", ", doc.branch.Split(',')
                                    .Select(b => branchMap.ContainsKey(b.Trim()) ? branchMap[b.Trim()] : b)),

                    username = doc.username,

                    documentType = int.TryParse(doc.documentType, out var objType) &&
                                   docTypeMap.TryGetValue(objType, out var objName)
                                   ? objName
                                   : doc.documentType,

                    fromDate = doc.fromDate,
                    toDate = doc.toDate,
                    timeLimit = doc.timeLimit,
                    createdOn = doc.createdOn,

                    action = string.Join(", ", doc.action.Split(',')
                                    .Select(a => actionMap.ContainsKey(a.Trim()) ? actionMap[a.Trim()] : a)),

                    createdBy = doc.createdBy,
                    createdByUser = doc.createdByUser,
                    createdByUserId = doc.createdByUserId,
                    flowId = doc.flowId
                }).ToList();

                _Itemmasterlogger.LogInformation("BKDT document details retrieved successfully.");
                return Ok(new { Success = true, Data = transformed });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving BKDT document details.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBKDTDocumentDetailUsingFlowId")]
        public async Task<ActionResult> GetBKDTDocumentDetailUsingFlowId(int flowId)
        {
            try
            {
                var bkdtDocDetails = await _ItemMasterService.GetBKDTDocumentDetailBasedOnFlowIdAsync(flowId);
                if (bkdtDocDetails == null || !bkdtDocDetails.Any())
                {
                    _Itemmasterlogger.LogInformation("No BKDT document details found for the given DocEntry");
                    return NotFound(new { Success = false, Message = "No BKDT document details found for the given DocEntry" });
                }

                // ✅ get companyId from first record (all details belong to same company)
                int companyId = bkdtDocDetails.First().companyId;

                // ✅ fetch Mobj details for documentType mapping
                var mobjDetails = await _ItemMasterService.GetMobjDetailsAsync(companyId);
                var docTypeMap = mobjDetails?.ToDictionary(d => d.ObjType, d => d.ObjName)
                                 ?? new Dictionary<int, string>();

                // branch & action maps
                var branchMap = new Dictionary<string, string>
        {
            { "1", "OIL" },
            { "2", "BEVERAGES" },
            { "3", "MART" }
        };

                var actionMap = new Dictionary<string, string>
        {
            { "A", "ADD" },
            { "U", "UPDATE" }
        };

                // ✅ transform
                var transformed = bkdtDocDetails.Select(doc => new BKDTDocumentDetailModels
                {
                    id = doc.id,
                    companyId = doc.companyId,

                    branch = string.Join(", ", doc.branch.Split(',')
                                    .Select(b => branchMap.ContainsKey(b.Trim()) ? branchMap[b.Trim()] : b)),

                    username = doc.username,

                    documentType = int.TryParse(doc.documentType, out var objType) &&
                                   docTypeMap.TryGetValue(objType, out var objName)
                                   ? objName
                                   : doc.documentType,

                    fromDate = doc.fromDate,
                    toDate = doc.toDate,
                    timeLimit = doc.timeLimit,
                    createdOn = doc.createdOn,

                    action = string.Join(", ", doc.action.Split(',')
                                    .Select(a => actionMap.ContainsKey(a.Trim()) ? actionMap[a.Trim()] : a)),

                    createdBy = doc.createdBy,
                    createdByUser = doc.createdByUser,
                    createdByUserId = doc.createdByUserId
                }).ToList();

                _Itemmasterlogger.LogInformation("BKDT document details retrieved successfully.");
                return Ok(new { Success = true, Data = transformed });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving BKDT document details.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("CreateDocument")]
        public async Task<ActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.CreateDocumentAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to create document");
                    return NotFound(new { Success = false, Message = "Failed to create document" });
                }
                _Itemmasterlogger.LogInformation("Document created successfully.");
                return Ok(result);
            }
            catch (SqlException ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred in database while creating document.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while creating document.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }

        }

        [HttpPost("ApproveBKDT")]
        public async Task<ActionResult> ApproveBKDT([FromBody] ApproveRequestModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }

                // Step 1: Approve Document
                var result = await _ItemMasterService.ApproveDocumentAsync(request);
                if (result == null || !result.Success)
                {
                    _Itemmasterlogger.LogInformation("Failed to approve BKDT document");
                    return NotFound(new { Success = false, Message = "Failed to approve BKDT document" });
                }

                _Itemmasterlogger.LogInformation("BKDT document approved successfully.");

                // Step 2: Get Flow Status
                var flowStatus = (await _ItemMasterService.GetFlowStatusAsync(request.flowId))?.FirstOrDefault();
                if (flowStatus == null)
                {
                    _Itemmasterlogger.LogInformation($"No flow status found for FlowId {request.flowId}");
                    return NotFound(new
                    {
                        Success = false,
                        Message = "No flow status found for the given Flow ID",
                        ApprovalResult = result
                    });
                }

                object backDateResult = null;
                string hanaStatusText = "HANA not triggered";

                // Step 3: If Status is "A" → Run BackDateSaveInHana
                if (flowStatus.Status == "A")
                {
                    var backDateSaveResult = await BackDateSaveInHana(request.flowId);

                    if (backDateSaveResult is ObjectResult objectResult)
                    {
                        backDateResult = objectResult.Value;

                        if (objectResult.StatusCode == 400 || objectResult.StatusCode == 500)
                        {
                            return StatusCode(objectResult.StatusCode.Value, new
                            {
                                Success = false,
                                Message = "Error in BackDateSaveInHana",
                                ApprovalResult = result,
                                FlowStatus = flowStatus,
                                BackDateResult = backDateResult,
                                HanaStatusText = hanaStatusText
                            });
                        }
                    }
                    else
                    {
                        backDateResult = "Unexpected BackDateSaveInHana result";
                    }
                }

                // Step 4: Return combined result
                return Ok(new
                {
                    Success = true,
                    FlowId = request.flowId,
                    Message = "BKDT document processed.",
                    ApprovalResult = result,
                    FlowStatus = flowStatus,
                    BackDateResult = backDateResult,
                    HanaStatusText = hanaStatusText
                });
            }
            catch (SqlException ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred in database while approving BKDT document.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while approving BKDT document.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("RejectBKDT")]
        public async Task<ActionResult> RejectBKDT([FromBody] RejectRequestModel request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.RejectDocumentAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to reject BKDT document");
                    return NotFound(new { Success = false, Message = "Failed to reject BKDT document" });
                }
                _Itemmasterlogger.LogInformation("BKDT document rejected successfully.");
                return Ok(result);
            }
            catch (SqlException ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred in database while rejecting BKDT document.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while rejecting BKDT document.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }


        [HttpGet("GetBackDateApprovalFlow")]
        public async Task<ActionResult> GetBackDateApprovalFlow(int flowId)
        {
            try
            {
                var approvalFlow = await _ItemMasterService.GetBackDateApprovalFlowAsync(flowId);
                if (approvalFlow == null || !approvalFlow.Any())
                {
                    _Itemmasterlogger.LogInformation("No BackDate approval flow found");
                    return NotFound(new { Success = false, Message = "No BackDate approval flow found" });
                }
                _Itemmasterlogger.LogInformation("BackDate approval flow retrieved successfully.");
                return Ok(new { Success = true, Data = approvalFlow });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving BackDate approval flow.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetUserDocumentInsights")]
        public async Task<ActionResult> GetUserDocumentInsights(string createdBy, string month)
        {
            try
            {
                var insights = await _ItemMasterService.GetUserDocumentInsightsAsync(createdBy, month);
                if (insights == null)
                {
                    _Itemmasterlogger.LogInformation("No user document insights found");
                    return NotFound(new { Success = false, Message = "No user document insights found" });
                }
                _Itemmasterlogger.LogInformation("User document insights retrieved successfully.");
                return Ok(new { Success = true, Data = insights });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving user document insights.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        private async Task<List<UserDocumentsByCreatedByAndMonthModel>> TransformDocsAsync(IEnumerable<UserDocumentsByCreatedByAndMonthModel> docs, int company)
        {
            // ✅ Get document type list from service
            var mobjDetails = await _ItemMasterService.GetMobjDetailsAsync(company);
            var docTypeMap = mobjDetails?.ToDictionary(d => d.ObjType, d => d.ObjName)
                             ?? new Dictionary<int, string>();

            var branchMap = new Dictionary<string, string>
            {
                { "1", "OIL" },
                { "2", "BEVERAGES" },
                { "3", "MART" }
            };

            var actionMap = new Dictionary<string, string>
            {
                { "A", "ADD" },
                { "U", "UPDATE" }
            };

            return docs.Select(doc => new UserDocumentsByCreatedByAndMonthModel
            {
                id = doc.id,
                companyId = doc.companyId,

                documentType = int.TryParse(doc.documentType, out var objType) &&
                               docTypeMap.TryGetValue(objType, out var objName)
                               ? objName
                               : doc.documentType,

                branch = string.Join(", ", doc.branch?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => branchMap.TryGetValue(b.Trim(), out var branchName) ? branchName : b) ?? new List<string>()),

                action = string.Join(", ", doc.action?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => actionMap.TryGetValue(a.Trim(), out var actionName) ? actionName : a) ?? new List<string>()),

                username = doc.username,
                fromDate = doc.fromDate,
                toDate = doc.toDate,
                timeLimit = doc.timeLimit,
                createdById = doc.createdById,
                createdBy = doc.createdBy,
                createdOn = doc.createdOn,
                status = doc.status,
                flowId = doc.flowId,
            }).ToList();
        }

        [HttpGet("GetUserDocumentsByCreatedByAndMonth")]
        public async Task<ActionResult> GetUserDocumentsByCreatedByAndMonth(string createdBy, string monthYear, string status, int company)
        {
            try
            {
                var documents = await _ItemMasterService.GetUserDocumentsByCreatedByAndMonthAsync(createdBy, monthYear, status);
                if (documents == null)
                {
                    _Itemmasterlogger.LogInformation("No documents found for the given user and month");
                    return NotFound(new { Success = false, Message = "No documents found for the given user and month" });
                }

                var transformed = await TransformDocsAsync(documents, company);

                _Itemmasterlogger.LogInformation("Documents retrieved successfully.");
                return Ok(new { Success = true, Data = transformed });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving documents.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetFlowStatus")]
        public async Task<ActionResult> GetFlowStatus(int flowId)
        {
            try
            {
                var flowStatus = await _ItemMasterService.GetFlowStatusAsync(flowId);
                if (flowStatus == null)
                {
                    _Itemmasterlogger.LogInformation("No flow status found for the given Flow ID");
                    return NotFound(new { Success = false, Message = "No flow status found for the given Flow ID" });
                }
                _Itemmasterlogger.LogInformation("Flow status retrieved successfully.");
                return Ok(new { Success = true, Data = flowStatus });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while retrieving flow status.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("UpdateHanaStatus")]
        public async Task<IActionResult> UpdateHanaStatus([FromBody] UpdateHanaStatusRequest request)
        {
            try
            {
                if (request == null)
                {
                    _Itemmasterlogger.LogWarning("Request is null");
                    return BadRequest(new { Success = false, Message = "Invalid request" });
                }
                var result = await _ItemMasterService.UpdateHanaStatusAsync(request);
                if (result == null)
                {
                    _Itemmasterlogger.LogInformation("Failed to update Hana status");
                    return NotFound(new { Success = false, Message = "Failed to update Hana status" });
                }
                _Itemmasterlogger.LogInformation("Hana status updated successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred while updating Hana status.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        private bool TryParseDate(string dateString, out DateTime result)
        {
            result = default(DateTime);

            if (string.IsNullOrWhiteSpace(dateString))
                return false;

            // Try different date formats commonly used
            string[] formats = {
        "MM/dd/yyyy HH:mm:ss",
        "MM/dd/yyyy",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy",
        "dd-MM-yyyy HH:mm:ss",
        "dd-MM-yyyy"
    };

            // First try with current culture
            if (DateTime.TryParse(dateString, out result))
                return true;

            // Try with US culture (for MM/dd/yyyy format)
            var usCulture = new CultureInfo("en-US");
            if (DateTime.TryParse(dateString, usCulture, DateTimeStyles.None, out result))
                return true;

            // Try specific formats
            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            return false;
        }

        [HttpPost("BackDateSaveInHana")]
        public async Task<IActionResult> BackDateSaveInHana(int flowId)
        {
            try
            {
                // 1. Get BKDT Document Detail (using same flowId)
                var bkdtDocDetails = await _ItemMasterService.GetBKDTDocumentDetailBasedOnFlowIdAsync(flowId);
                if (bkdtDocDetails == null || !bkdtDocDetails.Any())
                {
                    _Itemmasterlogger.LogInformation($"No BKDT document details found for FlowId {flowId}");
                    return NotFound(new { Success = false, Message = "No BKDT document details found." });
                }

                string hanaStatusText = "BKDT processed successfully."; // default message

                // 2. Save into HANA for each document detail
                foreach (var doc in bkdtDocDetails)
                {
                    try
                    {
                        if (!TryParseDate(doc.fromDate, out DateTime parsedFromDate))
                        {
                            hanaStatusText = $"Invalid fromDate format: {doc.fromDate}";
                            return BadRequest(new { Success = false, Message = hanaStatusText });
                        }

                        if (!TryParseDate(doc.toDate, out DateTime parsedToDate))
                        {
                            hanaStatusText = $"Invalid toDate format: {doc.toDate}";
                            return BadRequest(new { Success = false, Message = hanaStatusText });
                        }

                        if (!int.TryParse(doc.documentType, out int transType))
                        {
                            hanaStatusText = $"Invalid documentType format: {doc.documentType}";
                            return BadRequest(new { Success = false, Message = hanaStatusText });
                        }

                        DateTime? timeLimitDateTime = null;
                        if (!string.IsNullOrEmpty(doc.timeLimit) &&
                            TryParseDate(doc.timeLimit, out DateTime parsedTimeLimit))
                        {
                            timeLimitDateTime = parsedTimeLimit;
                        }

                        DateTime? createdOnDateTime = null;
                        if (!string.IsNullOrEmpty(doc.createdOn) &&
                            TryParseDate(doc.createdOn, out DateTime parsedCreatedOn))
                        {
                            createdOnDateTime = parsedCreatedOn;
                        }

                        var bkdtModel = new BKDTModel
                        {
                            Branch = doc.branch,
                            UserId = doc.username,
                            TransType = transType,
                            FromDate = parsedFromDate.ToString("dd-MM-yyyy"),
                            ToDate = parsedToDate.ToString("dd-MM-yyyy"),
                            TimeLimit = timeLimitDateTime ?? DateTime.MinValue,
                            Rights = "NO",
                            CreatedBy = doc.createdBy,
                            CreatedOn = createdOnDateTime ?? DateTime.MinValue,
                            DeletedBy = null,
                            DeletedOn = DateTime.MinValue
                        };

                        var saveResult = await _ItemMasterService.SaveBKDTAsync(bkdtModel);
                        if (!saveResult.Success)
                        {
                            hanaStatusText = saveResult.Message; // capture error
                            _Itemmasterlogger.LogWarning($"Failed to save BKDT for Flow {flowId}: {saveResult.Message}");
                            // continue loop or break? (here we stop and return)
                            return BadRequest(new { Success = false, Message = hanaStatusText });
                        }

                        hanaStatusText = saveResult.Message; // capture success
                    }
                    catch (Exception ex)
                    {
                        hanaStatusText = $"Error processing document: {ex.Message}";
                        _Itemmasterlogger.LogError(ex, $"Error processing document detail for Flow {flowId}");
                        return BadRequest(new { Success = false, Message = hanaStatusText });
                    }
                }

                // 3. Update Hana Status in DB with hanaStatusText
                var updateReq = new UpdateHanaStatusRequest
                {
                    FlowId = flowId,
                    Status = true,
                    hanastatusText = hanaStatusText
                };

                var updateResult = await _ItemMasterService.UpdateHanaStatusAsync(updateReq);
                if (updateResult == null || !updateResult.Success)
                {
                    _Itemmasterlogger.LogWarning($"BKDT saved but failed to update Hana status for Flow {flowId}.");
                    return BadRequest(new { Success = false, Message = "BKDT saved but failed to update Hana status.", HanaStatusText = hanaStatusText });
                }

                _Itemmasterlogger.LogInformation($"BKDT saved and Hana status updated successfully for Flow {flowId}.");
                return Ok(new
                {
                    Success = true,
                    FlowId = flowId,
                    Message = "BKDT saved and Hana status updated successfully.",
                    HanaStatusText = hanaStatusText
                });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error occurred in BackDateSaveInHana.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetBkdtUserIdsSendNotificatios")]
        public async Task<IActionResult> GetBkdtUserIdsSendNotificatios(int flowId)
        {
            try
            {
                var result = await _ItemMasterService.GetBkdtUserIdsSendNotificatiosAsync(flowId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error fetching Item Count for User.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("SendPendingBkdtCountNotification")]
        public async Task<IActionResult> SendPendingBkdtCountNotification()
        {
            try
            {
                var result = await _ItemMasterService.SendPendingBkdtCountNotificationAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error sending Pending BKDT Count Notification.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetBKDTCurrentUsersSendNotification")]
        public async Task<IActionResult> GetBKDTCurrentUsersSendNotification(int userDocumentId)
        {
            try
            {
                var result = await _ItemMasterService.GetBKDTCurrentUsersSendNotificationAsync(userDocumentId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error sending Pending BKDT Notification.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // --------------------------- BKDT end --------------------------------

        [HttpGet("GetDistinctItemNamesSQL")]
        public async Task<IActionResult> GetDistinctItemNamesSQL()
        {
            try
            {
                var result = await _ItemMasterService.GetDistinctItemNameSqlAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error fetching SQL Distinct Item Name.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetMergedDistinctItemNames")]
        public async Task<IActionResult> GetMergedDistinctItemNames(int company)
        {
            try
            {
                var hanaTask = _ItemMasterService.GetDistinctItemNameAsync(company);
                var sqlTask = _ItemMasterService.GetDistinctItemNameSqlAsync();

                await Task.WhenAll(hanaTask, sqlTask);

                var merged = hanaTask.Result
                    .Concat(sqlTask.Result)
                    .Where(x => !string.IsNullOrWhiteSpace(x.itemName))
                    .GroupBy(x => x.itemName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                return Ok(new { Success = true, Data = merged });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error merging Distinct Item Names.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetItemByUserId")]
        public async Task<IActionResult> GetItemByUserId(int userId, int company, string month)
        {
            try
            {
                var result = await _ItemMasterService.GetItemByIdAsync(userId, company, month);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error fetching  Item .");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetItemUserIdsSendNotificatios")]
        public async Task<IActionResult> GetItemUserIdsSendNotificatios(int flowId)
        {
            try
            {
                var result = await _ItemMasterService.GetItemUserIdsSendNotificatiosAsync(flowId);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error fetching  Item User Ids for Notifications.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("SendPendingItemCountNotification")]
        public async Task<IActionResult> SendPendingItemCountNotification()
        {
            try
            {
                var result = await _ItemMasterService.SendPendingItemCountNotificationAsync();
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error sending Pending Item Count Notification.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("GetItemCurrentUsersSendNotification")]
        public async Task<IActionResult> GetItemCurrentUsersSendNotification(int initID)
        {
            try
            {
                var result = await _ItemMasterService.GetItemCurrentUsersSendNotificationAsync(initID);
                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _Itemmasterlogger.LogError(ex, "Error sending Item  Notification.");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
    }
}

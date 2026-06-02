using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryAuditController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IInventoryAuditService _inventoryService;
        private readonly ILogger<InventoryAuditController> _inventorylogger;

        public InventoryAuditController(IConfiguration configuration, IInventoryAuditService InventoryAuditService, ILogger<InventoryAuditController> Inventorylogger)
        {
            _configuration = configuration;
            _inventoryService = InventoryAuditService;
            _inventorylogger = Inventorylogger;
        }

        [HttpPost("GetItemStockDetails")]
        public async Task<ActionResult> GetItemStockDetails(InventoryAuditParamModels model)
        {
            try
            {
                var Result = await _inventoryService.GetInventoryAuditAsync(model);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Data retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetUnits")]
        public async Task<ActionResult> GetUnits(int company)
        {
            try
            {
                var Result = await _inventoryService.GetUnitAsync(company);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Units retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetSubGroups")]
        public async Task<ActionResult> GetSubGroups(int company)
        {
            try
            {
                var Result = await _inventoryService.GetSubGroupAsync(company);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Sub Groups retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetLocation")]
        public async Task<ActionResult> GetLocation(string Unit, int company)
        {
            try
            {
                var Result = await _inventoryService.GetLocationAsync(Unit, company);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Location retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetWarehouse")]
        public async Task<ActionResult> GetWarehouse(int LocationCode, int company)
        {
            try
            {
                var Result = await _inventoryService.GetWarehouseAsync(LocationCode, company);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Warehouse retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetOITMitems")]
        public async Task<ActionResult> GetOITMitems(int company)
        {
            try
            {
                var Result = await _inventoryService.GetOITMitemsAsync(company);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("HSN retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("InsertStockCountDataBulk")]
        public async Task<IActionResult> InsertStockCountDataBulk([FromBody] InsertStockCountDataBulkRequest request)
        {
            if (request == null)
                return BadRequest(new ApiResponse { Success = false, Message = "Request cannot be null." });

            if (string.IsNullOrWhiteSpace(request.LotNumber))
                return BadRequest(new ApiResponse { Success = false, Message = "LotNumber is required." });

            if (string.IsNullOrWhiteSpace(request.Unit))
                return BadRequest(new ApiResponse { Success = false, Message = "Unit is required." });

            if (string.IsNullOrWhiteSpace(request.LocationCode))
                return BadRequest(new ApiResponse { Success = false, Message = "LocationCode is required." });

            if (string.IsNullOrWhiteSpace(request.LocationName))
                return BadRequest(new ApiResponse { Success = false, Message = "LocationName is required." });

            if (request.Items == null || !request.Items.Any())
                return BadRequest(new ApiResponse { Success = false, Message = "At least one item must be provided." });

            try
            {
                var response = await _inventoryService.InsertStockCountDataBulkAsync(request);
                if (response.Success)
                    return Ok(response);  // ✅ success
                else
                    return BadRequest(response);  // ❌ failure with message
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                });
            }
        }

        [HttpPost("GenerateLotNumber")]
        public async Task<IActionResult> GenerateUsername([FromBody] GenerateUsernameRequest request)
        {
            if (request == null || request.UserId <= 0)
                return BadRequest(new ApiResponse { Success = false, Message = "Invalid UserId" });

            var response = await _inventoryService.GenerateUniqueUsernameAsync(request.UserId);

            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpGet("GetLotNumber")]
        public async Task<ActionResult> GetLotNumber(string company)
        {
            try
            {
                var Result = await _inventoryService.GetLotNumber(company);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Lot Numbers retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("GetStockCountDataByFilter")]
        public async Task<ActionResult> GetStockCountDataByFilter([FromBody] StockCountRequest request)
        {
            try
            {
                var Result = await _inventoryService.GetStockCountDataByFilterAsync(request);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Stock Count Data retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (SqlException ex)
            {
                _inventorylogger.LogError(ex, "Error occurred From Database.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetItemsUsingLotNumber")]
        public async Task<ActionResult> GetItemsUsingLotNumber(string lotNumber,int UserId)
        {
            try
            {
                var Result = await _inventoryService.GetItemsUsingLotNumberAsync(lotNumber,UserId);
                if (Result == null)
                {
                    _inventorylogger.LogInformation("No Data found");
                    return NotFound(new { Success = false, Message = "No Result found" });
                }
                _inventorylogger.LogInformation("Items retrieved successfully.");
                return Ok(new { Success = true, Data = Result });
            }
            catch (SqlException ex)
            {
                _inventorylogger.LogError(ex, "Error occurred From Database.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while retrieving Result.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("UpdatePhysicalCount")]
        public async Task<IActionResult> UpdatePhysicalCount([FromBody] UpdatePhysicalCountDto dto)
        {
            if (dto == null)
                return BadRequest(new ApiResponse { Success = false, Message = "Request cannot be null." });
            if (dto.ItemId == 0)
                return BadRequest(new ApiResponse { Success = false, Message = "ItemId is required." });
            try
            {
                var response = await _inventoryService.UpdatePhysicalCountAsync(dto);
                if (response.Success)
                    return Ok(response);  // ✅ success
                else
                    return BadRequest(response);  // ❌ failure with message
            }
            catch (SqlException ex)
            {
                _inventorylogger.LogError(ex, "Error occurred From Database.");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                });
            }
        }

        [HttpPost("CreateSessionWithBulkStockCount")]
        public async Task<IActionResult> CreateBulkStockCount([FromBody] CreateBulkStockCountRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request" });
            }

            try
            {
                var result = await _inventoryService.CreateSessionWithBulkStockCountAsync(request);

                if (result == null)
                {
                    return StatusCode(500, new { Success = false, Message = "Failed to create bulk stock count session" });
                }

                return Ok(new { Success = true, Data = result });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("GetUserActiveSessions")]
        public async Task<IActionResult> GetUserActiveSessions(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { Success = false, Message = "UserId is required." });

            try
            {
                var response = await _inventoryService.GetUserActiveSessionsAsync(userId);

                if (response.Sessions == null || !response.Sessions.Any())
                    return NotFound(new { Success = false, Message = $"No active sessions found for user {userId}" });

                return Ok(new { Success = true, Data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        
        [HttpGet("GetUserInactiveSessions")]
        public async Task<IActionResult> GetUserInactiveSessions(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { Success = false, Message = "UserId is required." });

            try
            {
                var response = await _inventoryService.GetUserInactiveSessionsAsync(userId);

                if (response.Sessions == null || !response.Sessions.Any())
                    return NotFound(new { Success = false, Message = $"No inactive sessions found for user {userId}" });

                return Ok(new { Success = true, Data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("DeactivateSession")]
        public async Task<IActionResult> DeactivateSession([FromBody] DeactivateSessionRequest request)
        {
            if (request.SessionId <= 0)
                return BadRequest(new { Success = false, Message = "SessionId is required." });

            if (request.DeactivatedBy <= 0)
                return BadRequest(new { Success = false, Message = "DeactivatedBy is required." });

            try
            {
                var response = await _inventoryService.DeactivateSessionAsync(request);

                if (response == null)
                    return NotFound(new { Success = false, Message = $"Session {request.SessionId} not found or already inactive." });

                return Ok(new { Success = true, Data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("InsertItems")]
        public async Task<IActionResult> InsertStockCountItems([FromBody] InsertStockCountItemsRequest request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
                return BadRequest(new { Success = false, Message = "Invalid request" });

            try
            {
                var result = await _inventoryService.InsertStockCountItemsAsync(request);
                return Ok(new { Success = true, Message = "Successfully Inserted" });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Success = false, Message = "Database error", Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Unexpected error", Error = ex.Message });
            }
        }

        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateUserSessionStatusRequest request)
        {
            if (request.SessionId == 0)
                return BadRequest(new { Success = false, Message = "Invalid request SessionId" });

            try
            {
                var result = await _inventoryService.UpdateUserSessionStatusAsync(request);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Success = false, Message = "Database error", Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Unexpected error", Error = ex.Message });
            }
        }

        [HttpPost("InsertSingleStockItem")]
        public async Task<ActionResult> InsertSingleStockCountItem(InsertSingleStockCountItemRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.LotNumber))
                return BadRequest(new { Success = false, Message = "Invalid request payload" });

            try
            {
                var data = await _inventoryService.InsertSingleStockCountItemAsync(model);
                return Ok(new { Success = true, Message = "Successfully Inserted" });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Success = false, Message = "Database error", Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Unexpected error", Error = ex.Message });
            }
        }

        [HttpPost("DeactivateSessionIfAllUsersInactive")]
        public async Task<IActionResult> DeactivateSessionIfAllUsersInactive(int SessionId)
        {
            if (SessionId <= 0)
                return BadRequest(new { Success = false, Message = "SessionId is required." });
            try
            {
                var response = await _inventoryService.DeactivateSessionIfAllUsersInactiveAsync(SessionId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetStockCountReportByLot")]
        public async Task<IActionResult> GetStockCountReportByLot(string lotNumber)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
                return BadRequest(new { Success = false, Message = "LotNumber is required" });

            try
            {
                var reportData = await _inventoryService.GetStockCountReportByLotAsync(lotNumber);
                return Ok(new { Success = true, Data = reportData });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new 
                {
                    Success = false, 
                    Message = "Database error", 
                    Error = ex.Message 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error fetching stock count report",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("GetActiveSessionsByUser")]
        public async Task<IActionResult> GetActiveSessionsByUser(int createdBy)
        {
            if (createdBy <= 0)
                return BadRequest(new { Success = false, Message = "createdBy is required." });
            try
            {
                var sessions = await _inventoryService.GetActiveSessionsByUserAsync(createdBy);
                return Ok(new { Success = true, Data = sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetInActiveSessionsByUser")]
        public async Task<IActionResult> GetInActiveSessionsByUser(int createdBy)
        {
            if (createdBy <= 0)
                return BadRequest(new { Success = false, Message = "createdBy is required." });
            try
            {
                var sessions = await _inventoryService.GetInActiveSessionsByUserAsync(createdBy);
                return Ok(new { Success = true, Data = sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetSessionUsers")]
        public async Task<IActionResult> GetSessionUsers(int sessionId)
        {
            if (sessionId <= 0)
                return BadRequest(new { Success = false, Message = "sessionId is required." });
            try
            {
                var users = await _inventoryService.GetSessionUsersAsync(sessionId);
                return Ok(new { Success = true, Data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("DownloadInventoryReport")]
        public async Task<ActionResult> DownloadInventoryReport(string lotNumber, int userId)
        {
            try
            {
                // Step 1: Get items using lot number
                var itemsResult = await _inventoryService.GetItemsUsingLotNumberAsync(lotNumber, userId);

                if (itemsResult == null)
                {
                    _inventorylogger.LogInformation("No Data found for lot number");
                    return NotFound(new { Success = false, Message = "No data found for this lot number" });
                }

                // Step 2: Extract and build parameters for GetItemStockDetails
                var parameters = BuildInventoryParameters(itemsResult);

                // Step 3: Call GetItemStockDetails
                var stockDetailsResult = await _inventoryService.GetInventoryAuditAsync(parameters);

                // ✅ ADD LOGGING HERE
                _inventorylogger.LogInformation($"=== DEBUGGING ===");
                _inventorylogger.LogInformation($"stockDetailsResult Type: {stockDetailsResult?.GetType().Name}");
                _inventorylogger.LogInformation($"stockDetailsResult is null: {stockDetailsResult == null}");

                if (stockDetailsResult != null)
                {
                    // Check if it's a response object with .data property
                    try
                    {
                        var dataProperty = stockDetailsResult.GetType().GetProperty("data");
                        if (dataProperty != null)
                        {
                            var dataValue = dataProperty.GetValue(stockDetailsResult);
                            _inventorylogger.LogInformation($"stockDetailsResult.data exists, type: {dataValue?.GetType().Name}");

                            if (dataValue is IEnumerable<dynamic> list)
                            {
                                int count = 0;
                                foreach (var item in list) count++;
                                _inventorylogger.LogInformation($"stockDetailsResult.data count: {count}");
                            }
                        }
                        else
                        {
                            _inventorylogger.LogInformation("stockDetailsResult.data property does not exist");
                        }
                    }
                    catch (Exception ex)
                    {
                        _inventorylogger.LogError(ex, "Error checking stockDetailsResult structure");
                    }

                    // Try to count items if it's IEnumerable
                    try
                    {
                        if (stockDetailsResult is IEnumerable<dynamic> directList)
                        {
                            int count = 0;
                            foreach (var item in directList) count++;
                            _inventorylogger.LogInformation($"stockDetailsResult direct count: {count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _inventorylogger.LogError(ex, "Error counting stockDetailsResult");
                    }
                }

                // Step 4: Call GetStockCountReportByLot
                var stockCountReport = await _inventoryService.GetStockCountReportByLotAsync(lotNumber);

                // ✅ ADD LOGGING HERE
                _inventorylogger.LogInformation($"stockCountReport Type: {stockCountReport?.GetType().Name}");
                if (stockCountReport is IEnumerable<dynamic> countList)
                {
                    int count = 0;
                    foreach (var item in countList) count++;
                    _inventorylogger.LogInformation($"stockCountReport count: {count}");
                }

                // Step 5: Merge results with priority rules
                var mergedData = MergeReportData(itemsResult, stockDetailsResult, stockCountReport);

                _inventorylogger.LogInformation($"Final mergedData count: {mergedData.Count}");
                _inventorylogger.LogInformation("Report generated successfully");
                return Ok(new { Success = true, Data = mergedData });
            }
            catch (SqlException ex)
            {
                _inventorylogger.LogError(ex, "Database error occurred");
                return StatusCode(500, new { Success = false, Message = "Database error", Error = ex.Message });
            }
            catch (Exception ex)
            {
                _inventorylogger.LogError(ex, "Error occurred while generating report");
                return StatusCode(500, new { Success = false, Message = "Error generating report", Error = ex.Message });
            }
        }

        // Helper method to build parameters from GetItemsUsingLotNumber response
        private InventoryAuditParamModels BuildInventoryParameters(GetItemsUsingLotNumberResult itemsResult)
        {
            // Access the headers
            var header = itemsResult.Headers?.FirstOrDefault();

            if (header == null)
            {
                throw new Exception("No header data found in result");
            }

            // Extract warehouses
            var warehouses = string.Join(",",
                itemsResult.Warehouses?.Select(w => w.WarehouseName) ?? Enumerable.Empty<string>());

            // Extract item groups
            var itemGroups = string.Join(",",
                itemsResult.ItemGroups?.Select(ig => ig.ItemGroupCode.ToString()) ?? Enumerable.Empty<string>());

            // Extract sub groups
            var subGroups = string.Join(",",
                itemsResult.ItemSubGroups?.Select(sg => sg.ItemSubGroupName) ?? Enumerable.Empty<string>());

            return new InventoryAuditParamModels
            {
                p_warehouses = warehouses,
                p_units = header.Unit,
                p_itemGroups = itemGroups,
                p_subGroups = subGroups,
                p_locations = header.LocationCode.ToString(),
                p_itemCodes = "",
                company = int.Parse(header.Company)
            };
        }

        private List<object> MergeReportData(GetItemsUsingLotNumberResult itemsResult,dynamic stockDetailsResult,dynamic stockCountReport)
        {
            var result = new List<object>();
            var header = itemsResult.Headers?.FirstOrDefault();

            if (header == null)
            {
                _inventorylogger.LogWarning("No header data found");
                return result;
            }

            _inventorylogger.LogInformation("=== MergeReportData Debug Start ===");

            // Create lookup dictionaries
            var stockCountDict = new Dictionary<string, dynamic>();
            var stockDetailsDict = new Dictionary<string, dynamic>();

            // Process stockCountReport
            if (stockCountReport != null)
            {
                _inventorylogger.LogInformation($"stockCountReport Type: {stockCountReport.GetType().FullName}");

                int scCount = 0;
                foreach (var item in stockCountReport)
                {
                    scCount++;
                    string key = $"{item.ItemCode}_{item.Warehouse}";
                    if (!stockCountDict.ContainsKey(key))
                    {
                        stockCountDict[key] = item;
                    }
                }
                _inventorylogger.LogInformation($"stockCountReport: Processed {scCount} items, Dictionary has {stockCountDict.Count} unique items");
            }
            else
            {
                _inventorylogger.LogWarning("stockCountReport is NULL");
            }

            // Process stockDetailsResult
            if (stockDetailsResult != null)
            {
                _inventorylogger.LogInformation($"stockDetailsResult Type: {stockDetailsResult.GetType().FullName}");

                // Try to get count
                try
                {
                    var enumerable = stockDetailsResult as IEnumerable;
                    if (enumerable != null)
                    {
                        int tempCount = 0;
                        foreach (var item in enumerable)
                        {
                            tempCount++;
                        }
                        _inventorylogger.LogInformation($"stockDetailsResult: Total items in enumerable: {tempCount}");
                    }
                }
                catch (Exception ex)
                {
                    _inventorylogger.LogError(ex, "Error counting stockDetailsResult");
                }

                // Now actually process
                int sdCount = 0;
                int sdSkipped = 0;
                int sdAdded = 0;

                try
                {
                    foreach (var item in stockDetailsResult)
                    {
                        sdCount++;

                        try
                        {
                            string itemCode = item.ItemCode?.ToString() ?? item.itemCode?.ToString() ?? "";
                            string warehouse = item.Warehouse?.ToString() ?? item.warehouse?.ToString() ?? "";

                            if (string.IsNullOrEmpty(itemCode) || string.IsNullOrEmpty(warehouse))
                            {
                                sdSkipped++;
                                if (sdSkipped <= 5) // Log first 5 only
                                {
                                    _inventorylogger.LogWarning($"Item {sdCount}: Missing itemCode or warehouse");
                                }
                                continue;
                            }

                            string key = $"{itemCode}_{warehouse}";

                            if (!stockDetailsDict.ContainsKey(key))
                            {
                                stockDetailsDict[key] = item;
                                sdAdded++;

                                // Log first 3 items for debugging
                                if (sdAdded <= 3)
                                {
                                    _inventorylogger.LogInformation($"Added item {sdAdded}: {itemCode} - {warehouse}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _inventorylogger.LogError(ex, $"Error processing item {sdCount}");
                        }
                    }

                    _inventorylogger.LogInformation($"stockDetailsResult: Processed {sdCount} items, Skipped {sdSkipped}, Added {sdAdded} to dictionary");
                }
                catch (Exception ex)
                {
                    _inventorylogger.LogError(ex, "Error iterating stockDetailsResult");
                }
            }
            else
            {
                _inventorylogger.LogWarning("stockDetailsResult is NULL");
            }

            _inventorylogger.LogInformation($"Dictionaries ready: stockCount={stockCountDict.Count}, stockDetails={stockDetailsDict.Count}");

            var processedKeys = new HashSet<string>();
            int idCounter = 1;

            // STEP 1: Add from stockCountReport
            foreach (var kvp in stockCountDict)
            {
                string key = kvp.Key;
                var stockCountItem = kvp.Value;
                processedKeys.Add(key);

                var mergedItem = new
                {
                    id = idCounter++,
                    sessionId = (int)stockCountItem.SessionId,
                    lotNumber = (string)stockCountItem.LotNumber,
                    warehouse = (string)stockCountItem.Warehouse,
                    itemGroupCode = (string)stockCountItem.ItemGroupCode,
                    itemCode = (string)stockCountItem.ItemCode,
                    unit = (string)stockCountItem.Unit,
                    locationName = (string)stockCountItem.LocationName,
                    itemName = (string)stockCountItem.ItemName,
                    itemGroupName = (string)stockCountItem.ItemGroupName,
                    subGroupName = (string)stockCountItem.SubGroupName,
                    systemQty = (decimal)stockCountItem.SystemQty,
                    physicalQty = (decimal)stockCountItem.PhysicalQty,
                    diffQty = (decimal)stockCountItem.DiffQty,
                    totalStockValue = (decimal)stockCountItem.TotalStockValue,
                    diffValue = (decimal)stockCountItem.DiffValue,
                    diffLitre = (decimal)stockCountItem.DiffLitre,
                    salPackUn = (decimal)stockCountItem.SalPackUn,
                    isLitreFlag = (string)stockCountItem.IsLitreFlag
                };

                result.Add(mergedItem);
            }

            _inventorylogger.LogInformation($"Step 1 Complete: Added {processedKeys.Count} items from stockCountReport");

            // STEP 2: Add remaining from stockDetailsResult
            int addedFromStockDetails = 0;
            int skippedDuplicate = 0;

            foreach (var kvp in stockDetailsDict)
            {
                string key = kvp.Key;
                var item = kvp.Value;

                if (processedKeys.Contains(key))
                {
                    skippedDuplicate++;
                    continue;
                }

                processedKeys.Add(key);

                try
                {
                    decimal systemQty = Convert.ToDecimal(item.OnHand);
                    decimal stockValue = Convert.ToDecimal(item.StockValue);
                    decimal salPackUn = Convert.ToDecimal(item.SalPackUn);
                    string isLitreFlag = item.U_IsLitre?.ToString() ?? "N";

                    decimal physicalQty = 0;
                    decimal diffQty = physicalQty - systemQty;
                    decimal onHand = systemQty > 0 ? systemQty : 1;
                    decimal diffValue = (stockValue / onHand) * diffQty;
                    decimal diffLitre = 0;

                    if (isLitreFlag == "Y")
                    {
                        diffLitre = diffQty * salPackUn;
                    }

                    var newItem = new
                    {
                        id = idCounter++,
                        sessionId = header.sessionId,
                        lotNumber = header.LotNumber,
                        warehouse = item.Warehouse?.ToString() ?? "",
                        itemGroupCode = item.ItmsGrpCod?.ToString() ?? "",
                        itemCode = item.ItemCode?.ToString() ?? "",
                        unit = header.Unit,
                        locationName = item.LocationName?.ToString() ?? "",
                        itemName = item.ItemName?.ToString() ?? "",
                        itemGroupName = item.ItmsGrpNam?.ToString() ?? "",
                        subGroupName = item.U_Sub_Group?.ToString() ?? "",
                        systemQty = systemQty,
                        physicalQty = physicalQty,
                        diffQty = diffQty,
                        totalStockValue = stockValue,
                        diffValue = diffValue,
                        diffLitre = diffLitre,
                        salPackUn = salPackUn,
                        isLitreFlag = isLitreFlag
                    };

                    result.Add(newItem);
                    addedFromStockDetails++;
                }
                catch (Exception ex)
                {
                    _inventorylogger.LogError(ex, $"Error creating item for key: {key}");
                }
            }

            _inventorylogger.LogInformation($"Step 2 Complete: Added {addedFromStockDetails} items from stockDetails, Skipped {skippedDuplicate} duplicates");
            _inventorylogger.LogInformation($"=== Final Result: {result.Count} total items ===");

            return result;
        }
    }
}

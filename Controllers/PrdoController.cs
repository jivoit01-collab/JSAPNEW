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
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrdoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrdoController> _logger;
        private readonly IPrdoService _prdoService;

        public PrdoController(IConfiguration configuration, IPrdoService PrdoService, ILogger<PrdoController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _prdoService = PrdoService;
        }

        [HttpGet("GetApprovedProductionOrders")]
        public async Task<IActionResult> GetApprovedProductionOrders([FromQuery] int userId, [FromQuery] int company, string month)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { Message = "Invalid user ID" });
                }

                if (company <= 0)
                {
                    return BadRequest(new { Message = "Invalid company ID" });
                }

                _logger.LogInformation($"Retrieving approved production orders for UserId: {userId}, Company: {company}");

                var approvedOrders = await _prdoService.GetApprovedProductionOrdersAsync(userId, company, month);

                return Ok(new
                {
                    Success = true,
                    Data = approvedOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving approved production orders for UserId: {userId}, Company: {company}");
                return StatusCode(500, new PrdoModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetPendingProductionOrders")]
        public async Task<IActionResult> GetPendingProductionOrders([FromQuery] int userId, [FromQuery] int company, string month)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { Message = "Invalid user ID" });
                }

                if (company <= 0)
                {
                    return BadRequest(new { Message = "Invalid company ID" });
                }

                _logger.LogInformation($"Retrieving pending production orders for UserId: {userId}, Company: {company}");

                var pendingOrders = await _prdoService.GetPendingProductionOrdersAsync(userId, company, month);

                return Ok(new
                {
                    Success = true,
                    Data = pendingOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving pending production orders for UserId: {userId}, Company: {company}");
                return StatusCode(500, new PrdoModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetRejectedProductionOrders")]
        public async Task<IActionResult> GetRejectedProductionOrders([FromQuery] int userId, [FromQuery] int company, string month)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { Message = "Invalid user ID" });
                }
                if (company <= 0)
                {
                    return BadRequest(new { Message = "Invalid company ID" });
                }
                _logger.LogInformation($"Retrieving rejected production orders for UserId: {userId}, Company: {company}");
                var rejectedOrders = await _prdoService.GetRejectedProductionOrdersAsync(userId, company, month);
                return Ok(new
                {
                    Success = true,
                    Data = rejectedOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving rejected production orders for UserId: {userId}, Company: {company}");
                return StatusCode(500, new PrdoModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetProductionOrderInsight")]
        public async Task<IActionResult> GetProductionOrderInsight(int userId, int company, string month)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { Message = "Invalid user ID" });

                if (company <= 0)
                    return BadRequest(new { Message = "Invalid company ID" });

                _logger.LogInformation($"Retrieving data of UserId: {userId}, Company: {company}");

                var data = await _prdoService.GetProductionOrderInsightAsync(userId, company, month);

                return Ok(new { Success = true, Data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving data of UserId: {userId}, Company: {company}");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetProductionOrderInsightAll")]
        public async Task<IActionResult> GetProductionOrderInsightAll(int company, string month)
        {
            try
            {
                if (company <= 0)
                    return BadRequest(new { Message = "Invalid company ID" });

                _logger.LogInformation($"Retrieving data of Company: {company}");

                var data = await _prdoService.GetProductionOrderInsightAllAsync(company, month);

                return Ok(new { Success = true, Data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving data of Company: {company}");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }


        [HttpPost("ApproveProductionOrder")]
        public async Task<IActionResult> ApproveProductionOrder([FromBody] ProductionOrderApprovalRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _prdoService.ApproveProductionOrderAsync(request);

                if (response == null)
                {
                    _logger.LogInformation("No data found for approval");
                    return NotFound(new { Success = false, Message = "No data found for approval" });
                }
                _logger.LogInformation("Production Order approved successfully.");
                return Ok(response);

                /*if (response.Success)
                {
                    _logger.LogInformation($"Approval successful: {response.Message}");

                    return Ok(new
                    {
                        Success = true,
                        Data = response
                    });
                }
                else
                {
                    _logger.LogWarning($"Approval failed: {response.Message}");

                    return StatusCode(500, new PrdoModels
                    {
                        Success = false,
                        Message = response.Message
                    });
                }*/
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving production order {request.DocIds}");
                return StatusCode(500, new PrdoModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("RejectProductionOrder")]
        public async Task<IActionResult> RejectProductionOrder([FromBody] ProductionOrderRejectRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var response = await _prdoService.RejectProductionOrderAsync(request);
                if (response == null)
                {
                    _logger.LogInformation("No data found for rejection");
                    return NotFound(new { Success = false, Message = "No data found for rejection" });
                }
                _logger.LogInformation("Production Order rejected successfully.");
                return Ok(response);

                /* if (response.Success)
                 {
                     _logger.LogInformation($"Rejection successful: {response.Message}");
                     return Ok(new
                     {
                         Success = true,
                         Data = response
                     });
                 }
                 else
                 {
                     _logger.LogWarning($"Rejection failed: {response.Message}");
                     return StatusCode(500, new PrdoModels
                     {
                         Success = false,
                         Message = response.Message
                     });
                 }*/
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting production order {request.DocId}");
                return StatusCode(500, new PrdoModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetProductionOrderDetailById")]
        public async Task<IActionResult> GetProductionOrderDetailById(int productionOrderId, int company)
        {
            try
            {
                if (productionOrderId <= 0)
                    return BadRequest(new { Success = false, Message = "Invalid production order ID" });

                _logger.LogInformation($"Fetching production order detail for ID: {productionOrderId}");

                var result = await _prdoService.GetProductionOrderDetailByIdAsync(productionOrderId, company);

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching production order detail for ID: {productionOrderId}");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetProductionOrderApprovalFlow")]
        public async Task<IActionResult> GetProductionOrderApprovalFlow(int productionOrderId)
        {
            try
            {
                if (productionOrderId <= 0)
                    return BadRequest(new { Success = false, Message = "Invalid production order ID" });
                _logger.LogInformation($"Fetching approval flow for production order ID: {productionOrderId}");
                var result = await _prdoService.GetProductionOrderApprovalFlowAsync(productionOrderId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching approval flow for production order ID: {productionOrderId}");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetItemLocationStockModel")]
        public async Task<IActionResult> GetItemLocationStockModel(string ItemCode, string Warehouse, int company)
        {
            try
            {
                if (string.IsNullOrEmpty(ItemCode))
                    return BadRequest(new { Success = false, Message = "Invalid Item Code" });
                if (string.IsNullOrEmpty(Warehouse))
                    return BadRequest(new { Success = false, Message = "Invalid Warehouse" });
                _logger.LogInformation($"Fetching item location stock for ItemCode: {ItemCode}, Warehouse: {Warehouse}");
                var result = await _prdoService.GetItemLocationStockModelAsync(ItemCode, Warehouse, company);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching item location stock for ItemCode: {ItemCode}, Warehouse: {Warehouse}");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetAllProductionOrder")]
        public async Task<IActionResult> GetAllProductionOrder(int userId, int company, string month)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { Message = "Invalid user ID" });
                }
                if (company <= 0)
                {
                    return BadRequest(new { Message = "Invalid company ID" });
                }
                _logger.LogInformation($"Retrieving all production orders for UserId: {userId}, Company: {company}");
                var allOrders = await _prdoService.GetAllProductionOrderAsync(userId, company, month);
                return Ok(new
                {
                    Success = true,
                    Data = allOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving all production orders for UserId: {userId}, Company: {company}");
                return StatusCode(500, new PrdoModels
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("GetPrdoUserIdsSendNotificatios")]
        public async Task<IActionResult> GetPrdoUserIdsSendNotificatios(int FlowId)
        {
            try
            {
                if (FlowId <= 0)
                    return BadRequest(new { Success = false, Message = "Invalid production order ID" });
                _logger.LogInformation($"Fetching user IDs for notifications for FlowId: {FlowId}");
                var result = await _prdoService.GetProductionUserIdsSendNotificatiosAsync(FlowId);
                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching user IDs for notifications for PRDO ID: {FlowId}");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("SendPendingProductionCountNotification")]
        public async Task<IActionResult> SendPendingProductionCountNotification()
        {
            try
            {
                _logger.LogInformation("Sending pending production count notifications.");
                var response = await _prdoService.SendPendingProductionCountNotificationAsync();
                return Ok(new
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending pending production count notifications.");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}

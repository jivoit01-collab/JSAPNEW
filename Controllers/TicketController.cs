using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Models;
using System.Text.Json;
using JSAPNEW.Services.Implementation;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace TicketSystem.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpPost("assignticket")]
        public async Task<IActionResult> AssignTicket([FromBody] TicketAssignmentModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Call the service to add the comment on the ticket
            var result = await _ticketService.AssignTicketAsync(request);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("Error assign ticket.");
            }
        }

        [HttpPost("CreateTicket")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<IEnumerable<TicketResponse>>> CreateTicket()
        {
            try
            {
                var form = await Request.ReadFormAsync();

                // 1️⃣ Read main JSON data
                var requestJson = form["request"];
                if (string.IsNullOrWhiteSpace(requestJson))
                {
                    return BadRequest(new TicketResponse
                    {
                        Success = false,
                        Message = "Missing ticket data."
                    });
                }

                var model = JsonConvert.DeserializeObject<TicketCreateModel>(requestJson);

                // 2️⃣ Extract files
                var files = form.Files;
                model.Files = new List<TicketFile>();

                // 3️⃣ Folder path
                var uploadFolder = Path.Combine("wwwroot", "Uploads", "Ticket");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // 4️⃣ Save files and build attachment model
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName);
                        var newFileName = $"{Guid.NewGuid()}{ext}";
                        var savePath = Path.Combine(uploadFolder, newFileName);

                        using var stream = new FileStream(savePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        model.Files.Add(new TicketFile
                        {
                            FileName = newFileName,
                            FilePath = "/Uploads/Ticket", // relative path for DB/UI
                            FileType = ext.Replace(".", ""), // use extension as type
                                                             //FileSize = file.Length
                        });
                    }
                }

                // 5️⃣ Call service
                var result = await _ticketService.CreateTicketAsync(model, files.ToList());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new TicketResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("getuserticketsinsights")]
        public async Task<IActionResult> GetUserTicketsInsights(int userId, string month)
        {
            try
            {
                var result = await _ticketService.GetUserTicketsInsightsAsync(userId,month);
                if (result == null || !result.Any())
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "No ticket insights found for the given user.",
                        Data = new List<object>()
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "User ticket insights retrieved successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Data = new List<object>()
                });
            }
        }

        [HttpGet("getoneticketdetails")]
        public async Task<IActionResult> GetOneTicketDetails(int ticketId)
        {
            try
            {
                var result = await _ticketService.GetOneTicketDetailsAsync(ticketId);
                return Ok(new
                {
                    Success = true,
                    Message = "User ticket details retrieved successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Data = new List<object>()
                });
            }
        }

        [HttpGet("getuserticketswithstatus")]
        public async Task<IActionResult> GetUserTicketsWithStatus(int userId, string statusIds, string month)
        {
            try
            {
                var result = await _ticketService.GetUserTicketsWithStatusAsync(userId, statusIds,month);
                return Ok(new
                {
                    Success = true,
                    Message = "User ticket with status details retrieved successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Data = new List<object>()
                });
            }
        }

        [HttpPost("addcatalogone")]
        public async Task<IActionResult> AddCatalogOne([FromBody] AddCatalogOneModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Call the service to add the catalog item
            var result = await _ticketService.AddCatalogOneAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [HttpPost("addcatalogtwo")]
        public async Task<IActionResult> AddCatalogTwo([FromBody] AddCatalogTwoModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Call the service to add the catalog item
            var result = await _ticketService.AddCatalogTwoAsync(request);
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [HttpGet("getrequestname")]
        public async Task<IActionResult> GetRequestName()
        {
            try
            {
                var result = await _ticketService.GetRequestNameAsync();
                return Ok(new
                {
                    Success = true,
                    Message = "Get request name successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Data = new List<object>()
                });
            }
        }

        [HttpPost("updateticketstatus")]
        public async Task<IActionResult> UpdateTicketStatus([FromBody] updateticketstatusmodel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Call the service to update the ticket status
            var result = await _ticketService.UpdateTicketStatusAsync(request);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("Error updating ticket status.");
            }
        }

        [HttpPost("addcommentonticket")]
        public async Task<IActionResult> AddCommentOnTicket([FromBody] AddCommentOnTicketModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Call the service to add the comment on the ticket
            var result = await _ticketService.AddCommentOnTicketAsync(request);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("Error adding comment on ticket.");
            }
        }

        [HttpGet("getrequesttype")]
        public async Task<IActionResult> GetRequestType()
        {
            try
            {
                var result = await _ticketService.GetRequestTypeAsync();
                return Ok(new
                {
                    Success = true,
                    Message = "Get request type successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Data = new List<object>()
                });
            }
        }

        [HttpGet("GetTicketComments")]
        public async Task<IActionResult> GetTicketComments(int ticketId)
        {
            try
            {
                var result = await _ticketService.GetTicketCommentsAsync(ticketId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet("GetTicketAttachments")]
        public async Task<IActionResult> GetTicketAttachments(int ticketId)
        {
            try
            {
                var result = await _ticketService.GetTicketAttachmentsAsync(ticketId, Url);
                return Ok(new { success = true, data = result });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
            }
        }

        [HttpGet("GetCatalogOne")]
        public async Task<IActionResult> GetCatalogOne()
        {
            try
            {
                var result = await _ticketService.GetCatalogOneAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetCatalogTwo")]
        public async Task<IActionResult> GetCatalogTwo(int oneId)
        {
            try
            {
                var result = await _ticketService.GetCatalogTwoAsync(oneId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetOpenAndReopenTickets")]
        public async Task<IActionResult> GetOpenAndReopenTickets(string MonthYear)
        {
            try
            {
                var result = await _ticketService.GetOpenAndReopenTicketsAsync(MonthYear);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet("GetTicketInsightsByMonth")]
        public async Task<IActionResult> GetTicketInsightsByMonth([FromQuery] string monthYear, [FromQuery] int? assignerId = null)
        {
            if (string.IsNullOrEmpty(monthYear))
                return BadRequest(new { success = false, message = "MonthYear is required in format MM-YYYY" });

            try
            {
                var result = await _ticketService.GetTicketInsightsByMonthAsync(monthYear, assignerId);

                return Ok(new
                {
                    success = true,
                    message = "Ticket insights retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {

                return BadRequest(new { success = false, message = "ex.Message" });
            }
        }
        [HttpGet("GetTicketAssignedByAssignerMonth")]
        public async Task<IActionResult> GetTicketsAssignedByAssignerMonth(int assignerId, string monthYear)
        {
            if (assignerId <= 0)
                return BadRequest(new { success = false, message = "AssignerId is required" });
            if (string.IsNullOrEmpty(monthYear))
                return BadRequest(new { success = false, message = "MonthYear is required in format MM-YYYY" });

            try
            {
                var result = await _ticketService.GetTicketsAssignedByMonthAsync(assignerId, monthYear);

                return Ok(new
                {
                    success = true,
                    message = "Ticket insights retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {

                return BadRequest(new { success = false, message = "ex.Message" });
            }
        }

        [HttpGet("GetTicketDetailsByAssignee")]
        public async Task<IActionResult> GetTicketDetailsByAssignee(int assigneeId, string monthYear)
        {
            if (assigneeId <= 0)
                return BadRequest(new { success = false, message = "AssigneeId is required" });
            try
            {
                var result = await _ticketService.GetTicketDetailsByAssigneeAsync(assigneeId, monthYear);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetClosedTicketDetailsByAssignee")]
        public async Task<IActionResult> GetClosedTicketDetailsByAssignee(int assigneeId, string monthYear)
        {
            if (assigneeId <= 0)
                return BadRequest(new { success = false, message = "AssigneeId is required" });
            try
            {
                var result = await _ticketService.GetClosedTicketDetailsByAssigneeAsync(assigneeId, monthYear);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetTicketAssignmentInsights")]
        public async Task<IActionResult> GetTicketAssignmentInsights(int assigneeId, string monthYear)
        {
            if (assigneeId <= 0)
                return BadRequest(new { success = false, message = "AssigneeId is required" });
            try
            {
                var result = await _ticketService.GetTicketAssignmentInsightsAsync(assigneeId, monthYear);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpGet("GetTicketStatus")]
        public async Task<IActionResult> GetTicketStatus()
        {
            try
            {
                var result = await _ticketService.GetTicketStatusAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

    }
}
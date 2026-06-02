using System;
using System.Threading.Tasks;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketsService _ticketsService;
        private readonly INotificationService _notificationService;
        public TicketsController(ITicketsService ticketsService, INotificationService notificationService)
        {
            _ticketsService = ticketsService;
            _notificationService = notificationService;
        }

        #region Project Management

        [HttpPost("CreateProject")]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.CreateProjectAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("UpdateProject")]
        public async Task<IActionResult> UpdateProject([FromBody] UpdateProjectModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.UpdateProjectAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("DeleteProject")]
        public async Task<IActionResult> DeleteProject([FromBody] DeleteProjectModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.DeleteProjectAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetAllProjects")]
        public async Task<IActionResult> GetAllProjects([FromBody] GetAllProjectsModel model)
        {
            try
            {
                model = model ?? new GetAllProjectsModel();
                var result = await _ticketsService.GetAllProjectsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetProjectById")]
        public async Task<IActionResult> GetProjectById([FromBody] GetProjectByIdModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetProjectByIdAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        #endregion

        #region Ticket Creation

        [HttpPost("CreateTicket")]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.CreateTicketAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetMyTickets")]
        public async Task<IActionResult> GetMyTickets([FromBody] GetMyTicketsModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetMyTicketsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetTicketById")]
        public async Task<IActionResult> GetTicketById([FromBody] GetTicketByIdModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetTicketByIdAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("UpdateMyTicket")]
        public async Task<IActionResult> UpdateMyTicket([FromBody] UpdateMyTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.UpdateMyTicketAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        #endregion

        #region Ticket Assignment

        [HttpPost("GetOpenTickets")]
        public async Task<IActionResult> GetOpenTickets([FromBody] GetOpenTicketsModel model)
        {
            try
            {
                model = model ?? new GetOpenTicketsModel();
                var result = await _ticketsService.GetOpenTicketsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("AssignTicket")]
        public async Task<IActionResult> AssignTicket([FromBody] AssignTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.AssignTicketAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("ReassignTicket")]
        public async Task<IActionResult> ReassignTicket([FromBody] ReassignTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.ReassignTicketAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetAllTickets")]
        public async Task<IActionResult> GetAllTickets([FromBody] GetAllTicketsModel model)
        {
            try
            {
                model = model ?? new GetAllTicketsModel();
                var result = await _ticketsService.GetAllTicketsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpGet("GetUsersForAssignment")]
        public async Task<IActionResult> GetUsersForAssignment()
        {
            try
            {
                var result = await _ticketsService.GetUsersForAssignmentAsync();
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        #endregion

        #region Working on Tickets

        [HttpPost("GetAssignedTickets")]
        public async Task<IActionResult> GetAssignedTickets([FromBody] GetAssignedTicketsModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetAssignedTicketsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("StartTicket")]
        public async Task<IActionResult> StartTicket([FromBody] StartTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.StartTicketAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("HoldTicket")]
        public async Task<IActionResult> HoldTicket([FromBody] HoldTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.HoldTicketAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("ResumeTicket")]
        public async Task<IActionResult> ResumeTicket([FromBody] ResumeTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.ResumeTicketAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        //[HttpPost("CloseTicket")]
        //public async Task<IActionResult> CloseTicket([FromBody] CloseTicketModel model)
        //{
        //    if (model == null)
        //        return BadRequest(new { success = false, message = "Invalid request body" });

        //    try
        //    {
        //        var result = await _ticketsService.CloseTicketAsync(model);
        //        return Ok(result);

        //    }
        //    catch (SqlException ex)
        //    {
        //        return StatusCode(500, new TApiResponse
        //        {
        //            Success = false,
        //            StatusCode = 500,
        //            Message = "Database error: " + ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new TApiResponse
        //        {
        //            Success = false,
        //            StatusCode = 500,
        //            Message = "Internal server error: " + ex.Message
        //        });
        //    }
        //}

        [HttpPost("CloseTicket")]
        public async Task<IActionResult> CloseTicket([FromBody] CloseTicketModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.CloseTicketAsync(model);

                if (!result.Success)
                    return Ok(result);

                // Fetch Ticket Details after Close
                var ticketData = await _ticketsService.GetTicketByIdAsync(
                    new GetTicketByIdModel { TicketId = model.TicketId });

                if (ticketData == null || ticketData.Data == null)
                    return Ok(result); // No ticket found → skip notifications

                int fromUserId = ticketData.Data.FromUserId;
                string title = "Ticket Closed";
                string body = $"Your ticket (ID: {ticketData.Data.TicketId}) has been closed.";

                // Send notification
                await SendTicketCloseNotificationAsync(
                    fromUserId,
                    ticketData.Data.TicketId,
                    title,
                    body
                );

                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        private async Task SendTicketCloseNotificationAsync(int userId, int TicketId, string title, string message)
        {
            var fcmTokenList = await _notificationService.GetUserFcmTokenAsync(userId);

            if (fcmTokenList != null && fcmTokenList.Count > 0)
            {
                var sentTokens = new HashSet<string>();

                var data = new Dictionary<string, string>
                {
                    { "screen", "TICKET_DETAILS" },
                    { "ticketId", TicketId.ToString() }
                };

                foreach (var token in fcmTokenList)
                {
                    if (string.IsNullOrWhiteSpace(token.fcmToken))
                        continue;

                    if (sentTokens.Contains(token.fcmToken))
                        continue;

                    await _notificationService.SendPushNotificationAsync(
                        title,
                        message,
                        token.fcmToken,
                        data
                    );

                    sentTokens.Add(token.fcmToken);
                }
            }

            // Insert into Notification table
            await _notificationService.InsertNotificationAsync(new InsertNotificationModel
            {
                userId = userId,
                title = title,
                message = message,
                pageId = 6,
                data = $"Ticket ID: {TicketId}",
                BudgetId = TicketId
            });
        }


        [HttpPost("GetMyWorkloadSummary")]
        public async Task<IActionResult> GetMyWorkloadSummary([FromBody] GetMyWorkloadSummaryModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetMyWorkloadSummaryAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        #endregion

        #region Comments

        [HttpPost("AddComment")]
        public async Task<IActionResult> AddComment([FromBody] AddCommentModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.AddCommentAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("UpdateComment")]
        public async Task<IActionResult> UpdateComment([FromBody] UpdateCommentModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.UpdateCommentAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("DeleteComment")]
        public async Task<IActionResult> DeleteComment([FromBody] DeleteCommentModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.DeleteCommentAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetCommentsByTicketId")]
        public async Task<IActionResult> GetCommentsByTicketId([FromBody] GetCommentsByTicketIdModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetCommentsByTicketIdAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetCommentById")]
        public async Task<IActionResult> GetCommentById([FromBody] GetCommentByIdModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetCommentByIdAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetMyComments")]
        public async Task<IActionResult> GetMyComments([FromBody] GetMyCommentsModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetMyCommentsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        #endregion

        #region Timeline

        [HttpPost("GetTicketTimeline")]
        public async Task<IActionResult> GetTicketTimeline([FromBody] GetTicketTimelineModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request body" });

            try
            {
                var result = await _ticketsService.GetTicketTimelineAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        #endregion

        #region Insights

        [HttpPost("GetTicketRaiserInsights")]
        public async Task<IActionResult> GetTicketRaiserInsights([FromBody] GetTicketRaiserInsightsModel model)
        {
            try
            {
                model = model ?? new GetTicketRaiserInsightsModel();
                var result = await _ticketsService.GetTicketRaiserInsightsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetAssigneeInsights")]
        public async Task<IActionResult> GetAssigneeInsights([FromBody] GetAssigneeInsightsModel model)
        {
            try
            {
                model = model ?? new GetAssigneeInsightsModel();
                var result = await _ticketsService.GetAssigneeInsightsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("GetAssignerInsights")]
        public async Task<IActionResult> GetAssignerInsights([FromBody] GetAssignerInsightsModel model)
        {
            try
            {
                model = model ?? new GetAssignerInsightsModel();
                var result = await _ticketsService.GetAssignerInsightsAsync(model);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        #endregion

        // Add to TicketsController

        [HttpPost("UploadAttachment")]
        public async Task<IActionResult> UploadAttachment([FromForm] int ticketId, [FromForm] int userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded" });

            try
            {
                // Create unique file name
                var fileName = file.FileName;
                var storedName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

                // Create folder
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads", "Tickets", ticketId.ToString());
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var fullPath = Path.Combine(uploadFolder, storedName);

                // Save file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save to database
                var model = new TAddAttachmentModel
                {
                    TicketId = ticketId,
                    UserId = userId,
                    FileName = fileName,
                    FilePath = $"/Uploads/Tickets/{ticketId}/{storedName}"
                };

                var result = await _ticketsService.AddAttachmentAsync(model);

                if (!result.Success && System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost("DeleteAttachment")]
        public async Task<IActionResult> DeleteAttachment([FromBody] DeleteAttachmentModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request" });

            try
            {
                var result = await _ticketsService.DeleteAttachmentAsync(model);

                // Delete physical file
                if (result.Success && !string.IsNullOrEmpty(result.FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost("GetAttachmentsByTicketId")]
        public async Task<IActionResult> GetAttachmentsByTicketId([FromBody] GetAttachmentsByTicketIdModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request" });

            try
            {
                var result = await _ticketsService.GetAttachmentsByTicketIdAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost("GetAttachmentById")]
        public async Task<IActionResult> GetAttachmentById([FromBody] GetAttachmentByIdModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request" });

            try
            {
                var result = await _ticketsService.GetAttachmentByIdAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet("DownloadAttachment/{attachmentId}")]
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            try
            {
                var result = await _ticketsService.GetAttachmentByIdAsync(new GetAttachmentByIdModel { AttachmentId = attachmentId });

                if (!result.Success || result.Data == null)
                    return NotFound(new { success = false, message = "Attachment not found" });

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.Data.FilePath.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { success = false, message = "File not found" });

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/octet-stream", result.Data.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
using Azure.Core;
using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;


namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService; //An interface for user-related operations
        private readonly ILogger<NotificationController> _nlogger; //for recording events or errors

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _nlogger = logger;
        }

        [HttpPost("DeleteOldNotifications")]
        public async Task<IActionResult> DeleteOldNotifications(int days_old)
        {
            try
            {
                var response = await _notificationService.DeleteOldNotificationsAsync(days_old);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error deleting old notifications");
                return StatusCode(500, "An error occurred while deleting old notifications");
            }
        }

        [HttpPost("DeleteOldUserTokens")]
        public async Task<IActionResult> DeleteOldUserTokens(int days_old)
        {
            try
            {
                var response = await _notificationService.DeleteOldNotificationsAsync(days_old);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error deleting old user token notifications");
                return StatusCode(500, "An error occurred while deleting old user token");
            }
        }

        [HttpGet("GetUnreadNotificationCount")]
        public async Task<IActionResult> GetUnreadNotificationCount(int userId)
        {
            try
            {
                var response = await _notificationService.GetUnreadNotificationCountAsync(userId);
                return Ok(new { Success = true, Message = "Successfully Data Retrieve", Data = response });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error getting unread notification count");
                return StatusCode(500, "An error occurred while getting unread notification count");
            }
        }

        [HttpGet("GetUserNotifications")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            try
            {
                var response = await _notificationService.GetUserNotificationsAsync(userId);
                return Ok(new { Success = true, Message = "Successfully Data Retrieve", Data = response });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error getting user notifications");
                return StatusCode(500, "An error occurred while getting user notifications");
            }
        }

        [HttpGet("GetUserTokens")]
        public async Task<IActionResult> GetUserTokens(int userId)
        {
            try
            {
                var response = await _notificationService.GetUserTokenAsync(userId);
                return Ok(new { Success = true, Message = "Successfully Data Retrieve", Data = response });
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error getting user tokens");
                return StatusCode(500, "An error occurred while getting user tokens");
            }
        }

        [HttpGet("GetUserFcmTokens")]
        public async Task<IActionResult> GetUserFcmTokens(int userId)
        {
            try
            {
                var tokens = await _notificationService.GetUserFcmTokenAsync(userId);

                if (tokens == null || tokens.Count == 0)
                    return NotFound(new { Success = false, Message = "No tokens found for the specified user." });

                return Ok(new
                {
                    Success = true,
                    Message = "Successfully Data Retrieve",
                    Data = tokens
                });
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                    return BadRequest(new { Success = false, Message = ex.Message });

                return StatusCode(500, new { Success = false, Message = "Database error: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("InsertNotification")]
        public async Task<IActionResult> InsertNotification(InsertNotificationModel request)
        {
            try
            {
                var response = await _notificationService.InsertNotificationAsync(request);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error inserting notification");
                return StatusCode(500, "An error occurred while inserting notification");
            }
        }

        [HttpPost("MarkAllNotificationsAsRead")]
        public async Task<IActionResult> MarkAllNotificationsAsRead(int userId)
        {
            try
            {
                var response = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, "An error occurred while marking all notifications as read");
            }
        }

        [HttpPost("MarkNotificationAsRead")]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            try
            {
                var response = await _notificationService.MarkNotificationAsReadAsync(notificationId);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, "An error occurred while marking notification as read");
            }
        }

        [HttpPost("SaveUserToken")]
        public async Task<IActionResult> SaveUserToken(int userId, string fcmToken, string deviceId)
        {
            try
            {
                var response = await _notificationService.SaveUserToken(userId, fcmToken, deviceId);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error saving user token");
                return StatusCode(500, "An error occurred while saving user token");
            }
        }

        [HttpPost("CreatePage")]
        public async Task<IActionResult> CreatePage(CreatePageRequest request)
        {
            try
            {
                var response = await _notificationService.CreatePageAsync(request);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error creating page");
                return StatusCode(500, "An error occurred while creating page");
            }
        }

        [HttpPost("deleteToken")]
        public async Task<IActionResult> DeleteTokens(int userId, string deviceId)
        {
            try
            {
                var response = await _notificationService.DeleteTokensAsync(userId, deviceId);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error deleting token");
                return StatusCode(500, "An error occurred while deleting token");
            }
        }

        [HttpPost("InsertDeviceInfo")]
        public async Task<IActionResult> InsertDeviceInfo(int userId, string deviceId, string appVersion)
        {
            try
            {
                var response = await _notificationService.InsertDeviceInfoAsync(userId, deviceId, appVersion);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Ok(new { message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _nlogger.LogError(ex, "Error inserting device info");
                return StatusCode(500, "An error occurred while inserting device info");
            }
        }
    }
}

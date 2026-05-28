using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserService userService, ILogger<AccountController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<IActionResult> Settings()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            await Task.CompletedTask;
            return RedirectToAction("Index", "DashboardWeb");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOwnAccount([FromBody] OwnAccountUpdateRequest request)
        {
            var sessionUserId = HttpContext.Session.GetInt32("userId");
            if (sessionUserId == null)
            {
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });
            }

            request.UserId = sessionUserId.Value;

            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                    ?? "Invalid request data.";
                return BadRequest(new { success = false, message = error });
            }

            try
            {
                var result = await _userService.UpdateOwnAccountAsync(request);
                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message });
                }

                HttpContext.Session.SetString("username", request.NewLoginUser.Trim());
                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating own account for user {UserId}", sessionUserId.Value);
                return StatusCode(500, new { success = false, message = "An error occurred while updating account." });
            }
        }
    }
}

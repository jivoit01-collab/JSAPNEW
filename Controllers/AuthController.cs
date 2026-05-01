using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid request data" });

            var result = await _userService.ValidateUserAsync(request);

            if (!result.Success || result.User == null)
            {
                _logger.LogWarning("Failed login attempt for user: {User}", request.loginUser);
                return Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            var user = result.User;

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.userId.ToString()),
                new Claim(ClaimTypes.Name, user.userName ?? ""),
                new Claim("FirstName", user.firstName ?? ""),
                new Claim("LastName", user.lastName ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Successful login for user: {User}", request.loginUser);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                user = new
                {
                    userId = user.userId,
                    userName = user.userName,
                    firstName = user.firstName,
                    lastName = user.lastName
                }
            });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { success = true, message = "Logged out successfully" });
        }

        [HttpGet("getcompanies")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCompanies(int userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { success = false, message = "Invalid user ID" });

                var companies = await _userService.GetCompanyAsync(userId);
                var companyList = companies?.ToList() ?? new List<CompanyModel>();

                if (companyList.Count == 0)
                    return Ok(new { success = false, message = "No companies found for this user", data = companyList });

                return Ok(new { success = true, data = companyList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching companies for user: {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Failed to fetch companies", error = ex.Message });
            }
        }
    }
}

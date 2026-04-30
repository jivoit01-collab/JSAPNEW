using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IAuthSecurityService _authSecurityService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ITokenService tokenService, IAuthSecurityService authSecurityService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _authSecurityService = authSecurityService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid request data" });

            var result = await _userService.ValidateUserAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning("Failed login attempt for user: {User}", request.loginUser);
                return Unauthorized(result);
            }

            var accessToken = _tokenService.GenerateToken(result.User!);
            var refreshToken = await _authSecurityService.GenerateRefreshTokenAsync(result.User!.userId, GetIpAddress());

            _logger.LogInformation("Successful login for user: {User}", request.loginUser);

            return Ok(new LoginResult
            {
                Success = true,
                Message = "Login successful",
                User = result.User,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiresUtc = DateTime.UtcNow.AddMinutes(60)
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register([FromBody] UserRegistrationDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid registration data" });

            int statusCode = await _userService.RegisterUserAsync(request);

            return statusCode switch
            {
                2000 => Ok(new { success = true, message = "User registered successfully" }),
                5001 => Ok(new { success = false, message = "User already exists" }),
                _ => BadRequest(new { success = false, message = "Registration failed" })
            };
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { success = false, message = "Refresh token is required" });

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return Unauthorized(new { success = false, message = "Invalid access token" });

            var userId = int.Parse(userIdClaim);

            var isValid = await _authSecurityService.ValidateRefreshTokenAsync(request.RefreshToken, userId);
            if (!isValid)
                return Unauthorized(new { success = false, message = "Invalid or expired refresh token" });

            await _authSecurityService.RevokeRefreshTokenAsync(request.RefreshToken, GetIpAddress());

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { success = false, message = "User not found" });

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshToken = await _authSecurityService.GenerateRefreshTokenAsync(userId, GetIpAddress());

            return Ok(new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
            });
        }

        [HttpPost("revoke-token")]
        public async Task<ActionResult> RevokeToken([FromBody] RevokeTokenRequest? request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return Unauthorized();

            if (request?.Token != null)
            {
                await _authSecurityService.RevokeRefreshTokenAsync(request.Token, GetIpAddress());
            }
            else
            {
                await _authSecurityService.RevokeAllUserTokensAsync(int.Parse(userIdClaim));
            }

            return Ok(new { success = true, message = "Token revoked successfully" });
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return Unauthorized();

            var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrWhiteSpace(jti))
                await _authSecurityService.RevokeAccessTokenAsync(jti);

            await _authSecurityService.RevokeAllUserTokensAsync(int.Parse(userIdClaim));

            return Ok(new { success = true, message = "Logged out successfully" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || int.Parse(userIdClaim) != request.userId)
                return Forbid();

            var result = await _userService.ChangePasswordAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("change-password2")]
        public async Task<IActionResult> ChangePassword2([FromBody] ChangePasswordRequest2 request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || int.Parse(userIdClaim) != request.userId)
                return Forbid();

            var result = await _userService.ChangePasswordAsync2(request);
            return result.Success ? Ok(result) : BadRequest(result);
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

        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}

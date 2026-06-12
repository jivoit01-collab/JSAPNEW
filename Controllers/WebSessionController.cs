using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Security.Claims;
using JSAPNEW.Data.Entities;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Controllers
{
    [Route("websession")]
    [Authorize]
    public class WebSessionController : Controller
    {
        private readonly IUserService _userService;

        public WebSessionController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("set")]
        public async Task<IActionResult> SetSession([FromBody] SessionRequest request)
        {
            try
            {
                var userId = GetAuthenticatedUserId();

                if (userId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid authenticated user" });
                }

                // Fetch companies using the authenticated user from JWT claims.
                var companies = (await _userService.GetCompanyAsync(userId))?.ToList();

                if (companies == null || companies.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No companies found for user" });
                }

                // Set session values from server-side claims only.
                HttpContext.Session.SetInt32("userId", userId);
                HttpContext.Session.SetString("username", GetAuthenticatedUserName());
                HttpContext.Session.SetString("companyList", JsonConvert.SerializeObject(companies));
                HttpContext.Session.SetInt32("selectedCompanyId", companies[0].id); // Default to first

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetSession: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while setting session data" });
            }
        }

        [HttpPost("updateSelectedCompany")]
        public async Task<IActionResult> UpdateSelectedCompany([FromBody] CompanySelectRequest req)
        {
            try
            {
                if (req == null || req.CompanyId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid company ID" });
                }

                var userId = GetAuthenticatedUserId();

                if (userId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid authenticated user" });
                }

                var companies = (await _userService.GetCompanyAsync(userId))?.ToList();

                if (companies == null || !companies.Any(company => company.id == req.CompanyId))
                {
                    return Forbid();
                }

                HttpContext.Session.SetInt32("selectedCompanyId", req.CompanyId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in UpdateSelectedCompany: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while updating company selection" });
            }
        }

        // Add a method to check if session is valid
        [HttpGet("checksession")]
        public IActionResult CheckSession()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("userId");
                var companyList = HttpContext.Session.GetString("companyList");

                if (!userId.HasValue || string.IsNullOrEmpty(companyList))
                {
                    return Json(new { valid = false });
                }

                return Json(new { valid = true });
            }
            catch (Exception)
            {
                return Json(new { valid = false });
            }
        }

        private int GetAuthenticatedUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(claimValue, out var userId) ? userId : 0;
        }

        private string GetAuthenticatedUserName()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.Identity?.Name
                ?? "User";
        }
    }

    public class CompanySelectRequest
    {
        public int CompanyId { get; set; }
    }

    public class SessionRequest
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
    }

    public class CompanyDto
    {
        public int id { get; set; }
        public string company { get; set; }
    }
}

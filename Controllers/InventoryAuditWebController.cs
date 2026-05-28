using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JSAPNEW.Controllers
{
    public class InventoryAuditWebController : Controller
    {
        private readonly IUserService _userService;
        private  readonly IInventoryAuditService _inventoryAuditService;
        //private readonly IHttpClientFactory _httpClientFactory;
        public InventoryAuditWebController(IUserService UserService, IInventoryAuditService inventoryAuditService)
        {
            _userService = UserService;
            _inventoryAuditService = inventoryAuditService;

            // _httpClientFactory = httpClientFactory;

        }
        public async Task<IActionResult> CountEntry()
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;

            var data = await _userService.GetSelectedUserAsync(company, userId);
            var u = data?.FirstOrDefault();
            if (u == null) return NotFound("User not found");

            // ✅ Call stored procedure through service
            var uniqueUsernameResponse = await _inventoryAuditService.GenerateUniqueUsernameAsync(userId);
            if (!uniqueUsernameResponse.Success)
            {
                // If procedure fails, show message
                ViewBag.LotNumber = $"Error: {uniqueUsernameResponse.Message}";
            }
            else
            {
                ViewBag.LotNumber = uniqueUsernameResponse.UniqueUsername;  // ✅ FIXED: use UniqueUsername
            }

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;
            ViewBag.UserName = $"{u.firstName} {u.lastName}".Trim();
            ViewBag.empId = u.empId;

            return View(); // no model passed
        }
        public async Task<IActionResult> CountEntryPre()
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;

            var data = await _userService.GetSelectedUserAsync(company, userId);
            var u = data?.FirstOrDefault();
            if (u == null) return NotFound("User not found");

            // ✅ Call stored procedure through service
            var uniqueUsernameResponse = await _inventoryAuditService.GenerateUniqueUsernameAsync(userId);
            if (!uniqueUsernameResponse.Success)
            {
                // If procedure fails, show message
                ViewBag.LotNumber = $"Error: {uniqueUsernameResponse.Message}";
            }
            else
            {
                ViewBag.LotNumber = uniqueUsernameResponse.UniqueUsername;  // ✅ FIXED: use UniqueUsername
            }

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;
            ViewBag.UserName = $"{u.firstName} {u.lastName}".Trim();
            ViewBag.empId = u.empId;

            return View(); // no model passed
        }
        public async Task<IActionResult> EditCountEntry()
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;

            var data = await _userService.GetSelectedUserAsync(company, userId);
            var u = data?.FirstOrDefault();
            if (u == null) return NotFound("User not found");

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;
            ViewBag.UserName = $"{u.firstName} {u.lastName}".Trim();
            ViewBag.empId = u.empId;

            return View(); // no model passed
        }

        public async Task<IActionResult> AllInventory()
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;

            var data = await _userService.GetSelectedUserAsync(company, userId);
            var u = data?.FirstOrDefault();
            if (u == null) return NotFound("User not found");

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;
            ViewBag.UserName = $"{u.firstName} {u.lastName}".Trim();
            ViewBag.empId = u.empId;

            return View(); // no model passed
        }

        public async Task<IActionResult> MyAllSession()
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;

            var data = await _userService.GetSelectedUserAsync(company, userId);
            var u = data?.FirstOrDefault();
            if (u == null) return NotFound("User not found");

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;
            ViewBag.UserName = $"{u.firstName} {u.lastName}".Trim();
            ViewBag.empId = u.empId;

            return View(); // no model passed
        }
        public async Task<IActionResult> AddSession()
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;

            var data = await _userService.GetSelectedUserAsync(company, userId);
            var u = data?.FirstOrDefault();
            if (u == null) return NotFound("User not found");

            // ✅ Call stored procedure through service
            var uniqueUsernameResponse = await _inventoryAuditService.GenerateUniqueUsernameAsync(userId);
            if (!uniqueUsernameResponse.Success)
            {
                // If procedure fails, show message
                ViewBag.LotNumber = $"Error: {uniqueUsernameResponse.Message}";
            }
            else
            {
                ViewBag.LotNumber = uniqueUsernameResponse.UniqueUsername;  // ✅ FIXED: use UniqueUsername
            }

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;
            ViewBag.UserName = $"{u.firstName} {u.lastName}".Trim();
            ViewBag.empId = u.empId;

            return View(); // no model passed
        }

        public async Task<IActionResult> SessionList()
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;
            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;

            return View();
        }

        // GET: InventoryAudit/SessionDetails
        public async Task<IActionResult> SessionDetails(int sessionId, string lotNumber)
        {
            
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;
            int userId = adminId ?? 0;

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;
            ViewBag.SessionId = sessionId;
            ViewBag.LotNumber = lotNumber;

            return View();
        }
    }
}

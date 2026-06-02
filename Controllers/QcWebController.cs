using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class QcWebController : Controller
    {
        private readonly IPrdoService _PrdoService;
        private readonly IInventoryAuditService _inventoryAuditService;
        private readonly IUserService _userService;
        public QcWebController(IPrdoService PrdoService, IInventoryAuditService inventoryAuditService, IUserService userService)
        {
            _PrdoService = PrdoService;
            _inventoryAuditService = inventoryAuditService;
            _userService = userService;
        }
        public async Task<IActionResult> QualityCheck()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                // Redirect or show message
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.CompanyId = selectedCompanyId;
            int company = selectedCompanyId.Value;
            int UserId = userId ?? 0;

            // ✅ Call stored procedure through service
            /* var uniqueUsernameResponse = await _inventoryAuditService.GenerateUniqueUsernameAsync(UserId);
             if (!uniqueUsernameResponse.Success)
             {
                 // If procedure fails, show message
                 ViewBag.LotNumber = $"Error: {uniqueUsernameResponse.Message}";
             }
             else
             {
                 ViewBag.LotNumber = uniqueUsernameResponse.UniqueUsername;  // ✅ FIXED: use UniqueUsername
             }*/

            // ✅ send only these three via ViewBag
            ViewBag.Company = company;
            ViewBag.UserId = userId;

            return View(); // no model passed
        }
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                // Redirect or show message
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.CompanyId = selectedCompanyId;
            int company = selectedCompanyId.Value;

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View();
        }
    }
}

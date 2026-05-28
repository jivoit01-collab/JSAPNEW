using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class ReportsWebController : Controller
    {
        public IActionResult ApprovalStatusReport()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (userId == null || selectedCompanyId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.CompanyId = selectedCompanyId;
            ViewBag.UserId = userId;

            return View();
        }
    }
}

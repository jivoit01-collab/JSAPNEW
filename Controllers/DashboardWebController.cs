using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace JSAPNEW.Controllers
{
    [Authorize]
    public class DashboardWebController : Controller
    {
        public IActionResult Index()
        {
            if (!TrySetDashboardIdentity())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult ITdashboard()
        {
            if (!TrySetDashboardIdentity())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult TaskDashboard()
        {
            if (!TrySetDashboardIdentity())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult ClientDashboard()
        {
            if (!TrySetDashboardIdentity())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult MomDashboard()
        {
            if (!TrySetDashboardIdentity())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult AvtarDashboard()
        {
            if (!TrySetDashboardIdentity())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        private bool TrySetDashboardIdentity()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var username = HttpContext.Session.GetString("username");
            var companiesJson = HttpContext.Session.GetString("companyList");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            List<CompanyDto> companies = new List<CompanyDto>();
            if (!string.IsNullOrEmpty(companiesJson))
            {
                companies = JsonConvert.DeserializeObject<List<CompanyDto>>(companiesJson);
            }

            if (!userId.HasValue
                || userId.Value <= 0
                || string.IsNullOrWhiteSpace(username)
                || !selectedCompanyId.HasValue
                || selectedCompanyId.Value <= 0
                || companies.Count == 0
                || companies.All(company => company.id != selectedCompanyId.Value))
            {
                return false;
            }

            ViewBag.UserId = userId;
            ViewBag.Username = username;
            ViewBag.Companies = companies;
            ViewBag.SelectedCompanyId = selectedCompanyId;
            return true;
        }

        public IActionResult Overview()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_Overview");

            return View("_Overview"); // fallback if JavaScript fails
        }
    }
}

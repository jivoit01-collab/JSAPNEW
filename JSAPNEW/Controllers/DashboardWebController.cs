using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace JSAPNEW.Controllers
{
    public class DashboardWebController : Controller
    {
        public IActionResult Index()
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

            ViewBag.UserId = userId;
            ViewBag.Username = username;
            ViewBag.Companies = companies;
            ViewBag.SelectedCompanyId = selectedCompanyId;

            return View();
        }

        public IActionResult ITdashboard()
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

            ViewBag.UserId = userId;
            ViewBag.Username = username;
            ViewBag.Companies = companies;
            ViewBag.SelectedCompanyId = selectedCompanyId;

            return View();
        }

        public IActionResult TaskDashboard()
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

            ViewBag.UserId = userId;
            ViewBag.Username = username;
            ViewBag.Companies = companies;
            ViewBag.SelectedCompanyId = selectedCompanyId;

            return View();
        }

        public IActionResult ClientDashboard()
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

            ViewBag.UserId = userId;
            ViewBag.Username = username;
            ViewBag.Companies = companies;
            ViewBag.SelectedCompanyId = selectedCompanyId;

            return View();
        }

        public IActionResult MomDashboard()
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

            ViewBag.UserId = userId;
            ViewBag.Username = username;
            ViewBag.Companies = companies;
            ViewBag.SelectedCompanyId = selectedCompanyId;

            return View();
        }

        public IActionResult AvtarDashboard()
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

            ViewBag.UserId = userId;
            ViewBag.Username = username;
            ViewBag.Companies = companies;
            ViewBag.SelectedCompanyId = selectedCompanyId;

            return View();
        }

        public IActionResult Overview()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_Overview");

            return View("_Overview"); // fallback if JavaScript fails
        }
    }
}

using JSAPNEW.Models;
using JSAPNEW.Services;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace JSAPNEW.Controllers
{
    public class BillVerificationWebController : Controller
    {
        private readonly IBillVerificationService _billVerificationService;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public BillVerificationWebController(IBillVerificationService billVerificationService, IConfiguration configuration, IUserService UserService)
        {
            _billVerificationService = billVerificationService;
            _configuration = configuration;
            _userService = UserService;
        }
        public async Task<IActionResult> MakerPage()
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

            // Await the asynchronous call
            var users = await _userService.GetAllUserAsync(company);

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View("~/Views/BillVerification/MakerPage.cshtml", users);
        }

        public async Task<IActionResult> CheckerPage()
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

            // Await the asynchronous call
            var users = await _userService.GetAllUserAsync(company);

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View("~/Views/BillVerification/CheckerPage.cshtml", users);
        }

        public async Task<IActionResult> InvoicePaymentPage()
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

            // Await the asynchronous call
            var users = await _userService.GetAllUserAsync(company);

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View("~/Views/BillVerification/InvoicePaymentPage.cshtml", users);
        }


        public async Task<IActionResult> PaymentCheckerPage()
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

            // Await the asynchronous call
            var users = await _userService.GetAllUserAsync(company);

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View("~/Views/BillVerification/PaymentCheckerPage.cshtml", users);
        }

        public async Task<IActionResult> AdminPage()
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

            // Await the asynchronous call
            var users = await _userService.GetAllUserAsync(company);

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            ViewBag.IsReadOnly = false;
            return View("~/Views/BillVerification/AdminPage.cshtml", users);
        }

        // ============================
        // ADMIN (VIEW ONLY) — same dashboard as AdminPage but read-only:
        // no Attach / Delete / any mutating action. Gated by the
        // 'AdminView' permission of the 'Bill Verification' module.
        // ============================
        public async Task<IActionResult> AdminViewPage()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.CompanyId = selectedCompanyId;
            int company = selectedCompanyId.Value;

            var users = await _userService.GetAllUserAsync(company);

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            ViewBag.IsReadOnly = true;   // <- drives the read-only rendering in the view
            return View("~/Views/BillVerification/AdminPage.cshtml", users);
        }

    }
}

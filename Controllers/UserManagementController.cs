using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly IUserService _userService;
        //private readonly IHttpClientFactory _httpClientFactory;
        public UserManagementController(IUserService UserService)
        {
            _userService = UserService;
            // _httpClientFactory = httpClientFactory;

        }
        public async Task<IActionResult> AllUsers()
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
            return View(users);
        }

        public IActionResult UserRegistration()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (userId == null)
            {
                // Redirect if session expired
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View();
        }

        public IActionResult UserPermission()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (userId == null)
            {
                // Redirect if session expired
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View();
        }

        public async Task<IActionResult> EditUser(int userId)
        {
            var adminId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (selectedCompanyId == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            int company = selectedCompanyId.Value;

            var data1 = await _userService.GetSelectedUserAsync(company, userId);
            var data2 = await _userService.GetAllPermissionsOfOneUserAsync(userId, company);


            var viewModel = new EditUserViewModel
            {
                SelectedUser = data1.FirstOrDefault(),  // Since it's IEnumerable
                Permissions = data2
            };
            ViewBag.CompanyId = company;
            ViewBag.AdminId = adminId;

            return View(viewModel);
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DocumentDispatch()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (userId == null)
            {
                // Redirect if session expired
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View();
        }

        public IActionResult DocumentRecieve()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (userId == null)
            {
                // Redirect if session expired
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View();
        }

        public IActionResult RejectDocument()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (userId == null)
            {
                // Redirect if session expired
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View();
        }
    }
}

using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class GIGOwebController : Controller
    {
        private readonly IGIGOService _gigoService;
        private readonly IUserService _userService;
        public GIGOwebController(IGIGOService gigoService, IUserService UserService)
        {
            _gigoService = gigoService;
            _userService = UserService;
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

            // Await the asynchronous call
            var users = await _userService.GetAllUserAsync(company);

            ViewBag.UserId = userId.Value;
            ViewBag.CompanyId = selectedCompanyId;
            return View(users);
        }
    }
}

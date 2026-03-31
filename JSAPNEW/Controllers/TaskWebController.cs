using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class TaskWebController : Controller
    {
        private readonly ITaskService _TaskService;
        public TaskWebController(ITaskService TaskService)
        {
            _TaskService = TaskService;
        }
        public IActionResult Tasks()
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

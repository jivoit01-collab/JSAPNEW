using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JSAPNEW.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

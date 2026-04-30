using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JSAPNEW.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

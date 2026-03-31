using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class TicketsWebController : Controller
    {
        // View 1: User Raise Ticket
        public IActionResult RaiseTicket()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.UserId = userId;
            return View();
        }

        // View 2: IT Admin Dashboard - Check and Assign Tickets
        public IActionResult AdminDashboard()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.UserId = userId;
            return View();
        }

        // View 3: User View All Tickets with Status
        public IActionResult MyTickets()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.UserId = userId;
            return View();
        }

        // View 4: Ticket Details (Optional - for viewing single ticket)
        public IActionResult TicketDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.UserId = userId;
            ViewBag.TicketId = id;
            return View();
        }

        // View 5: Assigned Tickets (For IT Team Members)
        public IActionResult AssignedTickets()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.UserId = userId;
            return View();
        }
    }
}
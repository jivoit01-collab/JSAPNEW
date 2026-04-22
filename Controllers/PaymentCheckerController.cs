using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class PaymentCheckerController : Controller
    {
        private readonly PaymentCheckerService _service;

        public PaymentCheckerController(PaymentCheckerService service)
        {
            _service = service;
        }

        // ============================
        // LOAD PAGE
        // ============================
        public IActionResult PaymentCheckerPage()
        {
            return View();
        }

        // ============================
        // GET PAID BILLS
        // ============================
        [HttpGet]
        public IActionResult GetPaidBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = _service.GetPaidBillDetails(fromDate, toDate, accountName);
            return Json(data);
        }

        // ============================
        // GET INVOICE ITEMS (row expand)
        // ============================
        [HttpGet]
        public IActionResult GetInvoiceItems(int vchNumber)
        {
            var data = _service.GetInvoiceItemDetails(vchNumber);
            return Json(data);
        }

        // ============================
        // ACCOUNT SUGGESTIONS
        // ============================
        [HttpGet]
        public IActionResult GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
        {
            var data = _service.GetAccountSuggestions(term, fromDate, toDate);
            return Json(data);
        }
    }
}

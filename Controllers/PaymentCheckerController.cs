using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Controllers
{
    [Authorize]
    public class PaymentCheckerController : Controller
    {
        private readonly IPaymentCheckerService _service;

        public PaymentCheckerController(IPaymentCheckerService service)
        {
            _service = service;
        }

        // ============================
        // LOAD PAGE
        // ============================
        public IActionResult PaymentCheckerPage()
        {
            return View("~/Views/PaymentChecker/PaymentCheckerPage.cshtml");
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

    }
}
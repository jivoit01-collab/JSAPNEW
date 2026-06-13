using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Controllers
{
    [Authorize]
    public class PaymentCheckerController : Controller
    {
        private readonly IPaymentCheckerService _service;
        private readonly IMakerService _makerService;

        public PaymentCheckerController(IPaymentCheckerService service, IMakerService makerService)
        {
            _service = service;
            _makerService = makerService;
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

        [HttpGet]
        public IActionResult GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
        {
            var data = _makerService.GetAccountSuggestions(term, fromDate, toDate);
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
        [HttpPost]
        public IActionResult MarkVerified(int vchNumber)
        {
            _service.MarkVerified(vchNumber);

            return Json(new
            {
                success = true,
                message = "Payment verified successfully"
            });
        }

        [HttpPost]
        public IActionResult RejectPayment(int vchNumber, string remark)
        {
            var success = _service.RejectPayment(vchNumber, remark);

            return Json(new
            {
                success,
                message = success ? "Payment rejected successfully" : "Unable to reject payment"
            });
        }
    }
}

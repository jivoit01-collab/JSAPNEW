
using Microsoft.AspNetCore.Mvc;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Controllers
{
    public class InvoicePaymentController : Controller
    {
        private readonly IInvoicePaymentService _service;

        public InvoicePaymentController(IInvoicePaymentService service)
        {
            _service = service;
        }

        // ============================
        // LOAD PAGE
        // ============================
        public IActionResult InvoicePaymentPage()
        {
            return View();
        }

        // ============================
        // GET BILL DETAILS
        // ============================
        [HttpGet]
        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = _service.GetBillDetails(fromDate, toDate, accountName, null);
            return Json(data);
        }

        // ============================
        // GET INVOICE ITEMS
        // ============================
        [HttpGet]
        public IActionResult GetInvoiceItems(int vchNumber)
        {
            var data = _service.GetInvoiceItemDetails(vchNumber);
            return Json(data);
        }
        [HttpPost]
        public IActionResult MarkAsPaid([FromBody] MarkPaidRequest req)
        {
            var result = _service.MarkAsPaid(req.VchNumber);

            if (!result)
                return NotFound();

            return Ok();
        }

        public class MarkPaidRequest
        {
            public decimal VchNumber { get; set; }
        }
    }
}

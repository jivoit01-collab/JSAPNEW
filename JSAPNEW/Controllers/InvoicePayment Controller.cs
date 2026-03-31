
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace JSAPNEW.Controllers
{
    public class InvoicePaymentController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly InvoicePaymentService _service;

        public InvoicePaymentController(IConfiguration configuration, InvoicePaymentService service)
        {
            _configuration = configuration;
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
        // GET DATA (ONLY SEEN)
        // ============================

        [HttpGet]
        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = _service.GetBillDetails(fromDate, toDate, accountName, null);
            return Json(data);
        }
    }
}
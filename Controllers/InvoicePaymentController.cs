

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using System.Configuration;
//using System.Data;

//namespace JSAPNEW.Controllers
//{
//    public class InvoicePaymentController : Controller
//    {

//        private readonly IConfiguration _configuration;
//        private readonly InvoicePaymentService _service;

//        public InvoicePaymentController(IConfiguration configuration, InvoicePaymentService service)
//        {
//            _configuration = configuration;
//            _service = service;
//        }

//        // ============================
//        // LOAD PAGE
//        // ============================
//        public IActionResult InvoicePaymentPage()
//        {
//            return View();
//        }

//        // ============================
//        // GET DATA (ONLY SEEN)
//        // ============================

//        [HttpGet]
//        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
//        {
//            var data = _service.GetBillDetails(fromDate, toDate, accountName, null);
//            return Json(data);
//        }
//        [HttpGet]
//        public IActionResult GetInvoiceItems(int vchNumber)
//        {
//            var data = _service.GetInvoiceItemDetails(vchNumber); // ✅ int pass
//            return Json(data);
//        }
//    }
//}
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
    }
}
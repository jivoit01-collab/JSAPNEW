//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using System.Data;

//namespace JSAPNEW.Controllers
//{
//    public class CheckerController : Controller
//    {
//        private readonly IConfiguration _configuration;
//        private readonly CheckerService _service;

//        public CheckerController(IConfiguration configuration, CheckerService service)
//        {
//            _configuration = configuration;
//            _service = service;
//        }

//        // ============================
//        // LOAD PAGE
//        // ============================
//        public IActionResult CheckerPage()
//        {
//            return View();
//        }

//        // ============================
//        // GET DATA

//        [HttpGet]
//        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
//        {
//            var data = _service.GetBillDetails(fromDate, toDate, accountName);
//            return Json(data);
//        }


//        [HttpPost]
//        public IActionResult UpdateCheckerStatus(int vchNumber, string status, string remark)
//        {
//            try
//            {
//                _service.UpdateCheckerStatus(vchNumber, status, remark);
//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//    [HttpGet]
//        public IActionResult GetInvoiceItems(decimal serialNumber)
//        {
//            var data = _service.GetInvoiceItemDetails(serialNumber);
//            return Json(data);
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Controllers
{
    public class CheckerController : Controller
    {
        private readonly ICheckerService _service;

        public CheckerController(ICheckerService service)
        {
            _service = service;
        }

        // ============================
        // LOAD PAGE
        // ============================
        public IActionResult CheckerPage()
        {
            return View();
        }

        // ============================
        // GET BILL DETAILS
        // ============================
        [HttpGet]
        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = _service.GetBillDetails(fromDate, toDate, accountName);
            return Json(data);
        }

        // ============================
        // UPDATE CHECKER STATUS
        // ============================
        [HttpPost]
        public IActionResult UpdateCheckerStatus(int vchNumber, string status, string remark)
        {
            try
            {
                _service.UpdateCheckerStatus(vchNumber, status, remark);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================
        // GET INVOICE ITEMS
        // ============================
        [HttpGet]
        public IActionResult GetInvoiceItems(decimal serialNumber)
        {
            var data = _service.GetInvoiceItemDetails(serialNumber);
            return Json(data);
        }
    }
}
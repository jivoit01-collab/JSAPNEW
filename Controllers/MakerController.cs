

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using System.Data;

//namespace JSAPNEW.Controllers
//{
//    public class MakerController : Controller
//    {

//        private readonly IConfiguration _configuration;
//        private readonly IWebHostEnvironment _hostingEnvironment;
//        private readonly MakerService _service;

//        public MakerController(IConfiguration configuration,
//                               IWebHostEnvironment hostingEnvironment,
//                               MakerService service)
//        {
//            _configuration = configuration;
//            _hostingEnvironment = hostingEnvironment;
//            _service = service;
//        }

//        // LOAD PAGE

//        public IActionResult MakerPage()
//        {
//            return View();
//        }


//        // get bill detail//
//        [HttpGet]
//        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, decimal? serialNumber = null)
//        {
//            var data = _service.GetBillDetails(fromDate, toDate, accountName, serialNumber);


//            return Json(data);
//        }


//        // ACCOUNT SUGGESTIONS (OMNI SEARCH)


//        [HttpGet]
//        public IActionResult GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
//        {
//            var data = _service.GetAccountSuggestions(term, fromDate, toDate);
//            return Json(data);
//        }
//        // SUBMIT BILL (UPLOAD)

//        [HttpPost]
//        public async Task<IActionResult> SubmitBill(int vchNumber,
//                                                    IFormFile file,
//                                                    string makerRemark)
//        {
//            try
//            {
//                if (file == null || file.Length == 0)
//                {
//                    return Json(new { success = false, message = "Please select a file" });
//                }
//                {
//                    string filePath = null;

//                    if (file != null && file.Length > 0)
//                    {
//                        string folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "maker");

//                        if (!Directory.Exists(folderPath))
//                            Directory.CreateDirectory(folderPath);

//                        string uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
//                        string fullPath = Path.Combine(folderPath, uniqueFileName);

//                        using (var stream = new FileStream(fullPath, FileMode.Create))
//                        {
//                            await file.CopyToAsync(stream);
//                        }

//                        filePath = "/uploads/maker/" + uniqueFileName;
//                    }

//                    string connStr = _configuration.GetConnectionString("FHConnection");

//                    using (SqlConnection conn = new SqlConnection(connStr))
//                    {
//                        string query = @"
//                INSERT INTO AttachmentUpload
//                (VchNumber, AttachmentPath, MakerRemark, Status, CreatedDate)
//                VALUES (@VchNumber, @AttachmentPath, @MakerRemark, 'Submitted', GETDATE())";

//                        SqlCommand cmd = new SqlCommand(query, conn);

//                        cmd.Parameters.Add("@VchNumber", SqlDbType.Int).Value = vchNumber;
//                        cmd.Parameters.Add("@AttachmentPath", SqlDbType.NVarChar).Value = (object)filePath ?? DBNull.Value;
//                        cmd.Parameters.Add("@MakerRemark", SqlDbType.NVarChar).Value =
//                            string.IsNullOrWhiteSpace(makerRemark) ? DBNull.Value : makerRemark;

//                        conn.Open();
//                        cmd.ExecuteNonQuery();
//                    }

//                    return Json(new { success = true });
//                }
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }

//        }



//        [HttpGet]
//        public IActionResult GetInvoiceItems(int vchNumber)
//        {
//            var data = _service.GetInvoiceItemDetails(vchNumber);
//            return Json(data);
//        }
//    
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Controllers
{
    public class MakerController : Controller
    {
        private readonly IMakerService _service;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;

        public MakerController(IMakerService service,
                               IWebHostEnvironment hostingEnvironment,
                               IConfiguration configuration)
        {
            _service = service;
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
        }

        public IActionResult MakerPage() => View();

        [HttpGet]
        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, decimal? serialNumber = null)
        {
            var data = _service.GetBillDetails(fromDate, toDate, accountName, serialNumber);
            return Json(data);
        }

        [HttpGet]
        public IActionResult GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
        {
            var data = _service.GetAccountSuggestions(term, fromDate, toDate);
            return Json(data);
        }

        [HttpGet]
        public IActionResult GetInvoiceItems(int vchNumber)
        {
            var data = _service.GetInvoiceItemDetails(vchNumber);
            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitBill(int vchNumber, IFormFile file, string makerRemark)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Please select a file" });

                string folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "maker");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string fullPath = Path.Combine(folderPath, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                string filePath = "/uploads/maker/" + uniqueFileName;
                string connStr = _configuration.GetConnectionString("FHConnection");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string query = @"INSERT INTO AttachmentUpload
                        (VchNumber, AttachmentPath, MakerRemark, Status, CreatedDate)
                        VALUES (@VchNumber, @AttachmentPath, @MakerRemark, 'Submitted', GETDATE())";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.Add("@VchNumber", SqlDbType.Int).Value = vchNumber;
                    cmd.Parameters.Add("@AttachmentPath", SqlDbType.NVarChar).Value = filePath;
                    cmd.Parameters.Add("@MakerRemark", SqlDbType.NVarChar).Value =
                        string.IsNullOrWhiteSpace(makerRemark) ? DBNull.Value : makerRemark;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
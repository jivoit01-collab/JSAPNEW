
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
                if (!System.IO.File.Exists(fullPath))
                {
                    throw new Exception("File upload failed");
                }

                string filePath = "/uploads/maker/" + uniqueFileName;
                string connStr = _configuration.GetConnectionString("FHConnection");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string query = @"

IF EXISTS (SELECT 1 FROM AttachmentUpload WHERE VchNumber = @VchNumber)

BEGIN

    UPDATE AttachmentUpload
    SET
        AttachmentPath = @AttachmentPath,
        MakerRemark = @MakerRemark,
        CheckerStatus = NULL,
        CheckerRemark = NULL,
        Status = 'Submitted',
        CreatedDate = GETDATE()

    WHERE VchNumber = @VchNumber

END

ELSE

BEGIN

    INSERT INTO AttachmentUpload
    (
        VchNumber,
        AttachmentPath,
        MakerRemark,
        Status,
        CreatedDate
    )

    VALUES
    (
        @VchNumber,
        @AttachmentPath,
        @MakerRemark,
        'Submitted',
        GETDATE()
    )

END
";

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
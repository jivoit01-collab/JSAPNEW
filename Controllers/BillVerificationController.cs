using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    public class BillVerificationController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BillVerificationController> _logger;
        private readonly IBillVerificationService _service;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly string _connStr;
        private readonly IBillVerificationLogService _log;
        public BillVerificationController(IConfiguration configuration, ILogger<BillVerificationController> logger, IBillVerificationService billVerificationService, IWebHostEnvironment hostingEnvironment, IBillVerificationLogService log)
        {
            _configuration = configuration;
            _logger = logger;
            _service = billVerificationService;
            _hostingEnvironment = hostingEnvironment;
            _connStr = _configuration.GetConnectionString("FHConnection");
            _log = log;
        }

        // current logged-in user from session (for "who did it" columns)
        private int? CurrentUserId() => HttpContext.Session.GetInt32("userId");
        private string CurrentUserName() => HttpContext.Session.GetString("username");

        // for maker 

        [HttpGet("/Maker/GetBillDetails")]
        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, decimal? serialNumber = null)
        {
            var data = _service.GetBillDetails(fromDate, toDate, accountName, serialNumber);
            return Json(data);
        }

        [HttpGet("/Maker/GetAccountSuggestions")]
        [HttpGet("/Checker/GetAccountSuggestions")]
        public IActionResult GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
        {
            var data = _service.GetAccountSuggestions(term, fromDate, toDate);
            return Json(data);
        }

        [HttpGet("/Maker/GetInvoiceItems")]
        public IActionResult GetInvoiceItems(int vchNumber)
        {
            var data = _service.GetInvoiceItemDetails(vchNumber);
            return Json(data);
        }
        [HttpPost("/Maker/SubmitBill")]
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
                                MakerBy = @MakerBy,
                                MakerByName = @MakerByName,
                                CheckerStatus = NULL,
                                CheckerRemark = NULL,
                                CheckerDate = NULL,
                                CheckerBy = NULL,
                                CheckerByName = NULL,
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
                                MakerBy,
                                MakerByName,
                                Status,
                                CreatedDate
                            )

                            VALUES
                            (
                                @VchNumber,
                                @AttachmentPath,
                                @MakerRemark,
                                @MakerBy,
                                @MakerByName,
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
                    cmd.Parameters.AddWithValue("@MakerBy", (object)CurrentUserId() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MakerByName",
                        string.IsNullOrWhiteSpace(CurrentUserName()) ? DBNull.Value : CurrentUserName());
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                _log.Log(HttpContext, "Invoice Maker", "Submit / Upload", vchNumber, makerRemark, "File: " + file.FileName);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================
        // ADMIN ATTACH FILE — admin uploads a file for an entry that has none
        // ============================
        [HttpPost("/Admin/AttachFile")]
        public async Task<IActionResult> AdminAttachFile(int vchNumber, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Please select a file" });

                string folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "admin");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string fullPath = Path.Combine(folderPath, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                if (!System.IO.File.Exists(fullPath))
                    throw new Exception("File upload failed");

                string filePath = "/uploads/admin/" + uniqueFileName;
                string connStr = _configuration.GetConnectionString("FHConnection");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    // upsert: attach the file, put the entry into the workflow, record the admin as uploader
                    string query = @"
                        IF EXISTS (SELECT 1 FROM AttachmentUpload WHERE VchNumber = @VchNumber)
                        BEGIN
                            UPDATE AttachmentUpload
                            SET AttachmentPath = @AttachmentPath,
                                MakerBy        = @MakerBy,
                                MakerByName    = @MakerByName,
                                Status         = 'Submitted',
                                CreatedDate    = GETDATE()
                            WHERE VchNumber = @VchNumber
                        END
                        ELSE
                        BEGIN
                            INSERT INTO AttachmentUpload
                                (VchNumber, AttachmentPath, MakerBy, MakerByName, Status, CreatedDate)
                            VALUES
                                (@VchNumber, @AttachmentPath, @MakerBy, @MakerByName, 'Submitted', GETDATE())
                        END";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.Add("@VchNumber", SqlDbType.Int).Value = vchNumber;
                    cmd.Parameters.Add("@AttachmentPath", SqlDbType.NVarChar).Value = filePath;
                    cmd.Parameters.AddWithValue("@MakerBy", (object)CurrentUserId() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MakerByName",
                        string.IsNullOrWhiteSpace(CurrentUserName()) ? DBNull.Value : CurrentUserName());
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                _log.Log(HttpContext, "Admin", "Attach File", vchNumber, null, "File: " + file.FileName);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // for checker

        // ============================
        // GET BILL DETAILS
        // ============================
        [HttpGet("/Checker/GetBillDetails")]
        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, string status)
        {
            var data = _service.GetBillDetails(fromDate, toDate, accountName, status);
            return Json(data);
        }

        // ============================
        // UPDATE CHECKER STATUS
        // ============================
        [HttpPost("/Checker/UpdateCheckerStatus")]
        public IActionResult UpdateCheckerStatus(int vchNumber, string status, string remark)
        {
            try
            {
                _service.UpdateCheckerStatus(vchNumber, status, remark, CurrentUserId(), CurrentUserName());
                _log.Log(HttpContext, "Invoice Checker", status, vchNumber, remark);
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
        [HttpGet("/Checker/GetInvoiceItems")]
        public IActionResult GetInvoiceItems(decimal serialNumber)
        {
            var data = _service.GetInvoiceItemDetails(serialNumber);
            return Json(data);
        }

        // for invoice payment
        // ============================
        // GET BILL DETAILS
        // ============================
        [HttpGet("/InvoicePayment/GetBillDetails")]
        public IActionResult GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = _service.GetBillDetailsIP(fromDate, toDate, accountName, null);
            return Json(data);
        }

        // ============================
        // GET INVOICE ITEMS
        // ============================
        [HttpGet("/InvoicePayment/GetInvoiceItems")]
        [HttpGet("/InvoicePayment/GetInvoiceItemsIP")]
        public IActionResult GetInvoiceItemsIP(int vchNumber)
        {
            var data = _service.GetInvoiceItemDetails(vchNumber);
            return Json(data);
        }
        [HttpPost("/InvoicePayment/MarkAsPaid")]
        public IActionResult MarkAsPaid([FromBody] MarkPaidRequest req)
        {
            var result = _service.MarkAsPaid(req.VchNumber, CurrentUserId(), CurrentUserName());

            if (!result)
                return NotFound();

            _log.Log(HttpContext, "Payment Maker", "Mark Paid", req.VchNumber);
            return Ok();
        }

        public class MarkPaidRequest
        {
            public decimal VchNumber { get; set; }
        }

        // ============================
        // REJECT BY PAYMENT MAKER — remark is MANDATORY, action is logged
        // ============================
        [HttpPost("/InvoicePayment/Reject")]
        public IActionResult RejectByPaymentMaker([FromBody] RejectPaymentMakerRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Remark))
                return Json(new { success = false, message = "Remark is required to reject an entry." });

            var success = _service.RejectByPaymentMaker(req.VchNumber, req.Remark.Trim(),
                                                         CurrentUserId(), CurrentUserName());

            if (success)
                _log.Log(HttpContext, "Payment Maker", "Reject", req.VchNumber, req.Remark.Trim());

            return Json(new
            {
                success,
                message = success ? "Entry rejected successfully" : "Unable to reject entry"
            });
        }

        public class RejectPaymentMakerRequest
        {
            public decimal VchNumber { get; set; }
            public string Remark { get; set; }
        }

        // for invoice payment checker
        // ============================
        // GET PAID BILLS
        // ============================
        [HttpGet("/PaymentChecker/GetPaidBillDetails")]
        public IActionResult GetPaidBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = _service.GetPaidBillDetails(fromDate, toDate, accountName);
            return Json(data);
        }

        // ============================
        // GET INVOICE ITEMS (row expand)
        // ============================
        [HttpGet("/PaymentChecker/GetInvoiceItems")]
        [HttpGet("/PaymentChecker/GetInvoiceItemsPC")]
        public IActionResult GetInvoiceItemsPC(int vchNumber)
        {
            var data = _service.GetInvoiceItemDetails(vchNumber);
            return Json(data);
        }
        [HttpPost("/PaymentChecker/MarkVerified")]
        public IActionResult MarkVerified(int vchNumber)
        {
            _service.MarkVerified(vchNumber, CurrentUserId(), CurrentUserName());
            _log.Log(HttpContext, "Payment Checker", "Verify Payment", vchNumber);

            return Json(new
            {
                success = true,
                message = "Payment verified successfully"
            });
        }

        [HttpPost("/PaymentChecker/RejectPayment")]
        public IActionResult RejectPayment(int vchNumber, string remark)
        {
            var success = _service.RejectPayment(vchNumber, remark, CurrentUserId(), CurrentUserName());
            if (success)
                _log.Log(HttpContext, "Payment Checker", "Reject Payment", vchNumber, remark);

            return Json(new
            {
                success,
                message = success ? "Payment rejected successfully" : "Unable to reject payment"
            });
        }

        // for admin 

        // ✅ Summary Cards
        [HttpGet("/Admin/GetSummary")]
        public IActionResult GetSummary()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            //            var cmd = new SqlCommand(@"

            //DECLARE @StartDate DATE = '2026-04-01';

            //SELECT 
            //  COUNT(DISTINCT A.VchNumber) AS TotalBills,

            //    COUNT(DISTINCT CASE 
            //       WHEN AU.Status IS NULL OR AU.Status = 'Pending' 
            //        THEN A.VchNumber 
            //    END) AS PendingMaker,

            //    COUNT(DISTINCT CASE 
            //       WHEN AU.CheckerStatus = 'Approved' 
            //        THEN A.VchNumber 
            //    END) AS ApprovedChecker,

            //   COUNT(DISTINCT CASE 
            //        WHEN G.RefName IS NOT NULL 
            //        THEN A.VchNumber 
            //    END) AS TotalPaid

            //FROM PurchaseHeader A

            //LEFT JOIN AttachmentUpload AU 
            //    ON AU.VchNumber = A.VchNumber

            //LEFT JOIN RefMaster G 
            //   ON G.RefName    = A.SupplierRef
            //   AND G.AccountID = A.AccountID
            //    AND G.ToBy      = 43

            //WHERE A.VoucherDate >= @StartDate
            //           ", conn);
            var cmd = new SqlCommand("GetSummaryData", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StartDate", new DateTime(2026, 4, 1));

            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Json(new
                {
                    totalBills = reader["TotalBills"],
                    pendingMaker = reader["PendingMaker"],
                    approvedChecker = reader["ApprovedChecker"],
                    totalPaid = reader["TotalPaid"]
                });
            }
            return Json(new
            {
                totalBills = 0,
                pendingMaker = 0,
                approvedChecker = 0,
                totalPaid = 0
            });
        }

        // ✅ Shared method — used by all 3 tabs
        private List<object> FetchBillDetails(string accountName, string fromDate, string toDate)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand("GetBillDetails", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@AccountName",
                string.IsNullOrEmpty(accountName) ? DBNull.Value : (object)accountName);
            cmd.Parameters.AddWithValue("@FromDate",
                string.IsNullOrEmpty(fromDate) ? DBNull.Value : (object)DateTime.Parse(fromDate));
            cmd.Parameters.AddWithValue("@ToDate",
                string.IsNullOrEmpty(toDate) ? DBNull.Value : (object)DateTime.Parse(toDate));
            cmd.Parameters.AddWithValue("@SerialNumber", DBNull.Value);

            var reader = cmd.ExecuteReader();
            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    accountName = reader["AccountName"] == DBNull.Value ? "" : reader["AccountName"].ToString(),
                    vchNumber = reader["VchNumber"] == DBNull.Value ? "" : reader["VchNumber"].ToString(),
                    voucherDate = reader["VoucherDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                    billAmount = reader["BillAmount"] == DBNull.Value ? "" : reader["BillAmount"].ToString(),
                    supplierRef = reader["SupplierRef"] == DBNull.Value ? "-" : reader["SupplierRef"].ToString(),
                    dueDate = reader["DueDate"] == DBNull.Value ? "-" : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                    serialNumber = reader["SerialNumber"] == DBNull.Value ? "" : reader["SerialNumber"].ToString(),
                    makerRemark = reader["MakerRemark"] == DBNull.Value ? "-" : reader["MakerRemark"].ToString(),
                    makerStatus = reader["Status"] == DBNull.Value ? "Pending" : reader["Status"].ToString(),
                    checkerRemark = reader["CheckerRemark"] == DBNull.Value ? "-" : reader["CheckerRemark"].ToString(),
                    checkerStatus = reader["CheckerStatus"] == DBNull.Value ? "Pending" : reader["CheckerStatus"].ToString(),
                    paymentStatus = reader["PaymentStatus"] == DBNull.Value ? "UnPaid" : reader["PaymentStatus"].ToString(),
                    paymentDate = reader["PaymentDate"] == DBNull.Value ? "-" : Convert.ToDateTime(reader["PaymentDate"]).ToString("yyyy-MM-dd"),
                    attachment = reader["AttachmentPath"] == DBNull.Value ? "No File" : "File",
                    attachmentPath = reader["AttachmentPath"] == DBNull.Value
    ? null
    : (reader["AttachmentPath"].ToString().StartsWith("/uploads")
        ? reader["AttachmentPath"].ToString()
        : "/uploads/" + reader["AttachmentPath"].ToString())
                });
            }
            return list;
        }

        // ✅ Maker Activity Tab
        //[HttpGet]
        //public IActionResult GetMakerActivity(string accountName, string fromDate, string toDate)
        //{
        //    var data = FetchBillDetails(accountName, fromDate, toDate);
        //    return Json(data);
        //}
        [HttpGet("/Admin/GetMakerActivity")]
        public IActionResult GetMakerActivity(string accountName, string fromDate, string toDate)
        {
            var data = FetchBillDetails(accountName, fromDate, toDate);
            return Json(data);
        }
        //// ✅ Checker Activity Tab
        //[HttpGet]
        //public IActionResult GetCheckerActivity(string accountName, string fromDate, string toDate)
        //{
        //    var data = FetchBillDetails(accountName, fromDate, toDate);
        //    return Json(data);
        //}
        [HttpGet("/Admin/GetCheckerActivity")]
        public IActionResult GetCheckerActivity(string accountName, string fromDate, string toDate)
        {
            var data = FetchBillDetails(accountName, fromDate, toDate);
            return Json(data);
        }
        // ✅ Invoice Payment Tab
        [HttpGet("/Admin/GetInvoicePaymentActivity")]
        public IActionResult GetInvoicePaymentActivity(string accountName, string fromDate, string toDate)
        {
            var data = FetchBillDetails(accountName, fromDate, toDate);
            return Json(data);
        }
        [HttpGet("/Admin/GetPaymentCheckerActivity")]
        public IActionResult GetPaymentCheckerActivity(string accountName, string fromDate, string toDate)
        {
            var data = FetchBillDetails(accountName, fromDate, toDate);
            return Json(data);
        }
        [HttpPost("/Admin/RejectCheckerEntry")]
        public IActionResult RejectCheckerEntry([FromBody] AdminCheckerRejectRequest req)
        {
            if (req == null || req.VchNumber <= 0)
                return Json(new { success = false, message = "Invalid voucher number." });

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(@"
                UPDATE AttachmentUpload
                SET CheckerStatus = 'Rejected',
                    CheckerRemark = @Remark,
                    CheckerDate = GETDATE(),
                    CheckerBy = @UserId,
                    CheckerByName = @UserName
                WHERE VchNumber = @VchNumber", conn);

            cmd.Parameters.AddWithValue("@VchNumber", req.VchNumber);
            cmd.Parameters.AddWithValue("@Remark",
                string.IsNullOrWhiteSpace(req.Remark) ? DBNull.Value : req.Remark.Trim());
            cmd.Parameters.AddWithValue("@UserId", (object)CurrentUserId() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserName",
                string.IsNullOrWhiteSpace(CurrentUserName()) ? DBNull.Value : CurrentUserName());

            var affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                return Json(new { success = false, message = "No uploaded attachment record found for this voucher." });

            _log.Log(HttpContext, "Admin", "Reject Checker Entry", req.VchNumber, req.Remark);
            return Json(new { success = true, message = "Checker entry rejected successfully." });
        }
        // ✅ Delete Attachment — Admin Only
        [HttpPost("/Admin/DeleteAttachment")]
        public IActionResult DeleteAttachment([FromBody] DeleteRequest req)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            // Step 1 — Get file path from DB
            var getCmd = new SqlCommand(
                "SELECT AttachmentPath FROM AttachmentUpload WHERE VchNumber = @VchNumber", conn);
            getCmd.Parameters.AddWithValue("@VchNumber", req.VchNumber);
            var path = getCmd.ExecuteScalar()?.ToString();

            //            // Step 2 — Delete physical file from wwwroot
            if (!string.IsNullOrEmpty(path))
            {
                var fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            // Step 3 — Delete record from DB
            var delCmd = new SqlCommand(
                  "DELETE FROM AttachmentUpload WHERE VchNumber = @VchNumber", conn);
            delCmd.Parameters.AddWithValue("@VchNumber", req.VchNumber);
            delCmd.ExecuteNonQuery();

            _log.Log(HttpContext, "Admin", "Delete Attachment", req.VchNumber, null, path);
            return Json(new { success = true, message = "Attachment deleted successfully" });
        }

        // ✅ Mark Advance Payment — Admin Only
        [HttpPost("/Admin/MarkAdvancePayment")]
        public IActionResult MarkAdvancePayment([FromBody] MarkAdvancePaymentRequest req)
        {
            if (req == null || req.VchNumber <= 0)
                return Json(new { success = false, message = "Invalid voucher number." });

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(@"
                UPDATE AttachmentUpload
                SET PaymentStatus = 'Advance Payment OK',
                    PaymentDate   = GETDATE(),
                    PaidBy        = @UserId,
                    PaidByName    = @UserName
                WHERE VchNumber = @VchNumber", conn);

            cmd.Parameters.AddWithValue("@VchNumber", req.VchNumber);
            cmd.Parameters.AddWithValue("@UserId", (object)CurrentUserId() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserName",
                string.IsNullOrWhiteSpace(CurrentUserName()) ? DBNull.Value : CurrentUserName());

            var affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                return Json(new { success = false, message = "No uploaded attachment record found for this voucher." });

            _log.Log(HttpContext, "Admin", "Mark Advance Payment", req.VchNumber);
            return Json(new { success = true, message = "Marked as Advance Payment OK." });
        }

        public class MarkAdvancePaymentRequest
        {
            public decimal VchNumber { get; set; }
        }
    }

}

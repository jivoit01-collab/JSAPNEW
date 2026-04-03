using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace JSAPNEW.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _config;
        private readonly string _connStr;

        public AdminController(IConfiguration configuration)
        {
            _config = configuration;
            _connStr = _config.GetConnectionString("FHConnection");
        }

        // ✅ Admin Dashboard Page
        public IActionResult AdminPage()
        {
            return View("AdminPage");
        }

        // ✅ Summary Cards
        [HttpGet]
        public IActionResult GetSummary()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(@"
               /* SELECT 
                    COUNT(DISTINCT A.VchNumber)                                     AS TotalBills,
                    SUM(CASE WHEN AU.Status = 'Pending'          THEN 1 ELSE 0 END) AS PendingMaker,
                    SUM(CASE WHEN AU.CheckerStatus = 'Approved'  THEN 1 ELSE 0 END) AS ApprovedChecker,
                    SUM(CASE WHEN G.RefName IS NOT NULL          THEN 1 ELSE 0 END) AS TotalPaid
                FROM PurchaseHeader A 
                LEFT JOIN AttachmentUpload AU 
                    ON AU.VchNumber = A.VchNumber
                LEFT JOIN RefMaster G 
                    ON G.RefName    = A.SupplierRef
                    AND G.AccountID = A.AccountID
                    AND G.ToBy      = 43 */
DECLARE @StartDate DATE = '2026-04-01';

SELECT 
    COUNT(DISTINCT A.VchNumber) AS TotalBills,

    COUNT(DISTINCT CASE 
        WHEN AU.Status IS NULL OR AU.Status = 'Pending' 
        THEN A.VchNumber 
    END) AS PendingMaker,

    COUNT(DISTINCT CASE 
        WHEN AU.CheckerStatus = 'Approved' 
        THEN A.VchNumber 
    END) AS ApprovedChecker,

    COUNT(DISTINCT CASE 
        WHEN G.RefName IS NOT NULL 
        THEN A.VchNumber 
    END) AS TotalPaid

FROM PurchaseHeader A

LEFT JOIN AttachmentUpload AU 
    ON AU.VchNumber = A.VchNumber

LEFT JOIN RefMaster G 
    ON G.RefName    = A.SupplierRef
    AND G.AccountID = A.AccountID
    AND G.ToBy      = 43

WHERE A.VoucherDate >= @StartDate
            ", conn);

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
        [HttpGet]
        public IActionResult GetMakerActivity(string accountName, string fromDate, string toDate)
        {
            var data = FetchBillDetails(accountName, fromDate, toDate);
            return Json(data);
        }

        // ✅ Checker Activity Tab
        [HttpGet]
        public IActionResult GetCheckerActivity(string accountName, string fromDate, string toDate)
        {
            var data = FetchBillDetails(accountName, fromDate, toDate);
            return Json(data);
        }

        // ✅ Invoice Payment Tab
        [HttpGet]
        public IActionResult GetInvoiceActivity(string accountName, string fromDate, string toDate)
        {
            var data = FetchBillDetails(accountName, fromDate, toDate);
            return Json(data);
        }

        // ✅ Delete Attachment — Admin Only
        [HttpPost]
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

            // Step 2 — Delete physical file from wwwroot
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

            return Json(new { success = true, message = "Attachment deleted successfully" });
        }
    }

    // ✅ Request model for Delete
    public class DeleteRequest
    {
        public int VchNumber { get; set; }
    }
}
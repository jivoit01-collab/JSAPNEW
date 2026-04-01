using System.Data;
using System.Data.SqlClient;

namespace JSAPNEW.Services
{
    public class AdminService
    {
        private readonly IConfiguration _config;
        private readonly string _connStr;

        public AdminService(IConfiguration configuration)
        {
            _config = configuration;
            _connStr = _config.GetConnectionString("FHConnection");
        }

        // ✅ Summary Cards
        public async Task<object> GetSummaryAsync()
        {
            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            // var cmd = new SqlCommand(@"
            // SELECT 
            //    COUNT(DISTINCT A.VchNumber)                                      AS TotalBills,
            //   SUM(CASE WHEN AU.Status = 'Pending'         THEN 1 ELSE 0 END)  AS PendingMaker,
            //   SUM(CASE WHEN AU.CheckerStatus = 'Approved' THEN 1 ELSE 0 END)  AS ApprovedChecker,
            //  SUM(CASE WHEN G.RefName IS NOT NULL         THEN 1 ELSE 0 END)  AS TotalPaid
            //FROM PurchaseHeader A
            // LEFT JOIN AttachmentUpload AU 
            //     ON AU.VchNumber = A.VchNumber
            // LEFT JOIN RefMaster G 
            //   ON G.RefName    = A.SupplierRef
            //   AND G.AccountID = A.AccountID
            //  AND G.ToBy      = 43
            // ", conn);
            var cmd = new SqlCommand(@"
    /* FIX: Only April data + correct counting */

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

            var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new
                {
                    totalBills = Convert.ToInt32(reader["TotalBills"]),
                    pendingMaker = Convert.ToInt32(reader["PendingMaker"]),
                    approvedChecker = Convert.ToInt32(reader["ApprovedChecker"]),
                    totalPaid = Convert.ToInt32(reader["TotalPaid"])
                };
            }
            return new { totalBills = 0, pendingMaker = 0, approvedChecker = 0, totalPaid = 0 };
        }

        // ✅ Shared private method using GetBillDetails SP
        private async Task<List<object>> FetchBillDetails(
            string accountName, string fromDate, string toDate)
        {
            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new SqlCommand("GetBillDetails", conn); // ✅ correct SP
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@AccountName",
                string.IsNullOrEmpty(accountName) ? DBNull.Value : (object)accountName);
            cmd.Parameters.AddWithValue("@FromDate",
                string.IsNullOrEmpty(fromDate) ? DBNull.Value : (object)DateTime.Parse(fromDate));
            cmd.Parameters.AddWithValue("@ToDate",
                string.IsNullOrEmpty(toDate) ? DBNull.Value : (object)DateTime.Parse(toDate));
            cmd.Parameters.AddWithValue("@SerialNumber", DBNull.Value);

            var reader = await cmd.ExecuteReaderAsync();
            var list = new List<object>();

            while (await reader.ReadAsync())
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
                    attachmentPath = reader["AttachmentPath"] == DBNull.Value ? null : reader["AttachmentPath"].ToString()
                });
            }
            return list;
        }

        // ✅ Maker Activity
        public async Task<List<object>> GetMakerActivityAsync(
            string accountName, string fromDate, string toDate)
            => await FetchBillDetails(accountName, fromDate, toDate);

        // ✅ Checker Activity
        public async Task<List<object>> GetCheckerActivityAsync(
            string accountName, string fromDate, string toDate)
            => await FetchBillDetails(accountName, fromDate, toDate);

        // ✅ Invoice Activity
        public async Task<List<object>> GetInvoiceActivityAsync(
            string accountName, string fromDate, string toDate)
            => await FetchBillDetails(accountName, fromDate, toDate);

        // ✅ Delete Attachment
        public async Task<bool> DeleteAttachmentAsync(int vchNumber)
        {
            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            // Step 1 — Get file path
            var getCmd = new SqlCommand(
                "SELECT AttachmentPath FROM AttachmentUpload WHERE VchNumber = @VchNumber", conn);
            getCmd.Parameters.AddWithValue("@VchNumber", vchNumber);
            var path = (await getCmd.ExecuteScalarAsync())?.ToString();

            // Step 2 — Delete physical file
            if (!string.IsNullOrEmpty(path))
            {
                var fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/'));
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }

            // Step 3 — Delete from DB
            var delCmd = new SqlCommand(
                "DELETE FROM AttachmentUpload WHERE VchNumber = @VchNumber", conn);
            delCmd.Parameters.AddWithValue("@VchNumber", vchNumber);
            await delCmd.ExecuteNonQueryAsync();

            return true;
        }
    }
}
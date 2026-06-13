using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JSAPNEW.Services.Implementation
{
    public class AdminService : IAdminService
    {
        private readonly IConfiguration _config;

        public AdminService(IConfiguration config)
        {
            _config = config;
        }

        // ============================
        // GET SUMMARY — dashboard cards
        // ============================
        public BillSummaryDto GetSummary()
        {
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("GetSummaryData", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StartDate", new DateTime(2026, 4, 1));

                    conn.Open();


                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new BillSummaryDto
                            {
                                TotalBills = Convert.ToInt32(reader["TotalBills"]),
                                PendingMaker = Convert.ToInt32(reader["PendingMaker"]),
                                ApprovedChecker = Convert.ToInt32(reader["ApprovedChecker"]),
                                TotalPaid = Convert.ToInt32(reader["TotalPaid"])
                            };
                        }
                    }
                }
            }

            return new BillSummaryDto();
        }

        // ============================
        // GET BILL DETAILS — used by all 3 tabs
        // ============================
        public List<BillDetailDto> GetBillDetails(string accountName, string fromDate, string toDate)
        {
            var list = new List<BillDetailDto>();
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("GetBillDetails", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    cmd.Parameters.AddWithValue("@AccountName", string.IsNullOrEmpty(accountName) ? DBNull.Value : (object)accountName);
                    cmd.Parameters.AddWithValue("@FromDate", string.IsNullOrEmpty(fromDate) ? DBNull.Value : (object)DateTime.Parse(fromDate));
                    cmd.Parameters.AddWithValue("@ToDate", string.IsNullOrEmpty(toDate) ? DBNull.Value : (object)DateTime.Parse(toDate));
                    cmd.Parameters.AddWithValue("@SerialNumber", DBNull.Value);

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new BillDetailDto
                            {
                                AccountName = reader["AccountName"] == DBNull.Value ? "" : reader["AccountName"].ToString(),
                                VchNumber = reader["VchNumber"] == DBNull.Value ? "" : reader["VchNumber"].ToString(),
                                VoucherDate = reader["VoucherDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                                BillAmount = reader["BillAmount"] == DBNull.Value ? "" : reader["BillAmount"].ToString(),
                                SupplierRef = reader["SupplierRef"] == DBNull.Value ? "-" : reader["SupplierRef"].ToString(),
                                DueDate = reader["DueDate"] == DBNull.Value ? "-" : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                                MakerRemark = reader["MakerRemark"] == DBNull.Value ? "-" : reader["MakerRemark"].ToString(),
                                MakerStatus = reader["Status"] == DBNull.Value ? "Pending" : reader["Status"].ToString(),
                                CheckerRemark = reader["CheckerRemark"] == DBNull.Value ? "-" : reader["CheckerRemark"].ToString(),
                                CheckerStatus = reader["CheckerStatus"] == DBNull.Value ? "Pending" : reader["CheckerStatus"].ToString(),
                                PaymentStatus = reader["PaymentStatus"] == DBNull.Value ? "UnPaid" : reader["PaymentStatus"].ToString(),
                                PaymentDate = reader["PaymentDate"] == DBNull.Value ? "-" : Convert.ToDateTime(reader["PaymentDate"]).ToString("yyyy-MM-dd"),
                                AttachmentPath = reader["AttachmentPath"] == DBNull.Value
                                    ? null
                                    : (reader["AttachmentPath"].ToString().StartsWith("/uploads")
                                        ? reader["AttachmentPath"].ToString()
                                        : "/uploads/" + reader["AttachmentPath"].ToString())
                            });
                        }
                    }
                }
            }

            return list;
        }

        // ============================
        // DELETE ATTACHMENT
        // ============================
        public bool DeleteAttachment(int vchNumber, string wwwrootPath)
        {
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Step 1 — Get file path
                var getCmd = new SqlCommand(
                    "SELECT AttachmentPath FROM AttachmentUpload WHERE VchNumber = @VchNumber", conn);
                getCmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                var path = getCmd.ExecuteScalar()?.ToString();

                // Step 2 — Delete physical file
                if (!string.IsNullOrEmpty(path))
                {
                    var fullPath = Path.Combine(wwwrootPath, path.TrimStart('/'));
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }

                // Step 3 — Delete DB record
                var delCmd = new SqlCommand(
                    "DELETE FROM AttachmentUpload WHERE VchNumber = @VchNumber", conn);
                delCmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                delCmd.ExecuteNonQuery();
            }

            return true;
        }
    }
}
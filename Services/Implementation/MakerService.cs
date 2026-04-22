using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;

namespace JSAPNEW.Services.Implementation
{
    public class MakerService : IMakerService
    {
        private readonly IConfiguration _config;

        public MakerService(IConfiguration config)
        {
            _config = config;
        }

        // ============================
        // GET BILL DETAILS
        // ============================
        public List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate,
                                                  string accountName, decimal? serialNumber)
        {
            var bills = new List<BillDetailDto>();
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand("dbo.GetBillDetails", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountName", string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName.Trim());
                cmd.Parameters.AddWithValue("@SerialNumber", (object)serialNumber ?? DBNull.Value);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    bills.Add(new BillDetailDto
                    {
                        AccountName = reader["AccountName"]?.ToString(),
                        VchNumber = reader["VchNumber"],
                        VoucherDate = Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                        BillAmount = reader["BillAmount"],
                        CheckerStatus = reader["CheckerStatus"] == DBNull.Value ? "Pending" : reader["CheckerStatus"].ToString(),
                        AttachmentPath = reader["AttachmentPath"]?.ToString(),
                        MakerRemark = reader["MakerRemark"]?.ToString(),
                        TotalQuantity = reader["TotalQuantity"] != DBNull.Value ? reader["TotalQuantity"] : 0,
                        TotalItemValue = reader["TotalItemValue"] != DBNull.Value ? reader["TotalItemValue"] : 0,
                        TotalItems = reader["TotalItems"] != DBNull.Value ? reader["TotalItems"] : 0,
                        PaymentStatus = reader["PaymentStatus"]?.ToString(),
                        PaymentDate = reader["PaymentDate"] != DBNull.Value ? reader["PaymentDate"].ToString() : null,
                        SupplierRef = reader["SupplierRef"]?.ToString(),
                        SupplierRefDate = reader["SupplierRefDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),
                        DueDate = reader["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                    });
                }
            }

            return bills;
        }

        // ============================
        // GET ACCOUNT SUGGESTIONS
        // ============================
        public List<string> GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
        {
            var accounts = new List<string>();
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT DISTINCT TOP 10 F.AccountName
                    FROM PurchaseHeader A
                    INNER JOIN AccountMaster F ON F.AccountID = A.AccountID
                    WHERE
                        (@fromDate IS NULL OR CAST(A.VoucherDate AS DATE) >= @fromDate)
                        AND (@toDate IS NULL OR CAST(A.VoucherDate AS DATE) <= @toDate)
                        AND (
                            @term IS NULL
                            OR LTRIM(RTRIM(@term)) = ''
                            OR F.AccountName LIKE '%' + LTRIM(RTRIM(@term)) + '%'
                        )
                    ORDER BY F.AccountName";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@term", string.IsNullOrWhiteSpace(term) ? DBNull.Value : term.Trim());
                cmd.Parameters.AddWithValue("@fromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@toDate", (object)toDate ?? DBNull.Value);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                    accounts.Add(reader["AccountName"].ToString());
            }

            return accounts;
        }

        // ============================
        // GET INVOICE ITEM DETAILS
        // ============================
        public List<InvoiceItemDto> GetInvoiceItemDetails(int vchNumber)
        {
            var items = new List<InvoiceItemDto>();
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("GetInvoice", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    cmd.Parameters.Add("@VchNumber", SqlDbType.Decimal).Value = vchNumber;
                    cmd.Parameters.Add("@SerialNumber", SqlDbType.Decimal).Value = DBNull.Value;
                    cmd.Parameters.Add("@AccountName", SqlDbType.NVarChar).Value = DBNull.Value;
                    cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = DBNull.Value;
                    cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = DBNull.Value;

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.NextResult(); // skip header result set

                        while (reader.Read())
                        {
                            items.Add(new InvoiceItemDto
                            {
                                ProductName = reader["ProductName"]?.ToString(),
                                Quantity = reader["Quantity"],
                                Rate = reader["PurchaseCost"],
                                Tax = reader["TaxRate"],
                                Amount = reader["ItemValue"]
                            });
                        }
                    }
                }
            }

            return items;
        }
    }
}

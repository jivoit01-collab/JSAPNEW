

using Microsoft.Data.SqlClient;
using System.Data;

public class PaymentCheckerService
{
    private readonly IConfiguration _config;

    public PaymentCheckerService(IConfiguration config)
    {
        _config = config;
    }

    // ============================
    // GET PAID BILLS — GetBillDetails SP, filter Paid in C#
    // ============================
    public List<object> GetPaidBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
    {
        List<object> data = new List<object>();

        string connStr = _config.GetConnectionString("FHConnection");

        using (SqlConnection conn = new SqlConnection(connStr))
        {
            using (SqlCommand cmd = new SqlCommand("GetBillDetails", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;

                cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = (object)fromDate ?? DBNull.Value;
                cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = (object)toDate ?? DBNull.Value;
                cmd.Parameters.Add("@AccountName", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName;
                cmd.Parameters.Add("@SerialNumber", SqlDbType.Decimal).Value = DBNull.Value;

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string paymentStatus = reader["PaymentStatus"]?.ToString();

                        // Only Paid records
                        if (paymentStatus != "Paid") continue;

                        data.Add(new
                        {
                            accountName = reader["AccountName"]?.ToString(),
                            vchNumber = reader["VchNumber"],
                            voucherDate = reader["VoucherDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                            billAmount = reader["BillAmount"],
                            supplierRef = reader["SupplierRef"]?.ToString(),
                            supplierRefDate = reader["SupplierRefDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),
                            dueDate = reader["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                            paymentDate = reader["PaymentDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["PaymentDate"]).ToString("yyyy-MM-dd"),
                            attachmentPath = reader["AttachmentPath"] == DBNull.Value ? null : reader["AttachmentPath"].ToString(),
                            makerRemark = reader["MakerRemark"] == DBNull.Value ? "-" : reader["MakerRemark"].ToString(),
                            checkerRemark = reader["CheckerRemark"] == DBNull.Value ? "-" : reader["CheckerRemark"].ToString(),
                            checkerStatus = reader["CheckerStatus"] == DBNull.Value ? "-" : reader["CheckerStatus"].ToString(),
                            paymentStatus = paymentStatus
                        });
                    }
                }
            }
        }

        return data;
    }

    // ============================
    // GET INVOICE ITEMS — GetInvoice SP
    // ============================
    public List<object> GetInvoiceItemDetails(int vchNumber)
    {
        List<object> items = new List<object>();

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
                        items.Add(new
                        {
                            productName = reader["ProductName"]?.ToString(),
                            quantity = reader["Quantity"],
                            warehouseName = reader["WarehouseName"] == DBNull.Value ? "-" : reader["WarehouseName"].ToString(),
                            tax = reader["TaxRate"],
                            taxName = reader["TaxName"] == DBNull.Value ? "-" : reader["TaxName"].ToString(),
                            itemValue = reader["ItemValue"]
                        });
                    }
                }
            }
        }

        return items;
    }

    // ============================
    // ACCOUNT SUGGESTIONS — GetAccountSuggestions SP
    // ============================
    public List<string> GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
    {
        List<string> accounts = new List<string>();

        string connStr = _config.GetConnectionString("FHConnection");

        using (SqlConnection conn = new SqlConnection(connStr))
        {
            using (SqlCommand cmd = new SqlCommand("GetAccountSuggestions", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                cmd.Parameters.Add("@Term", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(term) ? DBNull.Value : term.Trim();
                cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = (object)fromDate ?? DBNull.Value;
                cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = (object)toDate ?? DBNull.Value;

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        accounts.Add(reader["AccountName"].ToString());
                }
            }
        }

        return accounts;
    }
}
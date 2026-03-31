using Microsoft.Data.SqlClient;
using System.Data;

public class MakerService
{
    private readonly IConfiguration _config;

    public MakerService(IConfiguration config)
    {
        _config = config;
    }

    public List<object> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName,
                                                                             decimal? serialNumber)
    {
        List<object> bills = new List<object>();

        string connStr = _config.GetConnectionString("FHConnection");

        using (SqlConnection conn = new SqlConnection(connStr))
        {
            SqlCommand cmd = new SqlCommand("dbo.GetBillDetails", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountName",
               string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName.Trim());
            cmd.Parameters.AddWithValue("@SerialNumber", (object)serialNumber ?? DBNull.Value);

            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                bills.Add(new
                {
                    accountName = reader["AccountName"]?.ToString(),
                    vchNumber = reader["VchNumber"],
                    voucherDate = Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                    billAmount = reader["BillAmount"],
                    // productName = reader["ProductName"]?.ToString(),
                    // quantity = reader["Quantity"],
                    //itemValue = reader["ItemValue"]

                    status = reader["Status"]?.ToString(),
                    attachmentPath = reader["AttachmentPath"]?.ToString(),
                    makerRemark = reader["MakerRemark"]?.ToString(),


                    totalQuantity = reader["TotalQuantity"] != DBNull.Value ? reader["TotalQuantity"] : 0,
                    totalItemValue = reader["TotalItemValue"] != DBNull.Value ? reader["TotalItemValue"] : 0,
                    totalItems = reader["TotalItems"] != DBNull.Value ? reader["TotalItems"] : 0,
                    paymentStatus = reader["PaymentStatus"]?.ToString(),
                    paymentDate = reader["PaymentDate"] != DBNull.Value ? reader["PaymentDate"] : null,
                    supplierRef = reader["SupplierRef"]?.ToString(),

                    supplierRefDate = reader["SupplierRefDate"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),

                    dueDate = reader["DueDate"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                });
            }
        }

        return bills;
    }

    public List<string> GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
       
    {
        List<string> accounts = new List<string>();

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

            cmd.Parameters.AddWithValue("@term",
                string.IsNullOrWhiteSpace(term) ? DBNull.Value : term.Trim());

            cmd.Parameters.AddWithValue("@fromDate", (object)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@toDate", (object)toDate ?? DBNull.Value);

            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                accounts.Add(reader["AccountName"].ToString());
            }
        }

        return accounts;
    }

    
}

using Microsoft.Data.SqlClient;
using System.Data;

public class CheckerService
{
    private readonly IConfiguration _config;

    public CheckerService(IConfiguration config)
    {
        _config = config;
    }

    public List<object> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
    {
        List<object> data = new List<object>();

        string connStr = _config.GetConnectionString("FHConnection");

        using (SqlConnection conn = new SqlConnection(connStr))
        {
            SqlCommand cmd = new SqlCommand("GetBillDetails", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountName",
                string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName);
            cmd.Parameters.AddWithValue("@SerialNumber", DBNull.Value);

            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                data.Add(new
                {
                    accountName = reader["AccountName"]?.ToString(),
                    vchNumber = reader["VchNumber"],
                    voucherDate = reader["VoucherDate"],
                    billAmount = reader["BillAmount"],
                    supplierRef = reader["SupplierRef"]?.ToString(),

                    supplierRefDate = reader["SupplierRefDate"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),

                    dueDate = reader["DueDate"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),

                    attachmentPath = reader["AttachmentPath"] == DBNull.Value ? null : reader["AttachmentPath"].ToString(),
                    makerRemark = reader["MakerRemark"] == DBNull.Value ? null : reader["MakerRemark"].ToString(),
                    checkerRemark = reader["CheckerRemark"] == DBNull.Value ? null : reader["CheckerRemark"].ToString(),

                    checkerStatus = reader["CheckerStatus"] == DBNull.Value ? null : reader["CheckerStatus"].ToString(),

                    status = reader["Status"] == DBNull.Value ? "Pending" : reader["Status"].ToString(),

                });
            }
        }

        return data;
    }
    public void UpdateCheckerStatus(int vchNumber, string status, string remark)
    {
        string connStr = _config.GetConnectionString("FHConnection");

        using (SqlConnection conn = new SqlConnection(connStr))
        {
            string query = @"
        UPDATE AttachmentUpload
        SET 
            CheckerStatus = @Status,
            CheckerRemark = @Remark,
            CheckerDate = GETDATE()
        WHERE VchNumber = @VchNumber";

            SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@VchNumber", vchNumber);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Remark",
                string.IsNullOrWhiteSpace(remark) ? DBNull.Value : remark);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
  
    }

using Microsoft.Data.SqlClient;
using System.Data;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Services.Implementation
{
    public class CheckerService : ICheckerService
    {
        private readonly IConfiguration _config;

        public CheckerService(IConfiguration config)
        {
            _config = config;
        }

        // ============================
        // GET BILL DETAILS
        // ============================
        public List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName,string status)
        {
            var data = new List<BillDetailDto>();
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand("GetBillDetails", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountName", string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName);
                cmd.Parameters.AddWithValue("@SerialNumber", DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status)? DBNull.Value: status);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new BillDetailDto
                    {
                        SerialNumber = reader["SerialNumber"]?.ToString(),

                        AccountName = reader["AccountName"]?.ToString(),
                        VchNumber = reader["VchNumber"],
                        VoucherDate = reader["VoucherDate"]?.ToString(),
                        BillAmount = reader["BillAmount"],
                        SupplierRef = reader["SupplierRef"]?.ToString(),
                        SupplierRefDate = reader["SupplierRefDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),
                        DueDate = reader["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                        AttachmentPath = reader["AttachmentPath"] == DBNull.Value ? null : reader["AttachmentPath"].ToString(),
                        MakerRemark = reader["MakerRemark"] == DBNull.Value ? null : reader["MakerRemark"].ToString(),
                        CheckerRemark = reader["CheckerRemark"] == DBNull.Value ? null : reader["CheckerRemark"].ToString(),
                        CheckerStatus = reader["CheckerStatus"] == DBNull.Value ? null : reader["CheckerStatus"].ToString(),
                        MakerStatus = reader["Status"] == DBNull.Value ? "Pending" : reader["Status"].ToString(),
                    });
                }
            }

            return data;
        }

        // ============================
        // UPDATE CHECKER STATUS
        // ============================
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
                        CheckerDate   = GETDATE()

                    WHERE VchNumber = @VchNumber";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Remark", string.IsNullOrWhiteSpace(remark) ? DBNull.Value : remark);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ============================
        // GET INVOICE ITEM DETAILS
        // ============================
        public List<InvoiceItemDto> GetInvoiceItemDetails(decimal serialNumber)
        {
            var items = new List<InvoiceItemDto>();
            string connStr = _config.GetConnectionString("FHConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("GetInvoice", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    cmd.Parameters.Add("@SerialNumber", SqlDbType.Decimal).Value = serialNumber;
          
   
                    cmd.Parameters.Add("@VchNumber", SqlDbType.Decimal).Value = DBNull.Value;
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
                                SerialNumber = reader["SerialNumber"]?.ToString(),

                                ProductName = reader["ProductName"]?.ToString(),

                                HSNSACID = reader["HSNSACID"]?.ToString(),

                                Quantity = reader["Quantity"],

                                PurchaseRate = reader["PurchaseCost"],

                                DiscountPercent = reader["DiscountPercent"],

                                DiscountAmount = reader["DiscountAmount"],

                                Margin = reader["Margin"],

                                MRP = reader["SellingRate"],

                                TaxRate = reader["TaxRate"] == DBNull.Value ? 0 : reader["TaxRate"],

                                TaxAmount = reader["TaxAmount"] == DBNull.Value ? 0 : reader["TaxAmount"],

                                Tax = reader["TaxRate"] == DBNull.Value ? 0 : reader["TaxRate"],

                                Amount = reader["ItemValue"],

                                TaxName = reader["TaxName"] == DBNull.Value ? "-" : reader["TaxName"].ToString(),

                                WarehouseName = reader["WarehouseName"] == DBNull.Value ? "-" : reader["WarehouseName"].ToString(),

                                ItemValue = reader["ItemValue"]
                            });
                        }
                    }
                }
            }

            return items;
        }
    }
}

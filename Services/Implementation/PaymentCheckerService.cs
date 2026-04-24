using Microsoft.Data.SqlClient;
using System.Data;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Services.Implementation
{
    public class PaymentCheckerService : IPaymentCheckerService
    {
        private readonly IConfiguration _config;

        public PaymentCheckerService(IConfiguration config)
        {
            _config = config;
        }

        // ============================
        // GET PAID BILL DETAILS — filter Paid in C#
        // ============================
        public List<BillDetailDto> GetPaidBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = new List<BillDetailDto>();
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

                            data.Add(new BillDetailDto
                            {
                                AccountName = reader["AccountName"]?.ToString(),
                                VchNumber = reader["VchNumber"],
                                VoucherDate = reader["VoucherDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                                BillAmount = reader["BillAmount"],
                                SupplierRef = reader["SupplierRef"]?.ToString(),
                                SupplierRefDate = reader["SupplierRefDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),
                                DueDate = reader["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                                PaymentDate = reader["PaymentDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["PaymentDate"]).ToString("yyyy-MM-dd"),
                                AttachmentPath = reader["AttachmentPath"] == DBNull.Value ? null : reader["AttachmentPath"].ToString(),
                                MakerRemark = reader["MakerRemark"] == DBNull.Value ? "-" : reader["MakerRemark"].ToString(),
                                CheckerRemark = reader["CheckerRemark"] == DBNull.Value ? "-" : reader["CheckerRemark"].ToString(),
                                CheckerStatus = reader["CheckerStatus"] == DBNull.Value ? "-" : reader["CheckerStatus"].ToString(),
                                PaymentStatus = paymentStatus
                            });
                        }
                    }
                }
            }

            return data;
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
                                WarehouseName = reader["WarehouseName"] == DBNull.Value ? "-" : reader["WarehouseName"].ToString(),
                                Tax = reader["TaxRate"],
                                TaxName = reader["TaxName"] == DBNull.Value ? "-" : reader["TaxName"].ToString(),
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
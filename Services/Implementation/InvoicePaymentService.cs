using Microsoft.Data.SqlClient;
using System.Data;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Services.Implementation
{
    public class InvoicePaymentService : IInvoicePaymentService
    {
        private readonly IConfiguration _config;

        public InvoicePaymentService(IConfiguration config)
        {
            _config = config;
        }

        // ============================
        // GET BILL DETAILS
        // ============================
        public List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate,
                                                  string accountName, decimal? serialNumber)
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
                    cmd.Parameters.Add("@SerialNumber", SqlDbType.Decimal).Value = (object)serialNumber ?? DBNull.Value;

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new BillDetailDto
                            {
                                AccountName = reader["AccountName"]?.ToString(),
                                VchNumber = reader["VchNumber"],
                                VoucherDate = reader["VoucherDate"]?.ToString(),
                                BillAmount = reader["BillAmount"],
                                AttachmentPath = reader["AttachmentPath"] == DBNull.Value ? null : reader["AttachmentPath"].ToString(),
                                MakerRemark = reader["MakerRemark"] == DBNull.Value ? null : reader["MakerRemark"].ToString(),
                                SupplierRef = reader["SupplierRef"]?.ToString(),
                                SupplierRefDate = reader["SupplierRefDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),
                                DueDate = reader["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                                CheckerStatus = reader["CheckerStatus"]?.ToString(),
                                CheckerRemark = reader["CheckerRemark"]?.ToString(),
                                PaymentStatus = reader["PaymentStatus"]?.ToString(),
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
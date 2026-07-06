using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JSAPNEW.Services.Implementation
{
    public class BillVerificationService : IBillVerificationService
    {
        private readonly string _connStr;

        public BillVerificationService(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("FHConnection");
        }

        //for maker

        public List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate,string accountName, decimal? serialNumber)
        {
            var bills = new List<BillDetailDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                SqlCommand cmd = new SqlCommand("dbo.GetBillDetails", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountName", string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName.Trim());
                cmd.Parameters.AddWithValue("@SerialNumber", (object)serialNumber ?? DBNull.Value);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                // columns the SP actually returns (so optional "who" fields don't crash if the SP wasn't updated yet)
                var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                    cols.Add(reader.GetName(i));

                while (reader.Read())
                {
                    bills.Add(new BillDetailDto
                    {
                        AccountName = reader["AccountName"]?.ToString(),
                        VchNumber = reader["VchNumber"],
                        VoucherDate = Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                        BillAmount = reader["BillAmount"],
                        CheckerStatus = reader["CheckerStatus"] == DBNull.Value ? "Pending" : reader["CheckerStatus"].ToString(),
                        CheckerRemark = cols.Contains("CheckerRemark") && reader["CheckerRemark"] != DBNull.Value ? reader["CheckerRemark"].ToString() : null,
                        CheckerDate = cols.Contains("CheckerDate") && reader["CheckerDate"] != DBNull.Value ? Convert.ToDateTime(reader["CheckerDate"]).ToString("yyyy-MM-dd HH:mm") : null,
                        CheckerBy = cols.Contains("CheckerBy") && reader["CheckerBy"] != DBNull.Value ? reader["CheckerBy"].ToString() : null,
                        CheckerName = cols.Contains("CheckerName") && reader["CheckerName"] != DBNull.Value ? reader["CheckerName"].ToString() : null,
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

        public List<string> GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate)
        {
            var accounts = new List<string>();
            using (SqlConnection conn = new SqlConnection(_connStr))
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

        public List<InvoiceItemDto> GetInvoiceItemDetails(int vchNumber)
        {
            var items = new List<InvoiceItemDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
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
                                SerialNumber = reader["SerialNumber"]?.ToString(),

                                ProductName = reader["ProductName"]?.ToString(),

                                HSNSACID = reader["HSNSACID"]?.ToString(),

                                Quantity = reader["Quantity"],

                                PurchaseRate = reader["PurchaseCost"],

                                DiscountPercent = reader["DiscountPercent"],

                                DiscountAmount = reader["DiscountAmount"],

                                Margin = reader["Margin"],

                                MRP = reader["SellingRate"],

                                Tax = reader["TaxRate"],

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

        //for checker

        // ============================
        // GET BILL DETAILS
        // ============================
        public List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, string status)
        {
            var data = new List<BillDetailDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                SqlCommand cmd = new SqlCommand("GetBillDetails", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountName", string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName);
                cmd.Parameters.AddWithValue("@SerialNumber", DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status) ? DBNull.Value : status);

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
        public void UpdateCheckerStatus(int vchNumber, string status, string remark, int? userId, string userName)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                string query = @"
                    UPDATE AttachmentUpload

                    SET
                        CheckerStatus = @Status,
                        CheckerRemark = @Remark,
                        CheckerDate   = GETDATE(),
                        CheckerBy     = @UserId,
                        CheckerByName = @UserName

                    WHERE VchNumber = @VchNumber";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Remark", string.IsNullOrWhiteSpace(remark) ? DBNull.Value : remark);
                cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(userName) ? DBNull.Value : userName);

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception(
                        $"No rows updated. Voucher={vchNumber}"
                    );
                }
            }
        }

        // ============================
        // GET INVOICE ITEM DETAILS
        // ============================
        public List<InvoiceItemDto> GetInvoiceItemDetails(decimal serialNumber)
        {
            var items = new List<InvoiceItemDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
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

        //for invoice payment
        // ============================
        // GET BILL DETAILS
        // ============================
        public List<BillDetailDto> GetBillDetailsIP(DateTime? fromDate, DateTime? toDate,
                                                  string accountName, decimal? serialNumber)
        {
            var data = new List<BillDetailDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
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
        public List<InvoiceItemDto> GetInvoiceItemDetailsIP(int vchNumber)
        {
            var items = new List<InvoiceItemDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
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
                                SerialNumber = reader["SerialNumber"]?.ToString(),

                                ProductName = reader["ProductName"]?.ToString(),

                                HSNSACID = reader["HSNSACID"]?.ToString(),

                                Quantity = reader["Quantity"],

                                PurchaseRate = reader["PurchaseCost"],

                                DiscountPercent = reader["DiscountPercent"],

                                DiscountAmount = reader["DiscountAmount"],

                                Margin = reader["Margin"],

                                MRP = reader["SellingRate"],

                                Tax = reader["TaxRate"],

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
        public bool MarkAsPaid(decimal vchNumber, int? userId, string userName)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE AttachmentUpload
                    SET
                        PaymentStatus = 'Paid',
                        PaymentDate = GETDATE(),
                        PaidBy = @UserId,
                        PaidByName = @UserName
                    WHERE VchNumber = @VchNumber
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(userName) ? DBNull.Value : userName);

                    int rows = cmd.ExecuteNonQuery();

                    return rows > 0;
                }
            }
        }

        // ============================
        // REJECT BY PAYMENT MAKER — mandatory remark, records who
        // Sends the bill BACK TO THE INVOICE MAKER: CheckerStatus='Rejected' so it
        // shows on the Maker page as Rejected, with the payment-maker's identity in
        // Checker* columns (that's what the Maker "who rejected" popup reads) and a
        // copy in Paid* columns for the payment-maker audit trail.
        // ============================
        public bool RejectByPaymentMaker(decimal vchNumber, string remark, int? userId, string userName)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                string query = @"
                    UPDATE AttachmentUpload
                    SET CheckerStatus = 'Rejected',
                        CheckerRemark = @Remark,
                        CheckerDate = GETDATE(),
                        CheckerBy = @UserId,
                        CheckerByName = @UserName,
                        PaymentStatus = 'UnPaid',
                        PaymentDate = NULL,
                        IsPaymentVerified = 0,
                        PaidBy = @UserId,
                        PaidByName = @UserName
                    WHERE VchNumber = @VchNumber";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                    cmd.Parameters.AddWithValue("@Remark",
                        string.IsNullOrWhiteSpace(remark) ? DBNull.Value : remark.Trim());
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(userName) ? DBNull.Value : userName);

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // for invoice payment checker

        // ============================
        // GET PAID BILL DETAILS — filter Paid in C#
        // ============================
        public List<BillDetailDto> GetPaidBillDetails(DateTime? fromDate, DateTime? toDate, string accountName)
        {
            var data = new List<BillDetailDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                using (SqlCommand cmd = new SqlCommand("GetBillDetails", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    // Payment Checker filters by PAYMENT DATE (done in C# below), NOT voucher date.
                    // So we do not restrict the SP by voucher date — it returns all paid bills and
                    // we keep only those whose payment date falls in the requested range.
                    cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = DBNull.Value;
                    cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = DBNull.Value;
                    cmd.Parameters.Add("@AccountName", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(accountName) ? DBNull.Value : accountName;
                    cmd.Parameters.Add("@SerialNumber", SqlDbType.Decimal).Value = DBNull.Value;

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string paymentStatus = reader["PaymentStatus"]?.ToString();
                            if (paymentStatus != "Paid") continue;

                            bool isVerified =
                                reader["IsPaymentVerified"] != DBNull.Value &&
                                Convert.ToBoolean(reader["IsPaymentVerified"]);

                            if (isVerified)
                                continue;

                            // ── filter by PAYMENT DATE (the day the payment was done) ──
                            DateTime? paymentDate = reader["PaymentDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["PaymentDate"]);

                            if (fromDate.HasValue &&
                                (!paymentDate.HasValue || paymentDate.Value.Date < fromDate.Value.Date))
                                continue;

                            if (toDate.HasValue &&
                                (!paymentDate.HasValue || paymentDate.Value.Date > toDate.Value.Date))
                                continue;

                            data.Add(new BillDetailDto
                            {
                                AccountName = reader["AccountName"]?.ToString(),
                                VchNumber = reader["VchNumber"],
                                VoucherDate = reader["VoucherDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["VoucherDate"]).ToString("yyyy-MM-dd"),
                                BillAmount = reader["BillAmount"],
                                SupplierRef = reader["SupplierRef"]?.ToString(),
                                SupplierRefDate = reader["SupplierRefDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["SupplierRefDate"]).ToString("yyyy-MM-dd"),
                                DueDate = reader["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                                PaymentDate = paymentDate?.ToString("yyyy-MM-dd"),
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
        public List<InvoiceItemDto> GetInvoiceItemDetailsPC(int vchNumber)
        {
            var items = new List<InvoiceItemDto>();
            using (SqlConnection conn = new SqlConnection(_connStr))
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

                                HSNSACID = reader["HSNSACID"]?.ToString(),

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
        public void MarkVerified(int vchNumber, int? userId, string userName)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                string query = @"
                    UPDATE AttachmentUpload
                    SET IsPaymentVerified = 1,
                        VerifiedBy = @UserId,
                        VerifiedByName = @UserName,
                        VerifiedDate = GETDATE()
                    WHERE VchNumber = @VchNumber";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(userName) ? DBNull.Value : userName);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Payment Checker reject → send the bill BACK TO THE INVOICE MAKER.
        // CheckerStatus='Rejected' + CheckerBy/CheckerByName drive the Maker's
        // "who rejected" popup; VerifiedBy/VerifiedByName keep the payment-checker audit.
        public bool RejectPayment(int vchNumber, string remark, int? userId, string userName)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                string query = @"
                    UPDATE AttachmentUpload
                    SET CheckerStatus = 'Rejected',
                        CheckerRemark = @Remark,
                        CheckerDate = GETDATE(),
                        CheckerBy = @UserId,
                        CheckerByName = @UserName,
                        PaymentStatus = 'UnPaid',
                        PaymentDate = NULL,
                        IsPaymentVerified = 0,
                        VerifiedBy = @UserId,
                        VerifiedByName = @UserName,
                        VerifiedDate = GETDATE()
                    WHERE VchNumber = @VchNumber";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VchNumber", vchNumber);
                    cmd.Parameters.AddWithValue("@Remark",
                        string.IsNullOrWhiteSpace(remark) ? DBNull.Value : remark.Trim());
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(userName) ? DBNull.Value : userName);

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // for admin

        public BillSummaryDto GetSummary()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
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
            using (SqlConnection conn = new SqlConnection(_connStr))
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
            using (SqlConnection conn = new SqlConnection(_connStr))
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

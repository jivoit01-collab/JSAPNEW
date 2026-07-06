using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Services.Implementation
{
    public class BillVerificationLogService : IBillVerificationLogService
    {
        private readonly string _connStr;

        public BillVerificationLogService(IConfiguration config)
        {
            // audit logs stored alongside the bill-verification data (FR8HODBNEW)
            _connStr = config.GetConnectionString("FHConnection");
        }

        public void Log(HttpContext ctx, string module, string action, object vchNumber, string remark = null, string details = null)
        {
            // logging must never break the actual action — swallow any error
            try
            {
                int? userId = ctx?.Session?.GetInt32("userId");
                string userName = ctx?.Session?.GetString("username");
                int? companyId = ctx?.Session?.GetInt32("selectedCompanyId");
                string ip = ctx?.Connection?.RemoteIpAddress?.ToString();

                using (var conn = new SqlConnection(_connStr))
                {
                    var cmd = new SqlCommand(@"
                        INSERT INTO BillVerificationLog
                            (Module, Action, VchNumber, Remark, Details, UserId, UserName, CompanyId, IpAddress, CreatedAt)
                        VALUES
                            (@Module, @Action, @VchNumber, @Remark, @Details, @UserId, @UserName, @CompanyId, @IpAddress, GETDATE())", conn);

                    cmd.Parameters.AddWithValue("@Module", (object)module ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Action", (object)action ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VchNumber", vchNumber == null ? DBNull.Value : (object)vchNumber.ToString());
                    cmd.Parameters.AddWithValue("@Remark", string.IsNullOrWhiteSpace(remark) ? DBNull.Value : (object)remark);
                    cmd.Parameters.AddWithValue("@Details", string.IsNullOrWhiteSpace(details) ? DBNull.Value : (object)details);
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", (object)userName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CompanyId", (object)companyId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IpAddress", (object)ip ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}

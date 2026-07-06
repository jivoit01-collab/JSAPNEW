using Microsoft.AspNetCore.Http;

namespace JSAPNEW.Services.Interfaces
{
    // Writes an audit trail of every Bill Verification action into jsap_test (DefaultConnection).
    public interface IBillVerificationLogService
    {
        void Log(HttpContext ctx, string module, string action, object vchNumber, string remark = null, string details = null);
    }
}

using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ServiceStack.Messaging;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService; //An interface for bom-related operations
        private readonly ILogger<PaymentController> _paymentlogger; //for recording events or errors
        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _paymentlogger = logger;
        }

        [HttpGet("getpendingpayments")]
        public async Task<ActionResult> GetPendingPayments(int userId, int company)
        {
            try
            {
                var pendingPayments = await _paymentService.GetPendingPaymentsAsync(userId, company);

                if (!pendingPayments.Any())
                {
                    _paymentlogger.LogInformation("No pending payments found");
                    return NotFound(new { Success = false, Message = "No pending payments found" });
                }

                var groupedPayments = pendingPayments
                    .GroupBy(p => new
                    {
                        p.DocEntry,
                        p.TransId,
                        p.CardCode,
                        p.TargetPath,
                        p.CardName,
                        p.DocNum,
                        p.DocDate,
                        p.DocType,
                        p.DocTotal,
                        p.ACCOUNT,
                        p.IFSC,
                        p.BRANCH,
                        p.Status
                    })
                    .Select(g => new
                    {
                        g.Key.DocEntry,
                        g.Key.TransId,
                        g.Key.CardCode,
                        g.Key.TargetPath,
                        g.Key.CardName,
                        g.Key.DocNum,
                        g.Key.DocDate,
                        g.Key.DocType,
                        g.Key.DocTotal,
                        g.Key.ACCOUNT,
                        g.Key.IFSC,
                        g.Key.BRANCH,
                        g.Key.Status,
                        filesData = g.Select(f => new {
                            f.FileName,
                            f.FileExt,
                            DownloadUrl = string.IsNullOrEmpty(f.TargetPath) ? null : Url.Action("DownloadFile", "File", new
                            {
                                filePath = Uri.EscapeDataString(f.TargetPath.Replace("\\", "/")),
                                fileName = Uri.EscapeDataString(f.FileName ?? ""),
                                fileExt = Uri.EscapeDataString(f.FileExt ?? "")
                            }, Request.Scheme)
                        }).ToList()  // Include all filenames
                    }).ToList();

                _paymentlogger.LogInformation("Pending payments retrieved successfully.");
                return Ok(new { Success = true, Data = groupedPayments });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "Error occurred while retrieving pending payments.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }


        [HttpGet("getapprovedpayments")]
        public async Task<ActionResult> GetApprovedPayments(int userId, int company)
        {
            try
            {
                var approvedpayments = await _paymentService.GetApprovePaymentsAsync(userId, company);

                if (!approvedpayments.Any())
                {
                    _paymentlogger.LogInformation("No approved payments found");
                    return NotFound(new { Success = false, Message = "No approved payments found" });
                }
                var groupedPayments = approvedpayments
                    .GroupBy(p => new
                    {
                        p.DocEntry,
                        p.TransId,
                        p.CardCode,
                        p.TargetPath,
                        p.CardName,
                        p.DocNum,
                        p.DocDate,
                        p.DocType,
                        p.DocTotal,
                        p.ACCOUNT,
                        p.IFSC,
                        p.BRANCH,
                        p.Status
                    })
                    .Select(g => new
                    {
                        g.Key.DocEntry,
                        g.Key.TransId,
                        g.Key.CardCode,
                        g.Key.TargetPath,
                        g.Key.CardName,
                        g.Key.DocNum,
                        g.Key.DocDate,
                        g.Key.DocType,
                        g.Key.DocTotal,
                        g.Key.ACCOUNT,
                        g.Key.IFSC,
                        g.Key.BRANCH,
                        g.Key.Status,
                        filesData = g.Select(f => new { 
                            f.FileName,
                            f.FileExt,
                            DownloadUrl = string.IsNullOrEmpty(f.TargetPath) ? null : Url.Action("DownloadFile", "File", new
                            {
                                filePath = Uri.EscapeDataString(f.TargetPath.Replace("\\", "/")),
                                fileName = Uri.EscapeDataString(f.FileName ?? ""),
                                fileExt = Uri.EscapeDataString(f.FileExt ?? "")
                            }, Request.Scheme)
                        }).ToList() // Include all filenames
                    }).ToList();

                _paymentlogger.LogInformation("approved payments retrieved successfully.");
                return Ok(new { Success = true, Data = groupedPayments });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "Error occurred while retrieving approved payments.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getrejectedpayments")]
        public async Task<ActionResult> GetRejectedPayments(int userId, int company)
        {
            try
            {
                var rejectedpayments = await _paymentService.GetRejectedPaymentsAsync(userId, company);

                if (!rejectedpayments.Any())
                {
                    _paymentlogger.LogInformation("No rejected payments found");
                    return NotFound(new { Success = false, Message = "No rejected payments found" });
                }
                var groupedPayments = rejectedpayments
                   .GroupBy(p => new
                   {
                       p.DocEntry,
                       p.TransId,
                       p.CardCode,
                       p.TargetPath,
                       p.CardName,
                       p.DocNum,
                       p.DocDate,
                       p.DocType,
                       p.DocTotal,
                       p.ACCOUNT,
                       p.IFSC,
                       p.BRANCH,
                       p.Status
                   })
                   .Select(g => new
                   {
                       g.Key.DocEntry,
                       g.Key.TransId,
                       g.Key.CardCode,
                       g.Key.TargetPath,
                       g.Key.CardName,
                       g.Key.DocNum,
                       g.Key.DocDate,
                       g.Key.DocType,
                       g.Key.DocTotal,
                       g.Key.ACCOUNT,
                       g.Key.IFSC,
                       g.Key.BRANCH,
                       g.Key.Status,
                       filesData = g.Select(f => new {
                           f.FileName,
                           f.FileExt,
                           DownloadUrl = string.IsNullOrEmpty(f.TargetPath) ? null : Url.Action("DownloadFile", "File", new
                           {
                               filePath = Uri.EscapeDataString(f.TargetPath.Replace("\\", "/")),
                               fileName = Uri.EscapeDataString(f.FileName ?? ""),
                               fileExt = Uri.EscapeDataString(f.FileExt ?? "")
                           }, Request.Scheme)
                       }).ToList()  // Include all filenames
                   }).ToList();
                _paymentlogger.LogInformation("rejected payments retrieved successfully.");
                return Ok(new { Success = true, Data = groupedPayments });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "Error occurred while retrieving rejected payments.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getallpayments")]
        public async Task<ActionResult> GetAllPayments(int userId, int company)
        {
            try
            {
                var allpayments = await _paymentService.GetAllPaymentsAsync(userId, company);

                if (!allpayments.Any())
                {
                    _paymentlogger.LogInformation("No payments found");
                    return NotFound(new { Success = false, Message = "No payments found" });
                }

                _paymentlogger.LogInformation("payments retrieved successfully.");
                return Ok(new { Success = true, Data = allpayments });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "Error occurred while retrieving payments.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("getpaymentdetails")]
        public async Task<ActionResult> GetPaymentDetails(int docEntry, int company)
        {
            try
            {
                var paymentdetails = await _paymentService.GetPaymentDetailsAsync(docEntry, company);

                if (!paymentdetails.Any())
                {
                    _paymentlogger.LogInformation("No payment details found");
                    return NotFound(new { Success = false, Message = "No payment details found" });
                }

                _paymentlogger.LogInformation("payment details retrieved successfully.");
                return Ok(new { Success = true, Data = paymentdetails });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "Error occurred while retrieving payment details.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("approvepayment")]
        public async Task<ActionResult> ApprovePayment(int paymentId, int userId)
        {
            try
            {
                var result = await _paymentService.ApprovePaymentAsync(paymentId,userId);

                // Check the result for success
                if (result == null)
                {
                    _paymentlogger.LogWarning("No response received from the database for userId: {UserId} and paymentId: {PaymentId}.", userId, paymentId);
                    return BadRequest(new { Success = false, Message = "Unable to approve the payment." });
                }

                _paymentlogger.LogInformation("Payment approved successfully for userId: {UserId} and paymentId: {PaymentId}.", userId, paymentId);
                return Ok(new { Success = true, Data = result, Message = "Payment approved successfully." });
            }
            catch (SqlException ex) when (ex.Number == 50001) // Custom SQL error for unauthorized user
            {
                _paymentlogger.LogWarning("SQL error (50001): {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "User is not authorized to approve this payment at the current stage." });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "An error occurred while approving the payment for userId: {UserId} and PaymentId: {paymentId}.", userId, paymentId);
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("rejectpayment")]
        public async Task<ActionResult> RejectPayment(int paymentId, int userId, string description)
        {
            try
            {
                var result = await _paymentService.RejectPaymentAsync(paymentId, userId, description);

                // Check the result for success
                if (result == null)
                {
                    _paymentlogger.LogWarning("No response received from the database for userId: {UserId} and paymentId: {PaymentId}.", userId, paymentId);
                    return BadRequest(new { Success = false, Message = "Unable to reject the payment." });
                }

                _paymentlogger.LogInformation("Payment rejected successfully for userId: {UserId} and paymentId: {PaymentId}.", userId, paymentId);
                return Ok(new { Success = true, Data = result, Message = "Payment rejected successfully." });
            }
            catch (SqlException ex) when (ex.Number == 50001) // Custom SQL error for unauthorized user
            {
                _paymentlogger.LogWarning("SQL error (50001): {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = "User is not authorized to reject this payment at the current stage." });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "An error occurred while approving the payment for userId: {UserId} and PaymentId: {paymentId}.", userId, paymentId);
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("getpaymentinsights")]
        public async Task<ActionResult> GetPaymentInsights(int userId, int company)
        {
            try
            {
                var payinsights = await _paymentService.GetPaymentInsightsAsync(userId, company);

                if (!payinsights.Any())
                {
                    _paymentlogger.LogInformation("No payment found");
                    return NotFound(new { Success = false, Message = "No payment found" });
                }

                _paymentlogger.LogInformation("payment retrieved successfully.");
                return Ok(new { Success = true, Data = payinsights });
            }
            catch (Exception ex)
            {
                _paymentlogger.LogError(ex, "Error occurred while retrieving payment.");
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request" });
            }
        }
    }
}

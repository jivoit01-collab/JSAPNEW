using System.Net;
using System.Text.Json;

namespace JSAPNEW.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred");

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An internal server error occurred. Please try again later."
            };

            if (exception is UnauthorizedAccessException)
            {
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "Unauthorized access.";
            }
            else if (exception is KeyNotFoundException)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = exception.Message;
            }
            else if (exception is ArgumentException)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = exception.Message;
            }

            if (_environment.IsDevelopment())
            {
                errorResponse.Details = exception.Message;
                errorResponse.StackTrace = exception.StackTrace;
            }

            var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(result);
        }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? StackTrace { get; set; }
    }
}

using System.Text.RegularExpressions;
using JSAPNEW.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JSAPNEW.Services.Implementation
{
    internal sealed class BpSapError
    {
        public int? SapCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public string ErrorCode => SapCode.HasValue ? $"SAP_{SapCode.Value}" : "SAP_POST_FAILED";
        public string RawResponse { get; init; } = string.Empty;

        public BpSapErrorInfo ToResponseInfo()
        {
            return new BpSapErrorInfo
            {
                code = SapCode,
                message = Message
            };
        }
    }

    internal sealed class BpSapException : Exception
    {
        public BpSapError SapError { get; }

        public BpSapException(string context, BpSapError sapError)
            : base(string.IsNullOrWhiteSpace(context) ? sapError.Message : $"{context}: {sapError.Message}")
        {
            SapError = sapError;
        }
    }

    internal static class BpSapErrorMapper
    {
        public static BpSapError ParseSapError(string? sapResponse, string? fallbackMessage = null)
        {
            return Map(sapResponse, fallbackMessage);
        }

        public static BpSapError ExtractSapError(Exception ex)
        {
            if (ex is BpSapException sapException)
                return sapException.SapError;

            var sapBody = FindSapErrorBody(ex);
            return !string.IsNullOrWhiteSpace(sapBody)
                ? Map(sapBody, ex.GetBaseException()?.Message ?? ex.Message)
                : Map(null, ex.GetBaseException()?.Message ?? ex.Message);
        }

        public static BpSapError Map(string? rawResponse, string? fallbackMessage = null)
        {
            var raw = rawResponse ?? string.Empty;
            var sapCode = default(int?);
            var sapMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var json = JObject.Parse(raw);
                    var error = json["error"];
                    sapCode = TryReadSapCode(error?["code"]);
                    sapMessage = ReadMessage(error?["message"])
                        ?? ReadMessage(json["message"])
                        ?? json["Message"]?.ToString()
                        ?? string.Empty;
                }
                catch (JsonException)
                {
                    sapMessage = raw;
                }
            }

            if (string.IsNullOrWhiteSpace(sapMessage))
                sapMessage = fallbackMessage ?? raw;

            if (string.IsNullOrWhiteSpace(sapMessage))
                sapMessage = "SAP returned an empty error response.";

            return new BpSapError
            {
                SapCode = sapCode,
                Message = NormalizeMessage(sapMessage),
                RawResponse = raw
            };
        }

        private static int? TryReadSapCode(JToken? token)
        {
            if (token == null)
                return null;

            if (token.Type == JTokenType.Integer && int.TryParse(token.ToString(), out var numericCode))
                return numericCode;

            var value = token.ToString();
            return int.TryParse(value, out var parsedCode) ? parsedCode : null;
        }

        private static string? ReadMessage(JToken? token)
        {
            if (token == null)
                return null;

            if (token.Type == JTokenType.Object)
            {
                return token["value"]?.ToString()
                    ?? token["Value"]?.ToString()
                    ?? token["message"]?.ToString()
                    ?? token["Message"]?.ToString();
            }

            return token.ToString();
        }

        private static string NormalizeMessage(string value)
        {
            return Regex.Replace(value.Trim(), "\\s+", " ");
        }

        private static string? FindSapErrorBody(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                var message = current.Message;
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                var start = message.IndexOf('{');
                var end = message.LastIndexOf('}');
                if (start < 0 || end <= start)
                    continue;

                var candidate = message[start..(end + 1)];
                try
                {
                    var json = JObject.Parse(candidate);
                    if (json["error"] != null || json["message"] != null || json["Message"] != null)
                        return candidate;
                }
                catch (JsonException)
                {
                    // Continue scanning inner exceptions.
                }
            }

            return null;
        }
    }
}

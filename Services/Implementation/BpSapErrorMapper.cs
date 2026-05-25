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
    }
}

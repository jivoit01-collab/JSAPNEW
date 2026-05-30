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
            return BpSapErrorMapper.BuildResponseInfo(SapCode, Message, RawResponse);
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

            var normalizedMessage = NormalizeMessage(sapMessage);
            if (!sapCode.HasValue)
                sapCode = TryReadSapCodeFromMessage(normalizedMessage);

            return new BpSapError
            {
                SapCode = sapCode,
                Message = normalizedMessage,
                RawResponse = raw
            };
        }

        public static BpSapErrorInfo BuildResponseInfo(int? sapCode, string message, string? rawResponse = null)
        {
            var normalizedMessage = NormalizeMessage(message);
            var resolvedCode = sapCode ?? TryReadSapCodeFromMessage(normalizedMessage);
            var details = AnalyzeMessage(normalizedMessage);

            return new BpSapErrorInfo
            {
                code = resolvedCode,
                message = normalizedMessage,
                field = details.Field,
                invalidValue = details.InvalidValue,
                validValues = details.ValidValues,
                reason = details.Reason,
                correctionHint = details.CorrectionHint
            };
        }

        public static BpSapErrorInfo BuildFieldErrorInfo(
            int? sapCode,
            string message,
            string field,
            string? invalidValue,
            string? rawResponse = null)
        {
            var info = BuildResponseInfo(sapCode, message, rawResponse);
            if (!string.IsNullOrWhiteSpace(field))
                info.field = NormalizeField(field);

            if (!string.IsNullOrWhiteSpace(invalidValue))
                info.invalidValue = invalidValue.Trim();

            return info;
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

        private static int? TryReadSapCodeFromMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            var explicitMatch = Regex.Match(
                message,
                @"SAP\s+Error\s+Code:\s*(?<code>-?\d+)",
                RegexOptions.IgnoreCase);

            if (explicitMatch.Success && int.TryParse(explicitMatch.Groups["code"].Value, out var explicitCode))
                return explicitCode;

            var internalMatch = Regex.Match(message, @"\((?<code>-\d+)\)");
            return internalMatch.Success && int.TryParse(internalMatch.Groups["code"].Value, out var internalCode)
                ? internalCode
                : null;
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

        private static string NormalizeField(string value)
        {
            var field = value.Trim();
            return field.StartsWith("OCRD.", StringComparison.OrdinalIgnoreCase)
                ? field["OCRD.".Length..]
                : field.StartsWith("OCRB.", StringComparison.OrdinalIgnoreCase)
                    ? field["OCRB.".Length..]
                    : field.StartsWith("BPAddresses.", StringComparison.OrdinalIgnoreCase)
                        ? field["BPAddresses.".Length..]
                        : field.StartsWith("BPBankAccounts.", StringComparison.OrdinalIgnoreCase)
                            ? field["BPBankAccounts.".Length..]
                            : field;
        }

        private static SapErrorDetails AnalyzeMessage(string message)
        {
            var details = new SapErrorDetails();
            if (string.IsNullOrWhiteSpace(message))
                return details;

            var invalidValueMatch = Regex.Match(
                message,
                @"'(?<invalid>[^']*)'\s+is\s+not\s+a\s+valid\s+value\s+for\s+property\s+'(?<field>[^']+)'",
                RegexOptions.IgnoreCase);

            if (invalidValueMatch.Success)
            {
                details.Field = invalidValueMatch.Groups["field"].Value;
                details.InvalidValue = invalidValueMatch.Groups["invalid"].Value;
                details.ValidValues = ExtractSapValidValues(message);
                details.Reason = $"Invalid SAP value for {details.Field}.";
                details.CorrectionHint = details.ValidValues.Count > 0
                    ? $"Use one of these valid SAP values: {string.Join(", ", details.ValidValues)}."
                    : $"Correct the value submitted for SAP field {details.Field}.";
                return details;
            }

            var propertyInvalidMatch = Regex.Match(
                message,
                @"Property\s+'(?<field>[^']+)'.*invalid",
                RegexOptions.IgnoreCase);

            if (propertyInvalidMatch.Success)
            {
                details.Field = propertyInvalidMatch.Groups["field"].Value;
                details.Reason = $"SAP rejected property {details.Field}.";
                details.CorrectionHint = $"Verify that {details.Field} exists in SAP and that the value sent by BP Master is allowed.";
                return details;
            }

            var fieldValueMatch = Regex.Match(
                message,
                @"'(?<invalid>[^']*)'\s+is\s+not\s+a\s+valid\s+value\s+for\s+(?:SAP\s+)?field\s+'(?<field>[^']+)'",
                RegexOptions.IgnoreCase);

            if (fieldValueMatch.Success)
            {
                details.Field = NormalizeField(fieldValueMatch.Groups["field"].Value);
                details.InvalidValue = fieldValueMatch.Groups["invalid"].Value;
                details.Reason = $"Invalid SAP value for {details.Field}.";
                details.CorrectionHint = $"Correct the value submitted for SAP field {details.Field}.";
                return details;
            }

            var explicitFieldValueMatch = Regex.Match(
                message,
                @"Field:\s*(?<field>[A-Za-z0-9_.]+)\.\s*Value:\s*'(?<invalid>[^']*)'",
                RegexOptions.IgnoreCase);

            if (explicitFieldValueMatch.Success)
            {
                details.Field = NormalizeField(explicitFieldValueMatch.Groups["field"].Value);
                details.InvalidValue = explicitFieldValueMatch.Groups["invalid"].Value;
                return details;
            }

            var bracketFieldMatch = Regex.Match(message, @"\[(?<field>[A-Za-z0-9_]+\.[A-Za-z0-9_]+)\]");
            if (bracketFieldMatch.Success)
                details.Field = NormalizeField(bracketFieldMatch.Groups["field"].Value);

            var lower = message.ToLowerInvariant();

            if (lower.Contains("debpayaacct") || lower.Contains("receivable/payable") || lower.Contains("liabilities"))
            {
                if (string.IsNullOrWhiteSpace(details.Field))
                    details.Field = "DebPayAcct";

                details.Reason = "SAP control account is missing or invalid.";
                details.CorrectionHint = "Configure a valid customer/vendor control account for this company and verify it exists in SAP OACT, is postable, and belongs to the correct drawer.";
                return details;
            }

            if (lower.Contains("branch") || lower.Contains("bplid"))
            {
                if (string.IsNullOrWhiteSpace(details.Field))
                    details.Field = "BPLId";

                details.Reason = "SAP branch value is missing or invalid.";
                details.CorrectionHint = "Use an active SAP branch value returned by the BP lookup/options API.";
                return details;
            }

            if (lower.Contains("bank") || lower.Contains("odsc"))
            {
                details.Field = string.IsNullOrWhiteSpace(details.Field) ? "BankCode" : details.Field;
                details.Reason = "SAP bank master validation failed.";
                details.CorrectionHint = "Use a bank code/name that exists in SAP bank master and verify vendor bank country, account number, IFSC, and branch values.";
                return details;
            }

            if (lower.Contains("ifsc"))
            {
                details.Field = "bankDetails.ifscCode";
                details.Reason = "IFSC code format is invalid.";
                details.CorrectionHint = "Use an 11-character IFSC code in the format ABCD0XXXXXX.";
                return details;
            }

            if (lower.Contains("no matching records found"))
            {
                details.Reason = "SAP did not find a matching master-data record.";
                details.CorrectionHint = "Use values returned by the lookup/options APIs and verify the selected value exists in SAP.";
                return details;
            }

            if (Regex.IsMatch(message, @"Internal\s+error\s+\(-?\d+\)\s+occurred", RegexOptions.IgnoreCase))
            {
                details.Reason = "SAP returned a generic internal validation error without field details.";
                details.CorrectionHint = "Check the SAP payload and master data setup for control account, branch, bank, state/country, currency, attachment, and SAP UDF/dropdown values.";
            }

            return details;
        }

        private static List<string> ExtractSapValidValues(string message)
        {
            var values = new List<string>();
            var valueSectionMatch = Regex.Match(
                message,
                @"valid\s+values\s+are:\s*(?<values>.*?)(?:\s*\(SAP\s+Error\s+Code:\s*-?\d+\))?$",
                RegexOptions.IgnoreCase);

            var source = valueSectionMatch.Success ? valueSectionMatch.Groups["values"].Value : message;
            foreach (Match match in Regex.Matches(source, @"'(?<value>[^']+)'\s*-\s*'[^']*'"))
            {
                var value = match.Groups["value"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value, StringComparer.OrdinalIgnoreCase))
                    values.Add(value);
            }

            return values;
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

        private sealed class SapErrorDetails
        {
            public string Field { get; set; } = string.Empty;
            public string InvalidValue { get; set; } = string.Empty;
            public List<string> ValidValues { get; set; } = new();
            public string Reason { get; set; } = string.Empty;
            public string CorrectionHint { get; set; } = string.Empty;
        }
    }
}

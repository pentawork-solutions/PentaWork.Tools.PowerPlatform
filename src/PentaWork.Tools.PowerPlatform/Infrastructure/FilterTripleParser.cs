using Microsoft.Xrm.Sdk.Query;

namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// Parses the CLI's "field:operator:value" filter triples (e.g. "statecode:Equal:0" or
    /// "name:In:Foo|Bar") into SDK <see cref="ConditionExpression"/> instances. Operator names match
    /// <see cref="ConditionOperator"/> member names case-insensitively; values are pipe-separated for
    /// multi-value operators (In, NotIn, Between, ...).
    /// </summary>
    internal static class FilterTripleParser
    {
        public static ConditionExpression Parse(string triple)
        {
            var parts = triple.Split(':', 3);
            if (parts.Length != 3)
                throw new CliValidationException($"Invalid filter triple '{triple}'. Expected format: field:operator:value");

            var (field, operatorName, rawValue) = (parts[0], parts[1], parts[2]);

            if (!Enum.TryParse<ConditionOperator>(operatorName, ignoreCase: true, out var conditionOperator))
                throw new CliValidationException($"Unknown condition operator '{operatorName}' in filter triple '{triple}'.");

            var values = rawValue.Split('|').Select(ParseValue).ToArray();
            return new ConditionExpression(field, conditionOperator, values);
        }

        public static List<ConditionExpression> ParseAll(IEnumerable<string> triples) => triples.Select(Parse).ToList();

        private static object ParseValue(string raw)
        {
            if (Guid.TryParse(raw, out var guid)) return guid;
            if (bool.TryParse(raw, out var boolValue)) return boolValue;
            if (long.TryParse(raw, out var longValue)) return longValue;
            if (decimal.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var decimalValue)) return decimalValue;
            if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dateValue)) return dateValue;
            return raw;
        }
    }
}

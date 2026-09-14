using System.Globalization;
using System.Text.RegularExpressions;

namespace OpdAccrRptWeb.Services;

public static partial class LegacyC11NumberConverter
{
    public static float ToSingle(decimal value) => (float)(double)value;

    public static float ToSingle(double value) => (float)value;

    public static float Subtract(float total, float withinPeriod) => (float)(total - withinPeriod);

    public static double VbVal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0d;
        Match match = NumericPrefix().Match(value.TrimStart());
        return match.Success && double.TryParse(
            match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : 0d;
    }

    [GeneratedRegex(@"^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumericPrefix();
}

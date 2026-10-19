using System.Globalization;

namespace AreaDown.Core;

/// <summary>Parses the timestamp formats accepted by Voryda Down.</summary>
public static class TimestampParser
{
    /// <summary>
    /// Accepts blank input, seconds, MM:SS, or HH:MM:SS. Decimal seconds use the
    /// invariant dot or the current culture's decimal separator.
    /// </summary>
    public static bool TryParse(string? input, out TimeSpan? timestamp)
    {
        timestamp = null;
        if (string.IsNullOrWhiteSpace(input))
            return true;

        var value = input.Trim();
        if (TimeSpan.TryParseExact(
                value,
                [@"h\:mm\:ss", @"m\:ss"],
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            if (parsed < TimeSpan.Zero)
                return false;

            timestamp = parsed;
            return true;
        }

        const NumberStyles styles = NumberStyles.AllowDecimalPoint;
        if (!double.TryParse(value, styles, CultureInfo.InvariantCulture, out var seconds) &&
            !double.TryParse(value, styles, CultureInfo.CurrentCulture, out seconds))
        {
            return false;
        }

        if (!double.IsFinite(seconds) || seconds < 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
            return false;

        try
        {
            timestamp = TimeSpan.FromSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}

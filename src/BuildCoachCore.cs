using System.Globalization;
using System.Text.RegularExpressions;

namespace WanderburgDamageHUD;

internal sealed record UpgradeChange(string Label, float Before, float After);

internal static class BuildCoachCore
{
    private static readonly Regex Comparison = new(
        @"<s>(?<before>.*?)</s>.*?<b>(?<after>.*?)</b>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex NumberWithOptionalUnit = new(
        @"^\s*(?<value>[+-]?\d+(?:[.,]\d+)?)(?:\s*[a-zA-Z%]*)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static IReadOnlyList<UpgradeChange> ParseChanges(IEnumerable<string> rawLines)
    {
        var changes = new List<UpgradeChange>();
        foreach (var raw in rawLines ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var matches = Comparison.Matches(raw);
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                if (!TryTaggedNumber(match.Groups["before"].Value, out var before) ||
                    !TryTaggedNumber(match.Groups["after"].Value, out var after)) continue;

                int suffixEnd = raw.Length;
                int nextComparison = raw.IndexOf("<s>", match.Index + match.Length, StringComparison.OrdinalIgnoreCase);
                int nextBreak = raw.IndexOf("<br", match.Index + match.Length, StringComparison.OrdinalIgnoreCase);
                if (nextComparison >= 0) suffixEnd = Math.Min(suffixEnd, nextComparison);
                if (nextBreak >= 0) suffixEnd = Math.Min(suffixEnd, nextBreak);
                string suffix = Clean(raw.Substring(match.Index + match.Length, suffixEnd - match.Index - match.Length));

                int prefixStart = 0;
                int previousBreak = raw.LastIndexOf("<br", match.Index, StringComparison.OrdinalIgnoreCase);
                if (previousBreak >= 0)
                {
                    int close = raw.IndexOf('>', previousBreak);
                    if (close >= 0 && close < match.Index) prefixStart = close + 1;
                }
                string prefix = Clean(raw.Substring(prefixStart, match.Index - prefixStart));
                string label = HasLetters(suffix) ? suffix : prefix;
                if (string.IsNullOrWhiteSpace(label)) label = "Value";
                changes.Add(new UpgradeChange(label.Trim(), before, after));
            }
        }
        return changes;
    }

    private static bool HasLetters(string value) => value.Any(char.IsLetter);
    private static bool TryNumber(string value, out float number) =>
        float.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    private static bool TryTaggedNumber(string value, out float number)
    {
        number = 0f;
        string clean = Regex.Replace(value ?? "", @"<[^>]*>", "");
        var match = NumberWithOptionalUnit.Match(clean);
        return match.Success && TryNumber(match.Groups["value"].Value, out number);
    }
    private static string Clean(string value) => Regex.Replace(Regex.Replace(value ?? "", @"<sprite[^>]*>", " "), @"<[^>]*>", " ").Trim(' ', '!', ':', '-', '·');
    private static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace WanderburgDamageHUD;

internal sealed record UpgradeChange(string Label, float Before, float After, bool LowerIsBetter, float Weight)
{
    internal float RelativeBenefit
    {
        get
        {
            if (LowerIsBetter)
                return After > 0 ? Math.Clamp(Before / After - 1f, -0.9f, 6f) : 0f;
            if (Math.Abs(Before) > 0.0001f)
                return Math.Clamp(After / Before - 1f, -0.9f, 6f);
            return After > Before ? 1f : 0f;
        }
    }
}

internal sealed record UpgradeAssessment(float UpgradeStrength, float? EstimatedBuildGain, string MainReason, bool HasUnmodelledEffect);

internal static class BuildCoachCore
{
    private static readonly Regex Comparison = new(
        @"<s>\s*(?<before>[+-]?\d+(?:[.,]\d+)?)(?<beforeUnit>\s*[a-zA-Z%]*)\s*</s>.*?<b>\s*(?<after>[+-]?\d+(?:[.,]\d+)?)(?<afterUnit>\s*[a-zA-Z%]*)\s*</b>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

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
                if (!TryNumber(match.Groups["before"].Value, out var before) ||
                    !TryNumber(match.Groups["after"].Value, out var after)) continue;

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
                var (lowerIsBetter, weight) = Classify(label);
                changes.Add(new UpgradeChange(label.Trim(), before, after, lowerIsBetter, weight));
            }
        }
        return changes;
    }

    internal static UpgradeAssessment Assess(IEnumerable<string> rawLines, float? channelShare, bool special)
    {
        var changes = ParseChanges(rawLines);
        float strength = 0f;
        UpgradeChange best = null;
        float bestContribution = float.MinValue;
        foreach (var change in changes)
        {
            float contribution = change.RelativeBenefit * change.Weight;
            strength += contribution;
            if (contribution > bestContribution)
            {
                bestContribution = contribution;
                best = change;
            }
        }

        float? buildGain = channelShare.HasValue ? Math.Max(0f, strength * Math.Clamp(channelShare.Value, 0f, 1f)) : null;
        string reason = best == null
            ? (special ? "Special effect is not modelled yet" : "No calculable stat change")
            : $"{best.Label}: {Format(best.Before)} → {Format(best.After)}";
        return new UpgradeAssessment(strength, buildGain, reason, special || changes.Count == 0);
    }

    private static (bool LowerIsBetter, float Weight) Classify(string label)
    {
        string key = label.ToUpperInvariant();
        if (ContainsAny(key, "COOLDOWN", "NACHLAD", "ABKLING")) return (true, 1f);
        if (ContainsAny(key, "DAMAGE", "SCHADEN")) return (false, 1f);
        if (ContainsAny(key, "CHARGE", "LADUNG", "AMMO", "MUNITION", "PROJECTILE", "PROJEKTIL")) return (false, .65f);
        if (ContainsAny(key, "DURATION", "DAUER")) return (false, .55f);
        if (ContainsAny(key, "SIZE", "RADIUS", "RANGE", "GRÖ", "GROE", "REICHWEITE")) return (false, .3f);
        if (ContainsAny(key, "SPEED", "GESCHW")) return (false, .3f);
        return (false, .2f);
    }

    private static bool ContainsAny(string value, params string[] needles) => needles.Any(value.Contains);
    private static bool HasLetters(string value) => value.Any(char.IsLetter);
    private static bool TryNumber(string value, out float number) =>
        float.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    private static string Clean(string value) => Regex.Replace(Regex.Replace(value ?? "", @"<sprite[^>]*>", " "), @"<[^>]*>", " ").Trim(' ', '!', ':', '-', '·');
    private static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

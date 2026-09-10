using System.Globalization;
using System.Text.RegularExpressions;

namespace WanderburgDamageHUD;

internal sealed record UpgradeChange(string Label, float Before, float After, bool LowerIsBetter, float Weight, bool Conditional)
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

internal sealed record UpgradeAssessment(float UpgradeStrength, float? EstimatedBuildGain, string MainReason, bool HasUnmodelledEffect)
{
    internal bool IsNumericallyComparable => UpgradeStrength > 0;
    internal float? ProjectedDps(float currentDps) => EstimatedBuildGain.HasValue && currentDps >= 0
        ? currentDps * (1f + EstimatedBuildGain.Value)
        : null;
}

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
                var (lowerIsBetter, weight, conditional) = Classify(label);
                changes.Add(new UpgradeChange(label.Trim(), before, after, lowerIsBetter, weight, conditional));
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
        return new UpgradeAssessment(strength, buildGain, reason, special || changes.Count == 0 || changes.Any(change => change.Conditional));
    }

    internal static float? ResolveAffectedShare(string kind, IEnumerable<string> rawLines, float active, float auto, float unknown, float total)
    {
        if (total <= 0) return null;
        bool hasChannelSplit = active + auto > 0;
        if (!hasChannelSplit)
        {
            // Without channel attribution we cannot know whether an ability cooldown
            // affects the damage that was observed for this module.
            if (kind == "Cooldown") return 0f;
            return Math.Clamp((active + auto + unknown) / total, 0f, 1f);
        }

        string text = string.Join(" ", rawLines ?? Array.Empty<string>()).ToUpperInvariant();
        float affected = kind switch
        {
            "Active" => active,
            "Auto" => auto,
            "Cooldown" when ContainsAny(text, "AUTO", "PASSIVE") => auto,
            "Cooldown" => active,
            _ => active + auto + unknown
        };
        return Math.Clamp(affected / total, 0f, 1f);
    }

    private static (bool LowerIsBetter, float Weight, bool Conditional) Classify(string label)
    {
        string key = label.ToUpperInvariant();
        if (ContainsAny(key, "COOLDOWN", "NACHLAD", "ABKLING")) return (true, .4f, true);
        if (ContainsAny(key, "DAMAGE", "SCHADEN")) return (false, 1f, false);
        if (ContainsAny(key, "CHARGE", "LADUNG", "AMMO", "MUNITION", "PROJECTILE COUNT", "PROJEKTILANZAHL")) return (false, .4f, true);
        if (ContainsAny(key, "DURATION", "DAUER")) return (false, 0f, true);
        if (ContainsAny(key, "SIZE", "RADIUS", "RANGE", "GRÖ", "GROE", "REICHWEITE")) return (false, 0f, true);
        if (ContainsAny(key, "SPEED", "GESCHW")) return (false, 0f, true);
        return (false, 0f, true);
    }

    private static bool ContainsAny(string value, params string[] needles) => needles.Any(value.Contains);
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

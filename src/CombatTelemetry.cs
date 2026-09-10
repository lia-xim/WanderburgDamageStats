using UnityEngine;

namespace WanderburgDamageHUD;

internal sealed class DamageWindow
{
    private const int WindowSeconds = 20;
    private readonly int[] seconds = Enumerable.Repeat(int.MinValue, WindowSeconds).ToArray();
    private readonly float[] active = new float[WindowSeconds];
    private readonly float[] auto = new float[WindowSeconds];
    private readonly float[] unknown = new float[WindowSeconds];

    internal float TotalActive { get; private set; }
    internal float TotalAuto { get; private set; }
    internal float TotalUnknown { get; private set; }

    internal void Add(ModuleDamageChannel channel, float damage, int second)
    {
        if (!float.IsFinite(damage) || damage <= 0) return;
        int index = Math.Abs(second % WindowSeconds);
        if (seconds[index] != second)
        {
            seconds[index] = second;
            active[index] = auto[index] = unknown[index] = 0f;
        }
        switch (channel)
        {
            case ModuleDamageChannel.Active: active[index] += damage; TotalActive += damage; break;
            case ModuleDamageChannel.AutoAttack: auto[index] += damage; TotalAuto += damage; break;
            default: unknown[index] += damage; TotalUnknown += damage; break;
        }
    }

    internal (float Active, float Auto, float Unknown) Recent(int now)
    {
        float a = 0, p = 0, u = 0;
        for (int i = 0; i < WindowSeconds; i++)
        {
            if (now - seconds[i] < 0 || now - seconds[i] >= WindowSeconds) continue;
            a += active[i]; p += auto[i]; u += unknown[i];
        }
        return (a, p, u);
    }
}

internal static class CombatTelemetry
{
    private static readonly Dictionary<string, DamageWindow> Modules = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, float> LastTotals = new(StringComparer.OrdinalIgnoreCase);
    internal static float StartedAt { get; private set; }
    internal static bool HasStarted { get; private set; }

    internal static void Reset()
    {
        Modules.Clear();
        LastTotals.Clear();
        StartedAt = Time.time;
        HasStarted = false;
        Plugin.Logger.LogInfo("Damage Stats telemetry reset for new run.");
    }

    internal static void RefreshFromGame()
    {
        var run = StatisticsQuery.CurrentRun;
        if (run == null || run.modules == null) return;
        for (int i = 0; i < run.modules.Count; i++)
        {
            var record = run.modules[i];
            if (record == null || string.IsNullOrWhiteSpace(record.moduleID) || !float.IsFinite(record.damageDealt)) continue;
            string rawId = record.moduleID;
            float current = Math.Max(0f, record.damageDealt);
            if (!LastTotals.TryGetValue(rawId, out var previous))
            {
                LastTotals[rawId] = current;
                continue;
            }
            LastTotals[rawId] = current;
            if (current < previous) continue;
            float delta = current - previous;
            if (delta <= 0) continue;
            if (!HasStarted)
            {
                StartedAt = Time.time;
                HasStarted = true;
            }
            var (baseId, channel) = Decode(rawId);
            if (!Modules.TryGetValue(baseId, out var window)) Modules[baseId] = window = new DamageWindow();
            window.Add(channel, delta, Mathf.FloorToInt(Time.time));
        }
    }

    private static (string BaseId, ModuleDamageChannel Channel) Decode(string moduleId)
    {
        const string activeSuffix = "\u001fBA:A";
        const string autoSuffix = "\u001fBA:P";
        if (moduleId.EndsWith(activeSuffix, StringComparison.Ordinal)) return (moduleId[..^activeSuffix.Length], ModuleDamageChannel.Active);
        if (moduleId.EndsWith(autoSuffix, StringComparison.Ordinal)) return (moduleId[..^autoSuffix.Length], ModuleDamageChannel.AutoAttack);
        return (moduleId, ModuleDamageChannel.Unknown);
    }

    internal static DamageWindow Find(string moduleId) =>
        !string.IsNullOrWhiteSpace(moduleId) && Modules.TryGetValue(moduleId, out var value) ? value : null;

    internal static (float Active, float Auto, float Unknown) RecentTotal()
    {
        int now = Mathf.FloorToInt(Time.time);
        float a = 0, p = 0, u = 0;
        foreach (var module in Modules.Values)
        {
            var recent = module.Recent(now);
            a += recent.Active; p += recent.Auto; u += recent.Unknown;
        }
        return (a, p, u);
    }

    internal static (float Active, float Auto, float Unknown) Recent(string moduleId) =>
        Find(moduleId)?.Recent(Mathf.FloorToInt(Time.time)) ?? (0f, 0f, 0f);

    internal static (float Active, float Auto, float Unknown) Cumulative(string moduleId)
    {
        var value = Find(moduleId);
        return value == null ? (0f, 0f, 0f) : (value.TotalActive, value.TotalAuto, value.TotalUnknown);
    }

    internal static (float Active, float Auto, float Unknown) CumulativeTotal()
    {
        float a = 0, p = 0, u = 0;
        foreach (var module in Modules.Values)
        {
            a += module.TotalActive; p += module.TotalAuto; u += module.TotalUnknown;
        }
        return (a, p, u);
    }
}

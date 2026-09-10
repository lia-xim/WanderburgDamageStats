using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace WanderburgDamageHUD;

[BepInPlugin("io.github.lia-xim.wanderburg-damage-stats", "Wanderburg Damage Stats", "0.2.0")]
public sealed class Plugin : BasePlugin
{
    internal static ManualLogSource Logger = null!;
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> Scale = null!;
    internal static ConfigEntry<float> Left = null!;
    internal static ConfigEntry<float> Top = null!;
    public override void Load()
    {
        Logger = Log;
        Enabled = Config.Bind("Anzeige", "Enabled", true, "F8 blendet die Anzeige ein/aus.");
        Scale = Config.Bind("Anzeige", "Scale", 1f, "Zusaetzlicher Skalierungsfaktor (0.7 bis 1.8).");
        Left = Config.Bind("Anzeige", "Left", 70f, "Abstand vom linken Rand im Run; im Upgrade-Menue wird der Kartenrand freigehalten.");
        Top = Config.Bind("Anzeige", "Top", 100f, "Abstand vom oberen Rand in skalierten Pixeln.");
        new Harmony("io.github.lia-xim.wanderburg-damage-stats").PatchAll();
        AddComponent<DamageOverlay>();
        Log.LogInfo("Damage Stats loaded. F8 toggles display. Damage telemetry is polled read-only; no gameplay/statistics methods are patched.");
    }
}

internal sealed record WeaponStats(float Active, float Auto, float ActiveCooldown, float AutoCooldown, float ActiveAmmo, float AutoAmmo)
{
    internal static WeaponStats Read(Module2 m) => new(
        ModuleSelection.CalculateDisplayedActiveDamage(m, m.currentActiveBaseDamage, m.flatDamageAdded, m.activeEffectPrefab),
        ModuleSelection.CalculateDisplayedPassiveDamage(m, m.currentPassiveBaseDamage, m.passiveFlatDamageAdded, m.flatDamageAdded, m.passiveAbilityPrefab),
        m.currentActiveAbilityCooldown, m.currentAutoAttackCooldown, m.activeAmmo, m.passiveAmmo);
}

internal sealed record ChoiceData(ModuleUpgradeOption Option, string Name, string ModuleId, string Kind, string Title, int Rarity, string[] RawLines, string[] Lines);

[HarmonyPatch(typeof(ModuleUpgradeOption), nameof(ModuleUpgradeOption.SetButton))]
internal static class ChoicePatch
{
    static void Postfix(ModuleUpgradeOption __instance, string moduleNameString, string upgradeTitleString, int rarity, ModuleUpgrade.UpgradeType upgradeType, Il2CppSystem.Collections.Generic.List<string> statLinesLeft, Il2CppSystem.Collections.Generic.List<string> statLinesRight)
    {
        try
        {
            var rawLines = new List<string>();
            foreach (var collection in new[] { statLinesLeft, statLinesRight })
            {
                if (collection == null) continue;
                for (int i=0;i<collection.Count;i++)
                    foreach (var line in System.Text.RegularExpressions.Regex.Split(collection[i] ?? "", @"<br\s*/?>|\r?\n"))
                        if (line.Contains("<s>") && line.Contains("<b>")) rawLines.Add(line);
            }
            string kind = upgradeType switch { ModuleUpgrade.UpgradeType.passive => "Auto", ModuleUpgrade.UpgradeType.ultimate => "Aktiv", ModuleUpgrade.UpgradeType.cooldown => "Nachladen", _ => "Spezial" };
            string moduleId = "";
            var gm = GM.gm;
            if (gm && gm.ms)
            {
                int index = __instance == gm.ms.upgradeChoice1 ? 0 : __instance == gm.ms.upgradeChoice2 ? 1 : __instance == gm.ms.upgradeChoice3 ? 2 : -1;
                if (index >= 0 && gm.ms.currentUpgradeOptions != null && index < gm.ms.currentUpgradeOptions.Count)
                {
                    var upgrade = gm.ms.currentUpgradeOptions[index];
                    if (upgrade) moduleId = upgrade.forModuleID ?? "";
                }
            }
            var distinctRaw = rawLines.Distinct().ToArray();
            var lines = distinctRaw.Select(PreviewText.Format).ToArray();
            DamageOverlay.Choices[__instance.GetInstanceID()] = new(__instance, moduleNameString, moduleId, kind, upgradeTitleString, rarity, distinctRaw, lines);
            Plugin.Logger.LogInfo($"Coach offer {moduleNameString}/{moduleId}: " + string.Join(" | ", lines));
        }
        catch (Exception ex) { Plugin.Logger.LogWarning($"Upgrade preview unavailable: {ex.Message}"); }
    }
}

[HarmonyPatch(typeof(ModuleUpgradeOption), nameof(ModuleUpgradeOption.PrepareForSelectionGeneration))]
internal static class ResetChoicePatch
{
    static void Postfix(ModuleUpgradeOption __instance) => DamageOverlay.Choices.Remove(__instance.GetInstanceID());
}

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

[BepInPlugin("io.github.lia-xim.wanderburg-damage-stats", "Wanderburg Damage Stats", Plugin.ModVersion)]
public sealed class Plugin : BasePlugin
{
    public const string ModVersion="0.6.0";
    internal static ManualLogSource Logger = null!;
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<float> Scale = null!;
    internal static ConfigEntry<float> Left = null!;
    internal static ConfigEntry<float> Top = null!;
    internal static ConfigEntry<bool> ExportCatalog = null!;
    internal static ConfigEntry<bool> FutureEnabled = null!;
    internal static ConfigEntry<int> FutureDepth = null!;
    public override void Load()
    {
        Logger = Log;
        Enabled = Config.Bind("Display", "Enabled", true, "Press F8 to show or hide the overlay.");
        Scale = Config.Bind("Display", "Scale", 1f, "Additional UI scale multiplier (0.7 to 1.8).");
        Left = Config.Bind("Display", "Left", 70f, "Distance from the left edge during a run; the upgrade screen uses its own margin.");
        Top = Config.Bind("Display", "Top", 100f, "Distance from the top edge in scaled pixels.");
        ExportCatalog = Config.Bind("Diagnostics", "ExportCatalogOnce", false, "Export loaded game assets locally for model investigation; resets after success.");
        FutureEnabled = Config.Bind("Planning", "Enabled", true, "F7 or the HUD button toggles all upgrade recommendations. DPS tracking stays enabled.");
        FutureDepth = Config.Bind("Planning", "FutureUpgrades", 5, new ConfigDescription("Number of future normal module upgrade decisions to simulate. More depth increases cost and uncertainty.",new AcceptableValueRange<int>(1,8)));
        new Harmony("io.github.lia-xim.wanderburg-damage-stats").PatchAll();
        AddComponent<DamageOverlay>();
        Log.LogInfo("Damage Stats loaded. F8 toggles display. Read-only counters and activation event observers; no combat/statistics methods patched.");
    }
}

internal sealed record WeaponStats(float Active, float Auto, float ActiveCooldown, float AutoCooldown, float ActiveAmmo, float AutoAmmo, float ActiveDuration, float AutoDuration, float ActiveSize, float AutoSize, float ActiveSpeed, float AutoSpeed)
{
    internal static WeaponStats Read(Module2 m) => new(
        ModuleSelection.CalculateDisplayedActiveDamage(m, m.currentActiveBaseDamage, m.flatDamageAdded, m.activeEffectPrefab),
        ModuleSelection.CalculateDisplayedPassiveDamage(m, m.currentPassiveBaseDamage, m.passiveFlatDamageAdded, m.flatDamageAdded, m.passiveAbilityPrefab),
        m.currentActiveAbilityCooldown, m.currentAutoAttackCooldown, m.activeAmmo, m.passiveAmmo,
        m.activeAbilityDuration, m.passiveAbilityDuration, m.activeAbilityCurrentSize, m.autoAttackCurrentSize,
        m.activeAbilityCurrentSpeed, m.autoAttackCurrentSpeed);


}

internal sealed record ChoiceData(ModuleUpgradeOption Option, int Index, string Name, string ModuleId, string Kind, string Title, int Rarity, bool HasSpecialEffect, string[] RawLines, string[] Lines);

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
                        if (!string.IsNullOrWhiteSpace(line)) rawLines.Add(line);
            }
            bool hasSpecialEffect=upgradeType is ModuleUpgrade.UpgradeType.legendary or ModuleUpgrade.UpgradeType.special;
            string kind = upgradeType switch { ModuleUpgrade.UpgradeType.passive => "Auto", ModuleUpgrade.UpgradeType.ultimate => "Active", ModuleUpgrade.UpgradeType.cooldown => "Cooldown", _ => "Special" };
            // Legendary and special cards can still target a normal damage channel.
            // The card UI exposes that channel more accurately than the broad upgrade enum.
            if(hasSpecialEffect)
            {
                if(__instance.autoElements && __instance.autoElements.activeInHierarchy) kind="Auto";
                else if(__instance.abilityElements && __instance.abilityElements.activeInHierarchy) kind="Active";
                else if(__instance.cooldownElements && __instance.cooldownElements.activeInHierarchy) kind="Cooldown";
            }
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
            int choiceIndex = gm && gm.ms ? (__instance == gm.ms.upgradeChoice1 ? 0 : __instance == gm.ms.upgradeChoice2 ? 1 : __instance == gm.ms.upgradeChoice3 ? 2 : -1) : -1;
            DamageOverlay.Choices[__instance.GetInstanceID()] = new(__instance, choiceIndex, moduleNameString, moduleId, kind, upgradeTitleString, rarity, hasSpecialEffect, distinctRaw, lines);
            Plugin.Logger.LogInfo($"Coach offer card {choiceIndex+1}, rarity {rarity}, {moduleNameString}/{moduleId}: " + string.Join(" | ", lines));
        }
        catch (Exception ex) { Plugin.Logger.LogWarning($"Upgrade preview unavailable: {ex.Message}"); }
    }
}

[HarmonyPatch(typeof(ModuleUpgradeOption), nameof(ModuleUpgradeOption.PrepareForSelectionGeneration))]
internal static class ResetChoicePatch
{
    static void Postfix(ModuleUpgradeOption __instance)
    {
        DamageOverlay.Choices.Remove(__instance.GetInstanceID());
        RuntimeFuture.NewMenu();
        RuntimeFuture.Reset();
    }
}

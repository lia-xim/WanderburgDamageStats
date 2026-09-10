using UnityEngine;

namespace WanderburgDamageHUD;

// Pure managed projection of an actual roll. Never calls UpgradeModule or changes a Unity object.
internal sealed record ProjectionState(Module2 Source, ModelProfile Profile,
    float currentActiveBaseDamage, float currentPassiveBaseDamage, float flatDamageAdded, float passiveFlatDamageAdded, float activeAmmo, float passiveAmmo, float activeAbilityDuration, float passiveAbilityDuration, float activeAbilityAddedSize, float autoAttackAddedSize, float activeAbilityAddedSpeed, float autoAttackAddedSpeed, float activeAbilityTotalAddedCooldownReduction, float autoAbilityTotalAddedCooldownReduction, GameObject activeEffectPrefab, GameObject passiveAbilityPrefab);

internal static class RuntimeProjection
{
    internal static ProjectionState Capture(Module2 m) => new(m,RuntimeModel.Read(m),
        m.currentActiveBaseDamage, m.currentPassiveBaseDamage, m.flatDamageAdded, m.passiveFlatDamageAdded, m.activeAmmo, m.passiveAmmo, m.activeAbilityDuration, m.passiveAbilityDuration, m.activeAbilityAddedSize, m.autoAttackAddedSize, m.activeAbilityAddedSpeed, m.autoAttackAddedSpeed, m.activeAbilityTotalAddedCooldownReduction, m.autoAbilityTotalAddedCooldownReduction, m.activeEffectPrefab, m.passiveAbilityPrefab);

    internal static ModelProfile Project(Module2 m,ChoiceData choice,ModuleSelection selection)
    {
        int index=choice.Index;
        if(!m || !selection || selection.currentUpgradeOptions==null || index<0 || index>=selection.currentUpgradeOptions.Count)
            throw new InvalidOperationException("Card data unavailable");
        var upgrade=selection.currentUpgradeOptions[index];
        if(!upgrade || upgrade.forModuleID!=m.moduleID || m.moduleID!=choice.ModuleId)
            throw new InvalidOperationException("Card module does not match installed weapon");
        if(selection.internalRarity==null || index>=selection.internalRarity.Length)
            throw new InvalidOperationException("Internal card rarity unavailable");
        int rarity=selection.internalRarity[index];
        var selected=new HashSet<int>();
        foreach(var collection in new[]{selection.selectedCoreAttributes,selection.selectedAdditionalAttributes})
        {
            if(collection==null || index>=collection.Count || collection[index]==null)
                throw new InvalidOperationException("Rolled attributes unavailable");
            foreach(int attribute in collection[index]) selected.Add(attribute);
        }
        return Apply(Capture(m),upgrade,rarity,selected).Profile;
    }

    internal static ProjectionState Apply(ProjectionState state,ModuleUpgrade upgrade,int rarity,HashSet<int> selected)
    {
        var m=state.Source;
        float Value(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<float> values) => NativeStatMath.Rolled(values?.ToArray(),rarity);
        float IntValue(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int> values)
        {
            if(values==null || rarity<0 || rarity>=values.Length) throw new InvalidOperationException("Invalid ammo rarity values");
            return values[rarity];
        }
        bool damage=selected.Contains(0),duration=selected.Contains(1),ammo=selected.Contains(2),size=selected.Contains(3),speed=selected.Contains(4);
        float activeBase=damage && upgrade.replacesBaseActiveDamage?upgrade.newBaseActiveDamage:state.currentActiveBaseDamage;
        float autoBase=damage && upgrade.replacesBasePassiveDamage?upgrade.newBasePassiveDamage:state.currentPassiveBaseDamage;
        float activeFlat=state.flatDamageAdded+(damage && upgrade.addsFlatDamage?Value(upgrade.addedFlatDamage):0);
        float autoFlat=state.passiveFlatDamageAdded+(damage && upgrade.addsPassiveFlatDamage?Value(upgrade.passiveFlatDamageAdded):0);
        var activeEffect=damage && upgrade.replacesActiveEffect?upgrade.replacementActiveEffect:state.activeEffectPrefab;
        var autoEffect=damage && upgrade.replacesPassiveEffect?upgrade.replacemnetPassiveEffect:state.passiveAbilityPrefab;
        float activeAmmo=state.activeAmmo+(ammo && upgrade.addsActiveAmmo?IntValue(upgrade.addedActiveAmmo):0);
        float autoAmmo=state.passiveAmmo+(ammo && upgrade.addsPassiveAmmo?Value(upgrade.addedPassiveAmmo):0);
        float activeDuration=state.activeAbilityDuration+(duration && upgrade.improvesActiveDuration?Value(upgrade.addedActiveDuration):0);
        float autoDuration=state.passiveAbilityDuration+(duration && upgrade.improvesPassiveDuration?Value(upgrade.addedPassiveDuration):0);
        float activeSize=state.activeAbilityAddedSize+(size && upgrade.addsActiveSize?Value(upgrade.addedActiveSize):0);
        float autoSize=state.autoAttackAddedSize+(size && upgrade.improvesAutoSize?Value(upgrade.addedAutoSize):0);
        float activeSpeed=state.activeAbilityAddedSpeed+(speed && upgrade.addsActiveSpeed?Value(upgrade.addedActiveSpeed):0);
        float autoSpeed=state.autoAttackAddedSpeed+(speed && upgrade.improvesAutoSpeed?Value(upgrade.addedAutoSpeed):0);
        float activeCooldown=state.activeAbilityTotalAddedCooldownReduction+(upgrade.changesCooldown?Value(upgrade.addedCooldown):0);
        float autoCooldown=state.autoAbilityTotalAddedCooldownReduction+(upgrade.changesCooldown?Value(upgrade.addedPassiveCooldown):0);
        var artifacts=ArtifactSystem.instance;
        float slot=1;
        if(artifacts) slot=m.topSlot?artifacts.topSlotCooldownFactor:m.sideSlot?artifacts.sideSlotCooldownFactor:
            m.backSlot?artifacts.backSlotCooldownFactor:m.frontSlot?artifacts.frontSlotCooldownFactor:1;
        var stats=new WeaponStats(
            ModuleSelection.CalculateDisplayedActiveDamage(m,activeBase,activeFlat,activeEffect),
            ModuleSelection.CalculateDisplayedPassiveDamage(m,autoBase,autoFlat,activeFlat,autoEffect),
            NativeStatMath.Cooldown(m.activeAbilityBaseCooldown,activeCooldown,slot,true),
            NativeStatMath.Cooldown(m.autoAbilityBaseCooldown,autoCooldown,slot,false),activeAmmo,autoAmmo,activeDuration,autoDuration,
            NativeStatMath.Scaled(m.abilityBaseSize,m.abilityMaxSize,activeSize,artifacts?artifacts.extraModuleSizeStat:0),
            NativeStatMath.Scaled(m.autoAttackBaseSize,m.autoAttackMaxSize,autoSize,artifacts?artifacts.extraModuleSizeStat:0),
            NativeStatMath.Scaled(m.abilityBaseSpeed,m.abilityMaxSpeed,activeSpeed,artifacts?artifacts.extraModuleSpeedStat:0),
            NativeStatMath.Scaled(m.autoAttackBaseSpeed,m.autoAttackMaxSpeed,autoSpeed,artifacts?artifacts.extraModuleSpeedStat:0));
        var before=state.Profile;
        var uncertain=before.Unknowns.ToList();
        if(upgrade.legendaryA || upgrade.legendaryB || upgrade.special0A || upgrade.special0B || upgrade.special1A || upgrade.special1B || upgrade.replacesAlternativeEffect || upgrade.replacesLegendaryActivePrefab)
            uncertain.Add("special behavior / future synergies not fully modeled");
        var ram=before.Ram==null?null:before.Ram with {Multiplier=autoAmmo,Flat=autoFlat,FuryMultiplier=activeBase+activeFlat,
            Duration=activeDuration,Cooldown=stats.ActiveCooldown,Speed=stats.ActiveSpeed};
        var profile=new ModelProfile(RuntimeModel.ReadChannel(m,stats,true,activeEffect),RuntimeModel.ReadChannel(m,stats,false,autoEffect),ram,uncertain.ToArray());
        return new(m,profile,activeBase, autoBase, activeFlat, autoFlat, activeAmmo, autoAmmo, activeDuration, autoDuration, activeSize, autoSize, activeSpeed, autoSpeed, activeCooldown, autoCooldown, activeEffect, autoEffect);
    }
}

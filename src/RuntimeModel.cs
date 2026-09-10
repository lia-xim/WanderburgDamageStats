using UnityEngine;

namespace WanderburgDamageHUD;

internal static class RuntimeModel
{
    internal static string Describe(Module2 m)
    {
        return Describe(Read(m),m.moduleID);
    }
    internal static string Describe(ModelProfile p,string moduleId)
    {
        return System.Text.Json.JsonSerializer.Serialize(new { id=moduleId,active=p.Active,auto=p.Auto,
            ram=p.Ram==null?null:new { p.Ram.Balance,p.Ram.Multiplier,p.Ram.Flat,p.Ram.FuryMultiplier,p.Ram.Duration,p.Ram.Cooldown,p.Ram.Speed },
            uncertain=p.Unknowns });
    }
    private sealed record Sample(float Time,float Speed,bool Fury);
    private static readonly Dictionary<string,List<Sample>> Movement=new();
    private sealed record Subscription(Module2 Module,Il2CppSystem.Action Active,Il2CppSystem.Action Auto);
    private static readonly Dictionary<int,Subscription> Subscriptions=new();
    private static readonly Dictionary<string,List<(float Time,bool Active)>> Activations=new();
    private static float lastSample=-1;
    internal static void Reset()
    {
        foreach(var s in Subscriptions.Values)
            if(s.Module) { s.Module.remove_OnActivationFire(s.Active); s.Module.remove_OnPassiveTick(s.Auto); }
        Subscriptions.Clear(); Activations.Clear(); Movement.Clear(); lastSample=-1;
    }

    private static void Record(string id,bool active)
    {
        if(!Activations.TryGetValue(id,out var events)) Activations[id]=events=new();
        events.Add((Time.time,active));
        events.RemoveAll(e=>Time.time-e.Time>20);
    }

    internal static void Observe(GM gm)
    {
        if(gm.upgradeMenuOpen || gm.mainMenuOpen || Time.time<=lastSample) return;
        lastSample=Time.time;
        foreach(var m in gm.vm.allModules2)
        {
            if(!m || !m.IsInstalled || !m.IsMountedOnVehicle(gm.vm)) continue;
            int key=m.GetInstanceID();
            if(!Subscriptions.ContainsKey(key))
            {
                string id=m.moduleID;
                var active=Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Action>((Action)(()=>Record(id,true)));
                var auto=Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Action>((Action)(()=>Record(id,false)));
                m.add_OnActivationFire(active); m.add_OnPassiveTick(auto);
                Subscriptions[key]=new(m,active,auto);
            }
            var ram=m.GetComponent<Module2Ramme>();
            if(!ram || !gm.vm.rb) continue;
            if(!Movement.TryGetValue(m.moduleID,out var samples)) Movement[m.moduleID]=samples=new();
            samples.Add(new(Time.time,gm.vm.rb.linearVelocity.magnitude,ram.furyMode));
            samples.RemoveAll(s=>Time.time-s.Time>20);
            if(samples.Count>160) samples.RemoveRange(0,samples.Count-160);
        }
    }

    internal static IReadOnlyList<RamSample> Samples(string id)
    {
        if(!Movement.TryGetValue(id,out var samples)) return Array.Empty<RamSample>();
        return samples.Select((s,i)=>new RamSample(s.Speed,s.Fury,i==0?.25f:Math.Clamp(s.Time-samples[i-1].Time,0,.5f))).ToArray();
    }

    internal static ModelProfile Read(Module2 m)
    {
        var w=WeaponStats.Read(m);
        var ram=m.GetComponent<Module2Ramme>();
        var unresolved=new List<string>();
        if(m.legendaryA || m.legendaryB || m.special0A || m.special0B || m.special1A || m.special1B)
            unresolved.Add("module special / legendary behavior requires an individual model");
        RamProfile ramStats=null;
        if(ram)
        {
            var curve=ram.damageOverSpeedCurve;
            float slot=ArtifactSystem.instance?ArtifactSystem.instance.frontSlotDamageFactor:1;
            ramStats=new(ram.generalBalanceDamageMult,m.passiveAmmo,m.passiveFlatDamageAdded,
                m.currentActiveBaseDamage+m.flatDamageAdded,m.activeAbilityDuration,m.currentActiveAbilityCooldown,
                m.activeAbilityCurrentSpeed,slot,speed=>curve.Evaluate(speed));
            // The native Ram path applies these conditional bonuses after its core formula.
            var artifacts=ArtifactSystem.instance;
            if(artifacts && (artifacts.extraMeleeDamageToFullHealth!=1 || artifacts.extraMeleeSpeedDamageFactor!=0))
                unresolved.Add("conditional melee artifacts / target health held constant");
        }
        return new(ReadChannel(m,w,true),ReadChannel(m,w,false),ramStats,unresolved.ToArray());
    }

    internal static AttackProfile ReadChannel(Module2 m,WeaponStats w,bool active) => ReadChannel(m,w,active,active?m.activeEffectPrefab:m.passiveAbilityPrefab);

    internal static AttackProfile ReadChannel(Module2 m,WeaponStats w,bool active,GameObject prefab)
    {
        float damage=active?w.Active:w.Auto;
        float count=active?w.ActiveAmmo:w.AutoAmmo;
        float duration=active?w.ActiveDuration:w.AutoDuration;
        float cooldown=active?w.ActiveCooldown:w.AutoCooldown;
        float tick=0,interval=0,unitInterval=0,secondary=0;
        var model=AttackModel.Unknown;
        var unknown=new List<string>();
        string effect=prefab?prefab.name:"none";
        var projectile=prefab?prefab.GetComponentInChildren<ProjectileV2>(true):null;
        var flame=prefab?prefab.GetComponentInChildren<FlameZone>(true):null;
        var zone=prefab?prefab.GetComponentInChildren<DamageZone2>(true):null;
        var unit=prefab?prefab.GetComponentInChildren<AgentUnit>(true):null;
        var laser=m.GetComponent<Module2LaserMage>();
        if(unit)
        {
            model=AttackModel.Summon;
            unitInterval=unit.attackCooldown+unit.attackChargeTime;
            var barracks=m.GetComponent<Module2Barracks>();
            if(barracks)
            {
                float spawnFactor=ArtifactSystem.instance?ArtifactSystem.instance.unitSpawnFactor:1;
                if(!active)
                {
                    model=AttackModel.PersistentArmy;
                    count=Math.Max(0,Mathf.FloorToInt(Math.Max(0,Mathf.RoundToInt(count))*spawnFactor))+(m.special0A?5:0);
                    unknown.Add("standing army: same survival and target access assumed; cooldown only replenishes losses");
                }
                else
                {
                    int points=0;
                    if(barracks.instantiationPoints!=null) foreach(var point in barracks.instantiationPoints)
                        if(point && point.gameObject.activeInHierarchy) points++;
                    count=Math.Max(0,Mathf.FloorToInt(count*spawnFactor))*points;
                    unknown.Add("temporary reinforcements: exposure and staggered spawn timing estimated");
                }
                unknown.Add("ability buffs existing allies; movement and attack-speed buff effects not quantified");
            }
            else
            {
                unknown.Add("summon lifetime, replacement cap and targeting estimated");
                if(duration<=0) unknown.Add("persistent unit lifetime unavailable");
            }
        }
        else if(flame || zone || laser)
        {
            model=AttackModel.Ticks;
            tick=flame?flame.damageTick:zone?zone.tickInterval:laser.damageTickInterval;
            count=Math.Max(1,count);
            unknown.Add("tick exposure and module-to-effect stat mapping estimated");
            if(flame && flame.fireDuration>0) unknown.Add("burn refresh / resistance not quantified");
            if(zone && zone.dealPercentageHealthDamage) unknown.Add("percentage-health damage needs target HP");
            if(zone && zone.dealDamageOnEnterZone) unknown.Add("zone entry hits depend on movement");
        }
        else if(projectile)
        {
            model=AttackModel.Projectile;
            // Count can be zero on utility modules; do not silently promote it to one.
            if(!m.GetComponent<Module2CrewArcher>() && !m.GetComponent<Module2CrewFire>()) unknown.Add("module volley count / timing estimated");
            if(projectile.passThroughEnemies || projectile.aoeDamageRadius>0)
                unknown.Add("piercing / area targets held constant");
            if(projectile.setsTargetsOnFire) unknown.Add("burn damage / refresh not quantified");
            if(projectile.slowsTraget || projectile.stunsTargets || projectile.fearsTarget || projectile.knockBackEffect)
                unknown.Add("crowd control and artifact synergies not quantified");
            if(projectile.execute) unknown.Add("execute needs target HP");
            if(projectile.afterEffectChainLightning || projectile.afterEffectArrowRain || projectile.afterEffectMortarRain ||
               projectile.afterEffectCannonBallStar || projectile.afterEffectConeCannonBall || projectile.afterEffectAtferExplosion ||
               projectile.afterEffectLightningStrike || projectile.afterEffectFlameZone || projectile.afterEffectFrostField)
                unknown.Add("authored proc values: trigger, overrides and target coverage estimated");
            var procs=new List<ProcProfile>();
            if(projectile.afterEffectChainLightning) procs.Add(new(projectile.afterEffectChanceToTriggerChainLightning,projectile.chainLightningDamage,1));
            if(projectile.afterEffectArrowRain) procs.Add(new(projectile.afterEffectChanceToTriggerArrowRain,projectile.arrowRainDamage,projectile.arrowRainProjectileCount));
            if(projectile.afterEffectMortarRain) procs.Add(new(projectile.afterEffectChanceToTriggerMortarRain,projectile.mortarRainDamage,projectile.mortarRainProjectileCount));
            if(projectile.afterEffectCannonBallStar) procs.Add(new(projectile.afterEffectChanceToTriggerCannonBallStar,projectile.cannonBallStarDamage,projectile.cannonBallStarProjectileCount));
            if(projectile.afterEffectConeCannonBall) procs.Add(new(projectile.afterEffectChanceToTriggerCannonBallCone,projectile.coneCannonBallDamage,projectile.coneCannonBallProjectileCount));
            if(projectile.afterEffectAtferExplosion) procs.Add(new(projectile.afterEffectChanceToTriggerAfterExplosion,projectile.afterExplosionDamage,1));
            if(projectile.afterEffectLightningStrike) procs.Add(new(projectile.afterEffectChanceToTriggerLightningStrike,projectile.lightningStrikeDamage,1));
            secondary=PredictionModel.ProcDamage(procs);
            effect+=$":{projectile.myProjectileType}:{projectile.passThroughEnemies}:{projectile.aoeDamageRadius}:{projectile.fireDuration}:{projectile.slowDuration}:{projectile.execute}:{secondary}";
        }
        else unknown.Add("weapon effect has no verified damage adapter");

        var topArcher=m.GetComponent<Module2TopArcher>();
        if(m.GetComponent<Module2CrewFire>() && !active && count>0)
        {
            // FirePassiveProcess divides passive duration by ammo between consecutive shots.
            interval=duration/count;
            unknown.Add("duration also changes burn time; burn refresh is not quantified");
        }
        if(topArcher && active) { interval=topArcher.activeProjectileInterval; unknown.Add("archer volley / bonus projectiles need runtime confirmation"); }
        var flamethrower=m.GetComponent<Module2_SideFlamethrower>();
        if(flamethrower && !active) interval=flamethrower.passiveVolleyInterval;
        return new(model,damage,count,cooldown,duration,tick,interval,unitInterval,
            active?w.ActiveSize:w.AutoSize,active?w.ActiveSpeed:w.AutoSpeed,secondary,effect,unknown.ToArray());
    }

    internal static Prediction Assess(Module2 current,ModelProfile projected,string moduleId,float seconds)
    {
        if(!current || projected==null)
            return Prediction.Unavailable("Matching game preview unavailable");
        return PredictionModel.Compare(Read(current),projected,Evidence(moduleId,seconds),Samples(moduleId));
    }

    internal static CombatEvidence Evidence(string moduleId,float seconds)
    {
        var total=CombatTelemetry.RecentTotal();
        var observed=CombatTelemetry.Recent(moduleId);
        var events=Activations.TryGetValue(moduleId,out var uses)?uses.Where(e=>Time.time-e.Time<=20).ToArray():Array.Empty<(float Time,bool Active)>();
        return new CombatEvidence(observed.Active,observed.Auto,observed.Unknown,total.Active+total.Auto+total.Unknown,seconds,
            events.Count(e=>e.Active),events.Count(e=>!e.Active));
    }
}

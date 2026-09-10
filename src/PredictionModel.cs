namespace WanderburgDamageHUD;

internal enum AttackModel { Projectile, Ticks, Summon, Unknown, PersistentArmy }

// Values describe one attack channel, not translated card labels.
internal sealed record AttackProfile(
    AttackModel Model, float Damage, float Count, float Cooldown, float Duration,
    float TickInterval, float VolleyInterval, float UnitAttackInterval,
    float Size, float Speed, float SecondaryDamage, string EffectKey,
    string[] Unknowns);

internal sealed record RamProfile(float Balance, float Multiplier, float Flat, float FuryMultiplier,
    float Duration, float Cooldown, float Speed, float SlotMultiplier, Func<float,float> Curve);
internal sealed record RamSample(float Speed, bool Fury, float Seconds);
internal sealed record CombatEvidence(float Active, float Auto, float Unknown, float Total,
    float Seconds, float ActiveUses, float AutoUses);
internal sealed record ModelProfile(AttackProfile Active, AttackProfile Auto, RamProfile Ram, string[] Unknowns);
internal sealed record ProcProfile(float Chance,float Damage,float Projectiles,float Targets=1);
internal sealed record Prediction(float? Gain, float? Low, float? High, string Reason, string[] Unknowns)
{
    internal bool CanRank => Gain.HasValue && float.IsFinite(Gain.Value);
    internal bool Complete => CanRank && Unknowns.Length == 0;
    internal static Prediction Unavailable(string reason) => new(null,null,null,reason,new[]{reason});
}

internal static class PredictionModel
{
    internal static bool Valid(float x) => float.IsFinite(x) && x >= 0;
    private static string F(float x) => x.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);

    // Expected extra damage, conditional on the authored trigger and target assumptions.
    internal static float ProcDamage(IEnumerable<ProcProfile> effects) => effects.Sum(p=>
        Valid(p.Chance) && Valid(p.Damage) && Valid(p.Projectiles) && Valid(p.Targets)
            ? Math.Clamp(p.Chance,0,1)*p.Damage*p.Projectiles*p.Targets : float.NaN);

    private static bool SameAttack(AttackProfile a,AttackProfile b) => a is not null && b is not null &&
        a.Model==b.Model && a.Damage==b.Damage && a.Count==b.Count && a.Cooldown==b.Cooldown &&
        a.Duration==b.Duration && a.TickInterval==b.TickInterval && a.VolleyInterval==b.VolleyInterval &&
        a.UnitAttackInterval==b.UnitAttackInterval && a.Size==b.Size && a.Speed==b.Speed &&
        a.SecondaryDamage==b.SecondaryDamage && a.EffectKey==b.EffectKey;

    // Damage per attack: independent projectiles, a timed series of ticks, or living units.
    // Duration is intentionally absent from the projectile formula.
    internal static float AttackDamage(AttackProfile p)
    {
        if (!Valid(p.Damage) || !Valid(p.Count) || !Valid(p.SecondaryDamage)) return float.NaN;
        float hits = p.Model switch
        {
            AttackModel.Projectile => p.Count,
            AttackModel.Ticks when p.TickInterval > 0 && Valid(p.Duration) => p.Count * p.Duration / p.TickInterval,
            AttackModel.Summon when p.UnitAttackInterval > 0 && Valid(p.Duration) => p.Count * p.Duration / p.UnitAttackInterval,
            AttackModel.PersistentArmy when p.UnitAttackInterval > 0 => p.Count / p.UnitAttackInterval,
            _ => float.NaN
        };
        return hits * (p.Damage + p.SecondaryDamage);
    }

    // Preserve observed unused time between attacks. No arbitrary 0.4/0.8 exponents.
    // Both cooldown-start conventions form a sensitivity range when a volley overlaps recharge.
    internal static float FrequencyRatio(AttackProfile before, AttackProfile after, float uses, float seconds, bool rechargeAfterAttack)
    {
        // The passive Barracks cooldown replenishes casualties; it does not respawn a full army each cycle.
        if(before.Model==AttackModel.PersistentArmy && after.Model==AttackModel.PersistentArmy) return 1;
        if (before.Cooldown <= 0 || after.Cooldown <= 0) return float.NaN;
        float oldBusy = Busy(before), newBusy = Busy(after);
        float oldCycle = rechargeAfterAttack ? before.Cooldown + oldBusy : Math.Max(before.Cooldown,oldBusy);
        float newCycle = rechargeAfterAttack ? after.Cooldown + newBusy : Math.Max(after.Cooldown,newBusy);
        if (oldCycle <= 0 || newCycle <= 0) return float.NaN;
        float idle = uses >= 2 && seconds > 0 ? Math.Max(0,seconds / uses - oldCycle) : 0;
        return (oldCycle + idle) / (newCycle + idle);
    }

    private static float Busy(AttackProfile p) => p.Model == AttackModel.Ticks ? Math.Max(0,p.Duration)
        : p.Model == AttackModel.Projectile ? Math.Max(0,p.Count-1)*Math.Max(0,p.VolleyInterval) : 0;

    internal static Prediction Compare(ModelProfile before, ModelProfile after, CombatEvidence evidence, IReadOnlyList<RamSample> samples)
    {
        if (before == null || after == null) return Prediction.Unavailable("Matching game preview unavailable");
        if (!Valid(evidence.Total) || evidence.Total <= 0) return Prediction.Unavailable("Waiting for representative combat data");
        if(!Valid(evidence.Active) || !Valid(evidence.Auto) || !Valid(evidence.Unknown) ||
            evidence.Active+evidence.Auto+evidence.Unknown>evidence.Total+.01f)
            return Prediction.Unavailable("Combat attribution inconsistent");
        var unknowns = new List<string>(before.Unknowns.Concat(after.Unknowns));
        if (evidence.Seconds < 10) unknowns.Add("short combat sample");
        if (before.Ram != null && after.Ram != null)
            return CompareRam(before.Ram,after.Ram,evidence,samples,unknowns);
        if (evidence.Unknown > 0)
            return CompareAggregate(before,after,evidence,samples);

        float gain=0,low=0,high=0;
        bool comparable=true;
        var reasons=new List<string>();
        foreach (var entry in new[]{(before.Active,after.Active,evidence.Active,evidence.ActiveUses,"ability"),
                                    (before.Auto,after.Auto,evidence.Auto,evidence.AutoUses,"auto")})
        {
            var (a,b,dealt,uses,label)=entry;
            if (SameAttack(a,b)) continue;
            if (a is null || b is null) { comparable=false; unknowns.Add(label+" effect not resolved"); continue; }
            unknowns.AddRange(a.Unknowns.Concat(b.Unknowns));
            if (a.Model != b.Model || a.EffectKey != b.EffectKey)
                unknowns.Add(label+" changes attack behavior");
            float oldDamage=AttackDamage(a),newDamage=AttackDamage(b);
            bool outputChanged = oldDamage != newDamage || a.Cooldown != b.Cooldown || Busy(a) != Busy(b);
            if (a.Size != b.Size) unknowns.Add(label+" area / target coverage changes");
            if (a.Speed != b.Speed) unknowns.Add(label+" travel / hit chance changes");
            if (!outputChanged) continue;
            if (!(oldDamage > 0) || !Valid(newDamage))
            { comparable=false; unknowns.Add(label+" new or unsupported damage source"); continue; }
            if (dealt <= 0)
            {
                unknowns.Add(label+" has no recent attributed damage; future use may differ");
                continue;
            }
            float frequency=FrequencyRatio(a,b,uses,evidence.Seconds,false);
            float alternate=FrequencyRatio(a,b,uses,evidence.Seconds,true);
            if (!Valid(frequency) || !Valid(alternate))
            { comparable=false; unknowns.Add(label+" attack timing unresolved"); continue; }
            float damageRatio=newDamage/oldDamage;
            float contribution=dealt/evidence.Total;
            float factor=damageRatio*frequency;
            float lo=Math.Min(factor,damageRatio*alternate),hi=Math.Max(factor,damageRatio*alternate);
            if (frequency != 1 || alternate != 1)
            {
                unknowns.Add(label+" cooldown utilization / overlap estimated");
                if(uses<2) unknowns.Add(label+" too few activations to measure idle time");
                lo=Math.Min(lo,damageRatio); hi=Math.Max(hi,damageRatio);
            }
            if (a.Model is AttackModel.Ticks or AttackModel.Summon)
            {
                unknowns.Add(label+" target exposure / unit lifetime estimated");
                // Existing per-hit damage scaling is the no-extra-exposure scenario.
                if (a.Damage > 0) { lo=Math.Min(lo,b.Damage/a.Damage); hi=Math.Max(hi,b.Damage/a.Damage); }
            }
            gain+=contribution*(factor-1); low+=contribution*(lo-1); high+=contribution*(hi-1);
            reasons.Add($"{label}: hit/volley ×{F(damageRatio)}, rate ×{F(frequency)}");
        }
        if (!comparable) return new(null,null,null,string.Join("; ",reasons.DefaultIfEmpty("Incomplete weapon model")),unknowns.Distinct().ToArray());
        return new(gain,low,high,string.Join("; ",reasons.DefaultIfEmpty("No modeled direct DPS change")),unknowns.Distinct().ToArray());
    }

    // Native statistics commonly contain only a module total. Infer a conditional split
    // from attack output and observed triggers instead of requiring nonexistent counters.
    // Alternate allocations expose sensitivity to unknown hit rates and target coverage.
    private static Prediction CompareAggregate(ModelProfile before,ModelProfile after,CombatEvidence e,IReadOnlyList<RamSample> samples)
    {
        float Weight(AttackProfile p,float uses)
        {
            if(p==null) return float.NaN;
            float damage=AttackDamage(p);
            if(!Valid(damage)) return float.NaN;
            if(damage==0) return 0;
            if(p.Model==AttackModel.PersistentArmy) return damage;
            if(uses>0 && e.Seconds>0) return damage*uses/e.Seconds;
            float cycle=Math.Max(p.Cooldown,Busy(p));
            return cycle>0?damage/cycle:float.NaN;
        }
        float active=Weight(before.Active,e.ActiveUses),auto=Weight(before.Auto,e.AutoUses);
        if(!Valid(active) || !Valid(auto) || active+auto<=0)
            return Prediction.Unavailable("Module total available; attack split needs a supported weapon model");
        float fraction=active/(active+auto);
        Prediction At(float share) => Compare(before,after,e with {
            Active=e.Active+e.Unknown*share,Auto=e.Auto+e.Unknown*(1-share),Unknown=0
        },samples);
        var result=At(fraction);
        var notes=result.Unknowns.ToList();
        notes.Add(active==0 || auto==0
            ? "module total assigned to its sole modeled damage attack"
            : "ability/auto damage split estimated from attack output and triggers");
        if((active>0 && e.ActiveUses<=0 && before.Active.Model!=AttackModel.PersistentArmy) || (auto>0 && e.AutoUses<=0 && before.Auto.Model!=AttackModel.PersistentArmy))
            notes.Add("missing triggers: maximum-use timing assumed for damage split");
        float? low=result.Low,high=result.High;
        if(result.CanRank && active>0 && auto>0)
        {
            foreach(float share in new[]{0f,1f})
            {
                var alternative=At(share);
                if(alternative.CanRank)
                {
                    low=Math.Min(low ?? result.Gain.Value,alternative.Low ?? alternative.Gain.Value);
                    high=Math.Max(high ?? result.Gain.Value,alternative.High ?? alternative.Gain.Value);
                }
            }
            notes.Add("damage split scenarios allow different ability/auto hit rates");
        }
        return result with {Low=low,High=high,Unknowns=notes.Distinct().ToArray()};
    }

    // Verified native Ram collision core, before target-dependent artifacts and damage resolution.
    internal static float RamHit(RamProfile p,float speed,bool fury)
    {
        float hit=(p.Balance*p.Curve(speed)*p.Multiplier+p.Flat)*p.SlotMultiplier;
        return hit*(fury?p.FuryMultiplier:1);
    }

    private static Prediction CompareRam(RamProfile a,RamProfile b,CombatEvidence e,IReadOnlyList<RamSample> samples,List<string> unknowns)
    {
        var valid=samples.Where(s=>s.Seconds>0 && s.Speed>.1f && float.IsFinite(s.Speed)).ToArray();
        if (valid.Length<4) return Prediction.Unavailable("Ram needs movement samples from combat");
        float totalTime=valid.Sum(s=>s.Seconds);
        float furyTime=valid.Where(s=>s.Fury).Sum(s=>s.Seconds)/totalTime;
        float normalA=0,normalB=0,boostA=0,boostB=0;
        foreach(var s in valid)
        {
            float w=s.Seconds/totalTime;
            normalA+=w*RamHit(a,s.Speed,false); normalB+=w*RamHit(b,s.Speed,false);
            boostA+=w*RamHit(a,s.Speed,true); boostB+=w*RamHit(b,s.Speed,true);
        }
        // Preserve correlation: Fury samples generally occur at different speeds.
        float oldHit=valid.Sum(s=>s.Seconds/totalTime*RamHit(a,s.Speed,s.Fury));
        float newHit=valid.Sum(s=>s.Seconds/totalTime*RamHit(b,s.Speed,s.Fury));
        if (!(oldHit>0) || !Valid(newHit)) return Prediction.Unavailable("Ram collision inputs invalid");
        float ratio=newHit/oldHit;
        float lo=ratio,hi=ratio;
        if (a.FuryMultiplier!=b.FuryMultiplier || a.Duration!=b.Duration || a.Cooldown!=b.Cooldown || a.Speed!=b.Speed)
        {
            unknowns.Add("Ram Fury ends on impact; activation and collision timing matter");
            // Duration / cooldown constrain the available Fury window, but do not guarantee extra hits.
            float oldDuty=a.Cooldown>0?Math.Clamp(a.Duration/a.Cooldown,0,1):0;
            float newDuty=b.Cooldown>0?Math.Clamp(b.Duration/b.Cooldown,0,1):0;
            float futureFury=oldDuty>0?Math.Clamp(furyTime*newDuty/oldDuty,0,1):furyTime;
            float windowRatio=ratio;
            if(furyTime>0 && furyTime<1)
            {
                float normalMean=valid.Where(s=>!s.Fury).Sum(s=>s.Seconds*RamHit(b,s.Speed,false))/(totalTime*(1-furyTime));
                float furyMean=valid.Where(s=>s.Fury).Sum(s=>s.Seconds*RamHit(b,s.Speed,true))/(totalTime*furyTime);
                windowRatio=(normalMean*(1-futureFury)+furyMean*futureFury)/oldHit;
            }
            lo=Math.Min(lo,windowRatio); hi=Math.Max(hi,windowRatio);
            if (normalA>0) {lo=Math.Min(lo,normalB/normalA);hi=Math.Max(hi,normalB/normalA);}
            if (boostA>0) {lo=Math.Min(lo,boostB/boostA);hi=Math.Max(hi,boostB/boostA);}
            if(a.Speed!=b.Speed)
            {
                unknowns.Add("Ram speed buff changes impact speed; no guaranteed speed-to-DPS conversion");
                if(a.Speed>0 && b.Speed>0)
                {
                    // Sensitivity scenario only: maximum speed scales with the native percentage buff.
                    // Actual impact speeds also depend on acceleration, steering and collisions.
                    float speedScenario=valid.Sum(s=>s.Seconds/totalTime*RamHit(b,s.Fury?s.Speed*b.Speed/a.Speed:s.Speed,s.Fury))/oldHit;
                    if(Valid(speedScenario)) { lo=Math.Min(lo,speedScenario); hi=Math.Max(hi,speedScenario); }
                }
            }
        }
        unknowns.Add("Ram uses sampled travel speeds, not individual impact speeds");
        float share=(e.Active+e.Auto+e.Unknown)/e.Total;
        return new(share*(ratio-1),share*(lo-1),share*(hi-1),
            $"Ram collision formula ×{F(ratio)}; Fury observed {F(furyTime*100)}%",unknowns.Distinct().ToArray());
    }
}

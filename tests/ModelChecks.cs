using WanderburgDamageHUD;

internal static class ModelChecks
{
    internal static void Run()
    {
        int checks=0;
        void Near(float actual,float expected,string name)
        { checks++; if(!float.IsFinite(actual) || Math.Abs(actual-expected)>.0005f) throw new Exception($"{name}: expected {expected}, got {actual}"); }
        void Check(bool condition,string name) { checks++; if(!condition) throw new Exception(name); }
        var shot=new AttackProfile(AttackModel.Projectile,10,4,10,1,0,0,0,1,1,0,"arrow",Array.Empty<string>());
        var empty=shot with { Damage=0,Count=0 };
        ModelProfile Profile(AttackProfile active,AttackProfile auto)=>new(active,auto,null,Array.Empty<string>());
        var evidence=new CombatEvidence(40,60,0,100,20,2,2);
        Prediction Compare(AttackProfile a,AttackProfile b)=>PredictionModel.Compare(Profile(empty,a),Profile(empty,b),evidence,Array.Empty<RamSample>());
        Near(PredictionModel.AttackDamage(shot),40,"four independent arrows");
        Near(Compare(shot,shot with{Count=6}).Gain.Value,.3f,"charges scale linearly, weighted by auto share");
        Near(Compare(shot,shot with{Damage=20,Count=6}).Gain.Value,1.2f,"damage and charges multiply rather than add");
        Near(Compare(shot,shot with{Cooldown=8}).Gain.Value,.15f,"cooldown: inverse 10/8, not -20 percent");
        Near(PredictionModel.FrequencyRatio(shot,shot with{Cooldown=8},2,40,false),20f/18,"observed idle time reduces cooldown benefit");
        Near(Compare(shot,shot with{Duration=5}).Gain.Value,0,"projectile duration does not invent extra shots");
        var ticks=shot with{Model=AttackModel.Ticks,Count=1,Duration=4,TickInterval=.5f};
        Near(PredictionModel.AttackDamage(ticks),80,"tick rate and exposure");
        Check(Compare(ticks,ticks with{Duration=6}).Gain>0,"longer tick effect increases modeled damage");
        Check(!Compare(ticks,ticks with{Duration=6}).Complete,"tick exposure remains conditional");
        var summon=shot with{Model=AttackModel.Summon,Count=2,Duration=6,UnitAttackInterval=2};
        Near(PredictionModel.AttackDamage(summon),60,"unit count, lifetime and attack interval");
        Near(PredictionModel.ProcDamage(new[]{new ProcProfile(.25f,20,4),new ProcProfile(.5f,10,1)}),25,"proc probability times damage times count");
        Near(Compare(shot,shot with{Damage=5}).Gain.Value,-.3f,"negative changes are preserved");
        Check(!Compare(shot with{Damage=0},shot).CanRank,"new damage source is unknown, not zero benefit");
        Check(!Compare(shot,shot with{Damage=float.NaN}).CanRank,"invalid preview does not rank");
        var unknown=empty with{Model=AttackModel.Unknown,Unknowns=new[]{"unknown"}};
        var both=PredictionModel.Compare(Profile(shot,shot),Profile(shot with{Damage=20},shot with{Damage=20}),evidence,Array.Empty<RamSample>());
        Near(both.Gain.Value,1,"both channels contribute to a single upgrade");
        var one=PredictionModel.Compare(Profile(unknown,shot),Profile(unknown with{Unknowns=new[]{"unknown"}},shot with{Damage=20}),evidence,Array.Empty<RamSample>());
        Near(one.Gain.Value,.6f,"unchanged unsupported channel does not block known change");
        var noAttribution=PredictionModel.Compare(Profile(empty,shot),Profile(empty,shot with{Damage=20}),evidence with{Auto=0,Unknown=60},Array.Empty<RamSample>());
        Near(noAttribution.Gain.Value,.6f,"native module total supports sole damage attack");
        Check(!noAttribution.Complete,"inferred attribution remains labeled");
        var aggregateEvidence=new CombatEvidence(0,0,100,100,20,2,2);
        var aggregate=PredictionModel.Compare(Profile(shot,shot),Profile(shot,shot with{Damage=20}),aggregateEvidence,Array.Empty<RamSample>());
        Near(aggregate.Gain.Value,.5f,"aggregate damage apportioned by expected attack output");
        Near(aggregate.Low.Value,0,"unknown hit rates include all damage on unchanged ability");
        Near(aggregate.High.Value,1,"unknown hit rates include all damage on upgraded auto");
        var identicalGains=PredictionModel.Compare(Profile(shot,shot),Profile(shot with{Damage=20},shot with{Damage=20}),aggregateEvidence,Array.Empty<RamSample>());
        Near(identicalGains.Gain.Value,1,"equal channel gains independent of attribution");
        Near(identicalGains.Low.Value,1,"equal gains keep allocation scenario narrow");
        var noTriggers=PredictionModel.Compare(Profile(shot,shot),Profile(shot,shot with{Damage=20}),aggregateEvidence with{ActiveUses=0,AutoUses=0},Array.Empty<RamSample>());
        Near(noTriggers.Gain.Value,.5f,"missing events use explicitly conditional cycle model");
        Check(noTriggers.Unknowns.Any(x=>x.Contains("maximum-use")),"missing event assumption is visible");
        Check(!PredictionModel.Compare(Profile(unknown,shot),Profile(unknown,shot with{Damage=20}),aggregateEvidence,Array.Empty<RamSample>()).CanRank,"unsupported second source must not receive an invented share");
        var fire=shot with{Damage=5,Count=2,Cooldown=2,VolleyInterval=.5f,Unknowns=new[]{"burn refresh not quantified"}};
        var fireActive=fire with{Count=0,Cooldown=99999};
        var fireResult=PredictionModel.Compare(Profile(fireActive,fire),Profile(fireActive,fire with{Count=3,VolleyInterval=1f/3}),aggregateEvidence with{Unknown=5.6f},Array.Empty<RamSample>());
        Near(fireResult.Gain.Value,.028f,"live Fire Crew roll: 2 to 3 charges on 5.6 percent share");
        Check(!fireResult.Complete,"Fire Crew burn scaling is still conditional");
        // Inputs from the 0.4.1 live Cannon / Cannon / Fire Crew menu and rounded HUD shares.
        var cannonActive=shot with{Damage=46,Count=3,Cooldown=13.5f,Duration=.5f,Speed=111.2627f};
        var cannonAuto=shot with{Damage=15,Count=1,Cooldown=2.6999998f,Duration=0,Speed=40};
        var cannonBefore=Profile(cannonActive,cannonAuto);
        var cannonEvidence=new CombatEvidence(0,0,34.9f,100,20,0,0);
        var cannonDamage=PredictionModel.Compare(cannonBefore,Profile(cannonActive with{Damage=60,Speed=117.57047f},cannonAuto),cannonEvidence,Array.Empty<RamSample>());
        float abilityShare=(138f/13.5f)/(138f/13.5f+15f/2.6999998f);
        Near(cannonDamage.Gain.Value,.349f*abilityShare*(60f/46f-1),"live Cannon damage card estimates with native aggregate counters");
        Check(!cannonDamage.Complete,"Cannon speed and inferred split remain conditional");
        var cannonCooldown=PredictionModel.Compare(cannonBefore,Profile(cannonActive with{Cooldown=12.272727f},cannonAuto with{Cooldown=2.4545453f}),cannonEvidence,Array.Empty<RamSample>());
        Near(cannonCooldown.Gain.Value,.0349f,"live Cannon both cooldowns improve maximum-use output by ten percent");
        Near(cannonCooldown.Low.Value,0,"cooldown scenario includes no additional use");
        var fireDouble=PredictionModel.Compare(Profile(fireActive,fire),Profile(fireActive,fire with{Count=4,Duration=2.5f,VolleyInterval=2.5f/4}),aggregateEvidence with{Unknown=16.1f},Array.Empty<RamSample>());
        Near(fireDouble.Gain.Value,.161f,"live Fire Crew double charges have a conditional numerical estimate");
        Near(fireDouble.Low.Value,.161f*(2*10f/11.375f-1),"Fire Crew duration spreads shots while preserving observed idle time");
        Check(fireDouble.Gain>cannonDamage.Gain && fireDouble.Gain>cannonCooldown.Gain,"live fixture ranks modeled Fire Crew gain first");
        var ram=new RamProfile(10,4,0,1.6f,4,15,1,1,v=>v);
        Near(PredictionModel.RamHit(ram,5,false),200,"native Ram speed curve times multiplier");
        Near(PredictionModel.RamHit(ram with{Flat=30},5,false),230,"Ram flat damage adds to full hit");
        Near(PredictionModel.RamHit(ram,5,true),320,"Fury multiplies collision damage");
        var samples=Enumerable.Range(0,20).Select(_=>new RamSample(5,false,.25f)).ToArray();
        var ramBefore=Profile(empty,empty) with{Ram=ram};
        var ramAfter=ramBefore with{Ram=ram with{Flat=30}};
        var ramEvidence=new CombatEvidence(0,0,100,100,20,0,0);
        var ramResult=PredictionModel.Compare(ramBefore,ramAfter,ramEvidence,samples);
        Near(ramResult.Gain.Value,.15f,"Ram common +30 is +15 percent for a 200 damage collision");
        Check(!ramResult.Complete,"travel sample is not an exact impact sample");
        var allFury=samples.Select(s=>s with{Fury=true}).ToArray();
        var epic=PredictionModel.Compare(ramBefore,ramBefore with{Ram=ram with{FuryMultiplier=2}},ramEvidence,allFury);
        Near(epic.Gain.Value,.25f,"Fury 1.6 to 2 is +25 percent during Fury");
        Check(epic.Gain>ramResult.Gain,"Epic can beat Common with actual collision formula");
        Check(!PredictionModel.Compare(ramBefore,ramAfter,ramEvidence,Array.Empty<RamSample>()).CanRank,"Ram needs movement evidence");
        var parsed=BuildCoachCore.ParseChanges(new[]{"<s>4</s> → <b><color=blue>4.7</color></b> DAMAGE MULT"});
        Near(parsed.Single().After,4.7f,"nested markup parser retained");
        Near(NativeStatMath.Cooldown(15,.25f,1,true),12,"native cooldown reduction stat: 15 / 1.25");
        Near(NativeStatMath.Cooldown(15,.25f,.8f,true),9.6f,"slot cooldown artifact applied once");
        Near(NativeStatMath.Cooldown(1,100,1,true),.5f,"active cooldown floor");
        Near(NativeStatMath.Cooldown(1,100,1,false),.1f,"auto cooldown floor");
        Near(NativeStatMath.Scaled(2,6,0,0),2,"zero added size/speed keeps base");
        Near(NativeStatMath.Scaled(2,6,50,50),2+4*(1-MathF.Exp(-.693f)),"size/speed diminishing returns and artifact bonus");
        Near(NativeStatMath.Rolled(new[]{30f,45f,70f,100f},1),45,"actual internal rarity selects rolled value");
        bool rejected=false;
        try {NativeStatMath.Rolled(new[]{1f},2);} catch(InvalidOperationException) {rejected=true;}
        Check(rejected,"missing rarity data must not silently become zero");
        var army=shot with{Model=AttackModel.PersistentArmy,Damage=3,Count=10,Cooldown=12,Duration=-1,UnitAttackInterval=1};
        Near(PredictionModel.AttackDamage(army),30,"persistent Barracks damage uses standing population, not a negative lifetime");
        var reinforced=army with{Damage=5.5f,Count=14};
        Near(PredictionModel.AttackDamage(reinforced),77,"Side Barracks rare damage and army count compound");
        Near(PredictionModel.FrequencyRatio(army,army with{Cooldown=6},0,20,false),1,"army replenishment does not double the standing army's DPS");
        var reinforcements=army with{Model=AttackModel.Summon,Count=40,Duration=10,Cooldown=60};
        var barracksBefore=Profile(reinforcements,army);
        var barracksAfter=Profile(reinforcements,reinforced);
        var barracks=PredictionModel.Compare(barracksBefore,barracksAfter,new(0,0,20.5f,100,20,0,1),Array.Empty<RamSample>());
        Check(barracks.CanRank,"real Side Barracks offer no longer disappears");
        Near(barracks.Gain.Value,.205f*.6f*(77f/30-1),"persistent army and temporary burst use consistent DPS units for attribution");
        Check(barracks.Gain>.006f,"Side Barracks fixture exceeds screenshot Ram +0.6 percent");
        Check(!barracks.Complete,"unknown damage split stays conditional");
        Console.WriteLine($"PASS: {checks} mechanics and uncertainty checks.");
    }
}

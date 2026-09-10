using UnityEngine;
using Random=System.Random;

namespace WanderburgDamageHUD;

internal static class RuntimeFuture
{
    private sealed record World(Dictionary<string,ProjectionState> Modules,ModuleUpgrade[] Pool,HashSet<int> Installed,
        Dictionary<string,double> Gains,double Score);
    private sealed record Transition(ProjectionState State,double Gain);
    private static IEnumerator<int> work;
    private static FutureSearch<World> search;
    private static string key="";
    internal static string Error {get;private set;}="";
    internal static int Omitted {get;private set;}
    internal static FutureResult[] Results => search?.Results ?? Array.Empty<FutureResult>();
    internal static bool Done => search?.Done ?? false;
    internal static int Depth => search?.Depth ?? 0;
    internal static int Samples => search?.Completed ?? 0;
    internal static int SupportedRoots {get;private set;}
    private static int nextToken;
    private static readonly Dictionary<int,ModuleUpgrade[]> successors=new();
    private static readonly Dictionary<(ProjectionState,int,int,int),Transition> cache=new();
    private static readonly Dictionary<string,ProjectionState> baseline=new();
    private static readonly Dictionary<string,CombatEvidence> evidence=new();
    private static readonly Dictionary<string,RamSample[]> movement=new();
    private static float[] rarityWeights,tierWeights;

    internal static void Pump()
    {
        if(!GM.gm || !GM.gm.upgradeMenuOpen)
        {if(work!=null || search!=null) Reset();return;}
        if(work==null) return;
        try
        {
            var frame=System.Diagnostics.Stopwatch.StartNew();
            do
            {
                if(work.MoveNext()) continue;
                work.Dispose();work=null;
                Plugin.Logger.LogInfo($"Future search done: depth={Depth}; trials={Samples}; computeMs={search.ComputeMs:0}; maxSliceMs={search.MaxSliceMs:0.##}; unsupportedDraws={Omitted}; "+
                    string.Join(" | ",Results.Select(r=>$"card={r.Id}, mean={r.Mean:0.####}, end={r.EndMean:0.####}, wins={r.WinShare:0.###}")));
                break;
            } while(frame.Elapsed.TotalMilliseconds<3);
        }
        catch(Exception ex) {Error="Future search unavailable: "+ex.Message;work?.Dispose();work=null;search=null;Plugin.Logger.LogWarning(Error);}
    }

    internal static void Reset()
    {work?.Dispose();work=null;search=null;key="";cache.Clear();successors.Clear();baseline.Clear();evidence.Clear();movement.Clear();}

    internal static void Ensure(ChoiceData[] choices,float seconds)
    {
        if(!GM.gm || !GM.gm.upgradeMenuOpen || !Plugin.FutureEnabled.Value) return;
        string newKey=nextToken+":"+string.Join("|",choices.Select(c=>$"{c.Index}:{c.Title}:{string.Join("",c.RawLines)}"));
        if(key==newKey) return;
        Reset();key=newKey;Error="";Omitted=0;SupportedRoots=0;
        try
        {
            var gm=GM.gm;var ms=gm.ms;
            foreach(var m in gm.vm.allModules2)
            {
                if(!m || !m.IsInstalled || !m.IsMountedOnVehicle(gm.vm)) continue;
                if(baseline.ContainsKey(m.moduleID)) throw new InvalidOperationException("Duplicate weapon IDs are not supported");
                var captured=RuntimeProjection.Capture(m);
                if(captured.Profile.Ram is { } ram)
                {
                    // One native curve read per distinct speed, not hundreds per simulated state.
                    var values=new Dictionary<float,float>();
                    float Curve(float speed)
                    {if(!values.TryGetValue(speed,out float value)) values[speed]=value=ram.Curve(speed);return value;}
                    captured=captured with{Profile=captured.Profile with{Ram=ram with{Curve=Curve}}};
                }
                baseline[m.moduleID]=captured;
                evidence[m.moduleID]=RuntimeModel.Evidence(m.moduleID,seconds);
                movement[m.moduleID]=RuntimeModel.Samples(m.moduleID).ToArray();
            }
            ms.GetRarityProbabilities(ArtifactSystem.instance?ArtifactSystem.instance.addedLuck:0,out float common,out float uncommon,out float rare,out float epic);
            rarityWeights=new[]{common,uncommon,rare,epic};
            // Native Roll falls back to Common when the total weight is nonpositive.
            if(!rarityWeights.Any(w=>float.IsFinite(w) && w>0)) rarityWeights=new[]{1f,0f,0f,0f};
            tierWeights=Enumerable.Range(0,ms.upgradeModuleRarities.Count).Select(i=>ms.upgradeModuleRarities[i]).ToArray();
            var installed=new HashSet<int>();
            foreach(var p in baseline.Values) if(p.Source.upgradesInstalled!=null)
                foreach(var u in p.Source.upgradesInstalled) if(u) installed.Add(u.GetInstanceID());
            var pool=new List<ModuleUpgrade>();
            foreach(var u in ms.moduleUpgradePool)
                if(u && baseline.ContainsKey(u.forModuleID) && !installed.Contains(u.GetInstanceID()) && Weight(u)>0 && !pool.Any(x=>x==u)) pool.Add(u);
            var world=new World(new(baseline),pool.ToArray(),installed,new(),0);
            var roots=new List<FutureOption<World>>();
            for(int i=0;i<choices.Length;i++)
            {
                int index=choices[i].Index;
                var attributes=new HashSet<int>();
                foreach(int a in ms.selectedCoreAttributes[index]) attributes.Add(a);
                foreach(int a in ms.selectedAdditionalAttributes[index]) attributes.Add(a);
                var result=Apply(world,ms.currentUpgradeOptions[index],ms.internalRarity[index],attributes);
                if(result!=null) roots.Add(new((i+1).ToString(),result,result.Score));
            }
            SupportedRoots=roots.Count;
            if(roots.Count<2) throw new InvalidOperationException("Need at least two supported cards for future comparison");
            search=new(roots.ToArray(),Draw,Plugin.FutureDepth.Value);
            work=search.Run(64,2000).GetEnumerator();
            Plugin.Logger.LogInfo($"Future search started: roots={roots.Count}/{choices.Length}, pool={pool.Count}, depth={Depth}, rarityWeights={string.Join(",",rarityWeights)}");
        }
        catch(Exception ex) {Error="Future comparison unavailable: "+ex.Message;Plugin.Logger.LogWarning(Error);}
    }

    internal static void NewMenu() {nextToken++;}
    private static float Weight(ModuleUpgrade u)=>u.upgradeTier>0 && u.upgradeTier<=tierWeights.Length?tierWeights[u.upgradeTier-1]:0;
    private static FutureOption<World>[] Draw(World world,Random random)
    {
        var pool=world.Pool.ToList();var result=new List<FutureOption<World>>();
        // Do not redraw unsupported cards: they still occupy their real offer slot.
        for(int i=0;i<3 && pool.Count>0;i++)
        {
            int j=FutureSearch<World>.WeightedIndex(pool.Select(Weight).ToArray(),random);if(j<0) break;
            var u=pool[j];pool.RemoveAt(j);
            int rarity=FutureSearch<World>.WeightedIndex(rarityWeights,random);
            if(u.overwritesInternalRarity || rarity<0) {Omitted++;continue;}
            var attrs=Attributes(u,rarity,random);
            var next=Apply(world,u,rarity,attrs);
            if(next!=null) result.Add(new(u.name,next,next.Score));
        }
        return result.ToArray();
    }

    private static HashSet<int> Attributes(ModuleUpgrade u,int rarity,Random random)
    {
        var core=new[]{u.coreStat_abilityAddDamage||u.coreStat_autoDamage,u.coreStat_abilityDuration||u.coreStat_autoDuration,
            u.coreStat_abilityAmmo||u.coreStat_autoAmmo,u.coreStat_abilitySize||u.coreStat_autoSize,u.coreStat_abilitySpeed||u.coreStat_autoSpeed};
        var optional=new[]{u.optionalStat_abilityAddDamage||u.optionalStat_autoDamage,u.optionalStat_abilityDuration||u.optionalStat_autoDuration,
            u.optionalStat_abilityAmmo||u.optionalStat_autoAmmo,u.optionalStat_abilitySize||u.optionalStat_autoSize,u.optionalStat_abilitySpeed||u.optionalStat_autoSpeed};
        var result=new HashSet<int>();
        var available=Enumerable.Range(0,5).Where(i=>core[i]).ToList();
        if(available.Count>0) result.Add(available[random.Next(available.Count)]);
        available=Enumerable.Range(0,5).Where(i=>optional[i]).ToList();
        for(int i=0;i<rarity && available.Count>0;i++) {int j=random.Next(available.Count);result.Add(available[j]);available.RemoveAt(j);}
        return result;
    }

    private static World Apply(World world,ModuleUpgrade u,int rarity,HashSet<int> attrs)
    {
        if(!u || !world.Modules.TryGetValue(u.forModuleID,out var previous)) {Omitted++;return null;}
        if(u.legendaryA || u.legendaryB || u.special0A || u.special0B || u.special1A || u.special1B || u.replacesAlternativeEffect || u.replacesLegendaryActivePrefab)
        {Omitted++;return null;}
        int id=u.GetInstanceID(),mask=attrs.Aggregate(0,(x,a)=>x|(1<<a));
        if(!cache.TryGetValue((previous,id,rarity,mask),out var transition))
        {
            try
            {
                var projected=RuntimeProjection.Apply(previous,u,rarity,attrs);
                var prediction=PredictionModel.Compare(baseline[u.forModuleID].Profile,projected.Profile,evidence[u.forModuleID],movement[u.forModuleID]);
                transition=prediction.CanRank?new(projected,prediction.Gain.Value):null;
            }
            catch {transition=null;}
            cache[(previous,id,rarity,mask)]=transition;
        }
        if(transition==null) {Omitted++;return null;}
        var modules=new Dictionary<string,ProjectionState>(world.Modules) {[u.forModuleID]=transition.State};
        var gains=new Dictionary<string,double>(world.Gains) {[u.forModuleID]=transition.Gain};
        var installed=new HashSet<int>(world.Installed) {id};
        var pool=world.Pool.Where(x=>x.GetInstanceID()!=id).ToList();
        if(!successors.TryGetValue(id,out var next))
        {var list=new List<ModuleUpgrade>();if(u.addToPoolUpgrades!=null) foreach(var n in u.addToPoolUpgrades) if(n && n.forModuleID==u.forModuleID) list.Add(n);successors[id]=next=list.ToArray();}
        foreach(var n in next) if(!installed.Contains(n.GetInstanceID()) && Weight(n)>0 && !pool.Any(x=>x==n)) pool.Add(n);
        return new(modules,pool.ToArray(),installed,gains,gains.Values.Sum());
    }
}

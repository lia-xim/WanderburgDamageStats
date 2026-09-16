using Il2CppInterop.Runtime.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace WanderburgDamageHUD;

internal sealed record RankedChoice(ChoiceData Choice, int Card, Prediction Assessment);

public sealed class DamageOverlay : MonoBehaviour
{
    internal static readonly Dictionary<int,ChoiceData> Choices = new();
    private float refreshAt;
    private float errorAt;
    private string lastInventory="";
    private float lastGameTime=-1;
    private bool details;
    private readonly HashSet<string> loggedPreviews=new();
    private HudView hud;
    public DamageOverlay(IntPtr pointer) : base(pointer) { }

    public void Update()
    {
        try
        {
            if(Keyboard.current!=null && Keyboard.current.f8Key.wasPressedThisFrame) Plugin.Enabled.Value=!Plugin.Enabled.Value;
            if(Keyboard.current!=null && Keyboard.current.f9Key.wasPressedThisFrame) details=!details;
            if(Keyboard.current!=null && Keyboard.current.f7Key.wasPressedThisFrame) ToggleCoach();
            RuntimeFuture.Pump();
            if(Time.unscaledTime<refreshAt) return;
            refreshAt=Time.unscaledTime+.25f;
            var gm=GM.gm;
            CatalogSnapshot.TryExport(gm);
            bool inRun=gm && gm.vm && gm.gameplayRoundStarted && !gm.PreRunLoadoutActive && !gm.mainMenuOpen && !gm.gameOver;
            if(!inRun)
            {
                hud?.Hide();
                RuntimeFuture.Reset();
                if(gm && gm.PreRunLoadoutActive && lastGameTime>=0) { CombatTelemetry.Reset(); lastGameTime=-1; }
                if(!gm || !gm.vm) Choices.Clear();
                return;
            }
            if(lastGameTime>=0 && gm.gameTime+1f<lastGameTime) {CombatTelemetry.Reset();RuntimeFuture.Reset();}
            lastGameTime=gm.gameTime;
            CombatTelemetry.RefreshFromGame();
            RuntimeModel.Observe(gm);
            if(!Plugin.Enabled.Value) { hud?.Hide(); return; }
            var modules=gm.vm.allModules2;
            if(modules==null) { hud?.Hide(); return; }
            var rows=new List<HudWeapon>();
            var inventory=new List<string>();
            var recentTotal=CombatTelemetry.RecentTotal();
            float recentAll=recentTotal.Active+recentTotal.Auto+recentTotal.Unknown;
            float observedSeconds=CombatTelemetry.HasStarted?Mathf.Clamp(Time.time-CombatTelemetry.StartedAt,1f,20f):1f;
            for(int i=0;i<modules.Count;i++)
            {
                var m=modules[i];
                if(!m || !m.IsInstalled || !m.IsMountedOnVehicle(gm.vm)) continue;
                var stats=WeaponStats.Read(m);
                if(stats.Active<=0 && stats.Auto<=0) continue;
                string name=Clean(m.moduleName);
                var recent=CombatTelemetry.Recent(m.moduleID);
                float dealt=recent.Active+recent.Auto+recent.Unknown;
                float share=recentAll>0?dealt/recentAll:0f;
                string channels=recent.Active+recent.Auto>0 ? $"Skill {Number(recent.Active/observedSeconds)} · Auto {Number(recent.Auto/observedSeconds)}"+(recent.Unknown>0?$" · Other {Number(recent.Unknown/observedSeconds)}":"") : "Channel split unavailable";
                rows.Add(new(name,share,dealt/observedSeconds,channels));
                inventory.Add($"{name}:{stats.Active:0.##}/{stats.Auto:0.##}");
            }
            string currentInventory=string.Join("|",inventory);
            if(currentInventory!=lastInventory)
            {
                lastInventory=currentInventory;
                Plugin.Logger.LogInfo("HUD inventory: "+currentInventory);
            }
            EnsureUI();
            if(hud==null) return;
            var activeChoices=Choices.Values.Where(c=>c.Option && c.Option.gameObject.activeInHierarchy && c.Option.HasConfiguredOption)
                .OrderBy(c=>c.Option.transform.position.x).Take(3).ToArray();
            bool showingUpgrade=gm.upgradeMenuOpen || activeChoices.Length>0;
            var content=new System.Text.StringBuilder();
            var hints=new List<CardHint>();
            if(Plugin.FutureEnabled.Value && showingUpgrade && activeChoices.Length>0)
            {
                var ranked=new List<RankedChoice>();
                for(int i=0;i<activeChoices.Length;i++)
                {
                    var c=activeChoices[i];
                    string moduleId=ResolveModuleId(c,modules);
                    var currentModule=ResolveModule(moduleId,modules);
                    ModelProfile projected=null;
                    Prediction assessment;
                    try
                    {
                        projected=RuntimeProjection.Project(currentModule,c,gm.ms);
                        assessment=RuntimeModel.Assess(currentModule,projected,moduleId,observedSeconds);
                    }
                    catch(Exception ex) { assessment=Prediction.Unavailable("Card projection unavailable: "+ex.Message); }
                    // Keep behavior-only cards visible even when generic numeric fields do not change.
                    if(c.HasSpecialEffect)
                        assessment=assessment with { Unknowns=assessment.Unknowns.Append("special behavior / future synergies not fully modeled").Distinct().ToArray() };
                    string previewKey=$"{c.Option.GetInstanceID()}:{c.Index}:{c.Title}:{string.Join("|",c.RawLines)}";
                    if(loggedPreviews.Add(previewKey))
                    {
                        Plugin.Logger.LogInfo($"Model card {i+1}: rolled data={(projected!=null?"yes":"no")}; module={moduleId}; gain={assessment.Gain}; {assessment.Reason}; unknown={string.Join("; ",assessment.Unknowns)}");
                        if(currentModule && projected!=null)
                            Plugin.Logger.LogInfo($"Model inputs card {i+1}: before={RuntimeModel.Describe(currentModule)}; after={RuntimeModel.Describe(projected,moduleId)}");
                    }
                    ranked.Add(new(c,i+1,assessment));
                }
                // The same order drives both the lead card and the visible list.
                var ordered=ranked.OrderByDescending(r=>r.Assessment.CanRank)
                    .ThenByDescending(r=>r.Assessment.Gain ?? float.NegativeInfinity).ToArray();
                var winner=ordered.FirstOrDefault(r=>r.Assessment.CanRank && r.Assessment.Gain>0);
                bool complete=ranked.All(r=>r.Assessment.Complete);
                RuntimeFuture.Ensure(activeChoices,observedSeconds);
                var future=RuntimeFuture.Results.OrderByDescending(r=>r.Mean).ThenByDescending(r=>r.WinShare).ToArray();
                bool futureReady=Plugin.FutureEnabled.Value && RuntimeFuture.Samples>=16 && future.Length>=2;
                bool partial=ranked.Any(r=>!r.Assessment.CanRank) || (futureReady && future.Length<ranked.Count);
                bool futureTie=futureReady && Math.Abs(future[0].Mean-future[1].Mean)<.005;
                foreach(var item in ranked)
                {
                    var route=future.FirstOrDefault(f=>f.Id==item.Card.ToString());
                    bool lead=futureReady ? !futureTie && route!=null && route.Id==future[0].Id : winner==item;
                    string label=lead?(partial?"PARTIAL LEAD":"ESTIMATED PICK"):"DAMAGE ESTIMATE";
                    string now=item.Assessment.CanRank?Signed(item.Assessment.Gain.Value)+" now":"Not estimated";
                    string later=futureReady && route!=null?$" · {Signed((float)route.EndMean)} after {RuntimeFuture.Depth}":"";
                    string caveat=partial?"Some cards not modeled":item.Assessment.Unknowns.Length>0?"Conditional estimate":"Modeled build DPS";
                    hints.Add(new(item.Choice.Option,$"<b>{label}</b>\n{now}{later}\n<size=80%>{caveat} · F9 details</size>",lead));
                }
                if(Plugin.FutureEnabled.Value)
                {
                    content.Append("<color=#F3C879><b>BUILD OUTLOOK</b></color>\n");
                    if(futureReady)
                    {
                        var best=future[0];
                        var chosen=ranked.First(r=>r.Card.ToString()==best.Id);
                        var missing=ranked.Where(r=>!future.Any(f=>f.Id==r.Card.ToString())).ToArray();
                        bool tied=Math.Abs(best.Mean-future[1].Mean)<.005;
                        if(missing.Length>0) content.Append("<color=#F3C879><b>PARTIAL COMPARISON</b></color>\n");
                        content.Append(tied?"<b>No clear lead yet</b>":missing.Length>0?$"<b>Modeled lead: Card {best.Id}</b>":$"<color=#8FE3A1><b>PLANNED PICK: CARD {best.Id}</b></color>");
                        content.Append($"\n{Clean(chosen.Choice.Name)} · {chosen.Choice.Kind}");
                        content.Append($"\n<size=85%>Top in {Percent((float)best.WinShare)} of simulations\nNext {RuntimeFuture.Depth} normal upgrades · {RuntimeFuture.Samples} paths/card{(RuntimeFuture.Done?"":" · calculating")}</size>");
                        content.Append("\n\n<size=85%><b>NOW → AFTER UPGRADES</b>");
                        foreach(var f in future)
                        {
                            var c=ranked.First(r=>r.Card.ToString()==f.Id);
                            content.Append($"\nCard {f.Id}: {Signed(c.Assessment.Gain ?? 0)} → {Signed((float)f.EndMean)}");
                        }
                        foreach(var c in missing) content.Append($"\n<color=#F3C879>Card {c.Card}: {Clean(c.Choice.Name)} · not modeled</color>");
                        content.Append("\nGains vs your current build. Pick balances damage now and later.</size>");
                        content.Append($"\n<color=#F3C879><size=80%>Estimated damage paths, not win odds. Same combat behavior; {(RuntimeFuture.Omitted>0?"some future effects excluded":"normal module upgrades only")}.{(RuntimeFuture.SupportedRoots<ranked.Count?" Some current cards not compared.":"")}</size></color>\n<size=80%>F9: model details</size>\n\n");
                    }
                    else
                    {
                        content.Append(RuntimeFuture.Error.Length>0?"<size=80%>Future comparison unavailable for this offer.</size>\n\n":$"<size=85%>Simulating the next {Plugin.FutureDepth.Value} upgrades…</size>\n\n");
                    }
                }
                if(!futureReady || details)
                {
                content.Append("<color=#F3C879><b>UPGRADE COMPARISON</b></color>");
                if(winner!=null)
                {
                    content.Append($"\n<color=#8FE3A1><b>{(complete?"DAMAGE PICK":"TENTATIVE DAMAGE PICK")}: CARD {winner.Card}</b></color>");
                    content.Append($"\n{Clean(winner.Choice.Name)} · {winner.Choice.Kind}");
                    content.Append($"\n<color=#8FE3A1>{Signed(winner.Assessment.Gain.Value)} modeled build DPS</color>");
                    if(winner.Assessment.Low.HasValue && winner.Assessment.High.HasValue && winner.Assessment.High-winner.Assessment.Low>.005f)
                        content.Append($"\n<size=80%>Sensitivity scenarios: {Signed(winner.Assessment.Low.Value)} to {Signed(winner.Assessment.High.Value)} (not bounds)</size>");
                    if(details) content.Append($"\n<size=80%>{winner.Assessment.Reason}</size>");
                }
                else content.Append("\nNo reliable damage pick yet.");
                if(!complete) content.Append("\n<color=#F3C879><size=80%>Overall winner uncertain: some effects or combat conditions are unresolved.</size></color>");
                content.Append("\n\n<size=85%><b>CARD COMPARISON</b>");
                foreach(var item in ordered)
                {
                    string value=item.Assessment.CanRank?Signed(item.Assessment.Gain.Value):"not estimated";
                    content.Append($"\nCard {item.Card}: {Clean(item.Choice.Name)} · {value}");
                    if(details && item.Assessment.Unknowns.Length>0)
                        content.Append($"\n<size=85%>{string.Join("; ",item.Assessment.Unknowns.Take(2))}{(item.Assessment.Unknowns.Length>2?"; further uncertainties":"")}</size>");
                }
                content.Append("</size>\n\n");
                if(!details) content.Append("<size=80%>F9: model details</size>\n\n");
                }
            }
            hud.Render(rows.ToArray(),recentAll/observedSeconds,recentAll>0,showingUpgrade,content.ToString().Trim(),details,hints.ToArray());
        }
        catch(Exception ex)
        {
            if(Time.unscaledTime>errorAt) { errorAt=Time.unscaledTime+15; Plugin.Logger.LogWarning(ex.ToString()); }
        }
    }

    [HideFromIl2Cpp]
    private void EnsureUI()
    {
        if(hud!=null) return;
        var fonts=Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset font=null;
        foreach(var candidate in fonts)
        {
            if(!candidate) continue;
            if(font==null) font=candidate;
            if(candidate.name.Contains("LiberationSans") || candidate.name.Contains("Roboto-Regular")) {font=candidate;break;}
        }
        if(!font) return;
        hud=new HudView(font,ToggleCoach,()=>{details=!details;refreshAt=0;});
        Plugin.Logger.LogInfo("DPS-first HUD created; font: "+font.name);
    }

    [HideFromIl2Cpp]
    private void ToggleCoach()
    {
        Plugin.FutureEnabled.Value=!Plugin.FutureEnabled.Value;
        RuntimeFuture.Reset();refreshAt=0;
        Plugin.Logger.LogInfo("Upgrade coach "+(Plugin.FutureEnabled.Value?"enabled":"disabled"));
    }

    [HideFromIl2Cpp]
    internal static string Clean(string value)=>System.Text.RegularExpressions.Regex.Replace(value??"Weapon","<[^>]*>","");
    [HideFromIl2Cpp]
    internal static string Number(float value)=>float.IsFinite(value)?value.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture):"–";
    [HideFromIl2Cpp]
    internal static string Percent(float value)=>float.IsFinite(value)?(value*100f).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"%":"–";

    [HideFromIl2Cpp]
    private static string ResolveModuleId(ChoiceData choice,Il2CppSystem.Collections.Generic.List<Module2> modules)
    {
        if(!string.IsNullOrWhiteSpace(choice.ModuleId)) return choice.ModuleId;
        for(int i=0;i<modules.Count;i++)
        {
            var module=modules[i];
            if(module && string.Equals(Clean(module.moduleName),Clean(choice.Name),StringComparison.OrdinalIgnoreCase)) return module.moduleID;
        }
        return "";
    }

    [HideFromIl2Cpp]
    private static Module2 ResolveModule(string moduleId,Il2CppSystem.Collections.Generic.List<Module2> modules)
    {
        Module2 found=null;
        if(string.IsNullOrWhiteSpace(moduleId)) return null;
        for(int i=0;i<modules.Count;i++)
        {
            var module=modules[i];
            if(module && module.IsInstalled && module.IsMountedOnVehicle(GM.gm.vm) && string.Equals(module.moduleID,moduleId,StringComparison.OrdinalIgnoreCase))
            { if(found) return null; found=module; }
        }
        return found;
    }

    [HideFromIl2Cpp]
    private static string Signed(float value) => (value>0?"+":"")+Percent(value);

}

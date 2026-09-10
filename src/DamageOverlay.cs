using Il2CppInterop.Runtime.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace WanderburgDamageHUD;

internal sealed record RankedChoice(ChoiceData Choice, int Card, UpgradeAssessment Assessment, float? ChannelShare);

public sealed class DamageOverlay : MonoBehaviour
{
    internal static readonly Dictionary<int,ChoiceData> Choices = new();
    private float refreshAt;
    private float errorAt;
    private string lastInventory="";
    private float lastGameTime=-1;
    private GameObject canvasObject;
    private RectTransform panel;
    private TextMeshProUGUI text;
    public DamageOverlay(IntPtr pointer) : base(pointer) { }

    public void Update()
    {
        try
        {
            if(Keyboard.current!=null && Keyboard.current.f8Key.wasPressedThisFrame) Plugin.Enabled.Value=!Plugin.Enabled.Value;
            if(Time.unscaledTime<refreshAt) return;
            refreshAt=Time.unscaledTime+.25f;
            var gm=GM.gm;
            bool inRun=gm && gm.vm && !gm.mainMenuOpen;
            if(!inRun || !Plugin.Enabled.Value)
            {
                if(canvasObject) canvasObject.SetActive(false);
                if(!gm || !gm.vm) Choices.Clear();
                return;
            }
            if(lastGameTime>=0 && gm.gameTime+1f<lastGameTime) CombatTelemetry.Reset();
            lastGameTime=gm.gameTime;
            CombatTelemetry.RefreshFromGame();
            var modules=gm.vm.allModules2;
            if(modules==null) return;
            var rows=new List<string>();
            var inventory=new List<string>();
            var recentTotal=CombatTelemetry.RecentTotal();
            float recentAll=recentTotal.Active+recentTotal.Auto+recentTotal.Unknown;
            var cumulativeTotal=CombatTelemetry.CumulativeTotal();
            float cumulativeAll=cumulativeTotal.Active+cumulativeTotal.Auto+cumulativeTotal.Unknown;
            bool scoreFromRecent=recentAll>0.01f;
            float scoreAll=scoreFromRecent?recentAll:cumulativeAll;
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
                rows.Add($"<b>{name}</b>\n{Percent(share)} Anteil  ·  {Number(dealt/observedSeconds)} DPS");
                inventory.Add($"{name}:{stats.Active:0.##}/{stats.Auto:0.##}");
            }
            string currentInventory=string.Join("|",inventory);
            if(currentInventory!=lastInventory)
            {
                lastInventory=currentInventory;
                Plugin.Logger.LogInfo("HUD inventory: "+currentInventory);
            }
            EnsureUI();
            if(!canvasObject) return;
            var content=new System.Text.StringBuilder("<color=#F3C879><b>DAMAGE STATS</b></color>\n");
            content.Append(recentAll>0
               ?$"<size=85%>Letzte 20 s: {Number(recentAll/observedSeconds)} DPS · F8</size>\n\n"
                :"<size=85%>Warte auf Kampftreffer · F8</size>\n\n");
            content.Append(rows.Count>0?string.Join("\n\n",rows):"Noch keine Waffen montiert.");
            if(gm.upgradeMenuOpen)
            {
                var active=Choices.Values.Where(c=>c.Option && c.Option.gameObject.activeInHierarchy && c.Option.HasConfiguredOption)
                    .OrderBy(c=>c.Option.transform.position.x).Take(3).ToArray();
                if(active.Length>0)
                {
                    var ranked=new List<RankedChoice>();
                    for(int i=0;i<active.Length;i++)
                    {
                        var c=active[i];
                        string moduleId=ResolveModuleId(c,modules);
                        float? share=ResolveChannelShare(moduleId,c.Kind,scoreFromRecent,scoreAll);
                        bool special=c.Kind=="Spezial";
                        ranked.Add(new(c,i+1,BuildCoachCore.Assess(c.RawLines,share,special),share));
                    }
                    var calculable=ranked.Where(r=>r.Assessment.UpgradeStrength>0 && !r.Assessment.HasUnmodelledEffect)
                        .OrderByDescending(r=>r.Assessment.EstimatedBuildGain ?? r.Assessment.UpgradeStrength).ToArray();
                    var winner=calculable.FirstOrDefault();
                    bool hasSpecial=ranked.Any(r=>r.Assessment.HasUnmodelledEffect);
                    content.Append("\n\n<color=#F3C879><b>UPGRADE-TIPP</b></color>");
                    if(winner!=null)
                    {
                        content.Append($"\n<b>{(hasSpecial?"ZAHLEN-SIEGER":"EMPFEHLUNG")} · KARTE {winner.Card}</b>");
                        content.Append($"\n{Clean(winner.Choice.Name)} · {winner.Choice.Kind}");
                        content.Append(winner.Assessment.EstimatedBuildGain.HasValue
                            ?$"\n<color=#8FE3A1>≈ +{Percent(winner.Assessment.EstimatedBuildGain.Value)} Build-Output</color>"
                            :$"\n<color=#8FE3A1>+{Percent(winner.Assessment.UpgradeStrength)} Karteneffekt</color>");
                        content.Append($"\n<size=82%>{winner.Assessment.MainReason}");
                        if(winner.ChannelShare.HasValue) content.Append($" · betrifft {Percent(winner.ChannelShare.Value)} deines Schadens");
                        content.Append("</size>");
                    }
                    else content.Append("\nNoch keine numerisch belastbare Empfehlung.");

                    content.Append("\n\n<size=85%><b>RANGLISTE</b>");
                    foreach(var item in ranked.OrderByDescending(r=>r.Assessment.HasUnmodelledEffect?float.MinValue:r.Assessment.EstimatedBuildGain??r.Assessment.UpgradeStrength))
                    {
                        string value=item.Assessment.HasUnmodelledEffect?"situativ":item.Assessment.EstimatedBuildGain.HasValue?"≈ +"+Percent(item.Assessment.EstimatedBuildGain.Value):"+"+Percent(item.Assessment.UpgradeStrength);
                        content.Append($"\nKarte {item.Card}: {Clean(item.Choice.Name)} · {value}");
                    }
                    if(hasSpecial) content.Append("\nSpezialkarten bitte auf der Karte prüfen.");
                    if(scoreAll<=0) content.Append("\nVorläufig: noch keine Schadensdaten.");
                    content.Append("</size>");
                }
            }
            canvasObject.SetActive(true);
            text.text=content.ToString();
            float factor=Mathf.Clamp(Screen.height/1080f,.5f,3f)*Mathf.Clamp(Plugin.Scale.Value,.7f,1.8f);
            float width=Mathf.Min(216f*factor,Screen.width*.18f);
            float x=Mathf.Clamp((gm.upgradeMenuOpen?16:Plugin.Left.Value)*factor,0,Screen.width-width);
            float y=Mathf.Clamp(Plugin.Top.Value*factor,0,Screen.height-100);
            panel.anchoredPosition=new Vector2(x,-y);
            panel.sizeDelta=new Vector2(width,Screen.height-y-16);
            text.fontSize=15f*factor;
            text.fontSizeMax=15f*factor;
            text.fontSizeMin=11f*factor;
            // Preferred height uses the current width; bounded to the viewport for long inventories.
            float preferred=text.GetPreferredValues(text.text,width-20*factor,10000).y+24*factor;
            panel.sizeDelta=new Vector2(width,Mathf.Min(preferred,Screen.height-y-16));
        }
        catch(Exception ex)
        {
            if(Time.unscaledTime>errorAt) { errorAt=Time.unscaledTime+15; Plugin.Logger.LogWarning(ex.ToString()); }
        }
    }

    [HideFromIl2Cpp]
    private void EnsureUI()
    {
        if(canvasObject) return;
        var fonts=Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset font=null;
        for(int i=0;i<fonts.Length;i++)
        {
            if(!fonts[i]) continue;
            if(font==null) font=fonts[i];
            if(fonts[i].name.Contains("LiberationSans") || fonts[i].name.Contains("Roboto-Regular")) { font=fonts[i]; break; }
        }
        if(!font) return;
        canvasObject=new GameObject("WanderburgDamageHUD.Canvas");
        Object.DontDestroyOnLoad(canvasObject);
        var canvas=canvasObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=30000;
        var panelObject=new GameObject("StatsPanel");
        panel=panelObject.AddComponent<RectTransform>();
        panel.SetParent(canvasObject.transform,false);
        panel.anchorMin=new Vector2(0,1);
        panel.anchorMax=new Vector2(0,1);
        panel.pivot=new Vector2(0,1);
        var background=panelObject.AddComponent<Image>();
        background.color=new Color(.045f,.05f,.065f,.92f);
        background.raycastTarget=false;
        var textObject=new GameObject("StatsText");
        var textRect=textObject.AddComponent<RectTransform>();
        textRect.SetParent(panel,false);
        textRect.anchorMin=Vector2.zero;
        textRect.anchorMax=Vector2.one;
        textRect.offsetMin=new Vector2(10,12);
        textRect.offsetMax=new Vector2(-10,-12);
        text=textObject.AddComponent<TextMeshProUGUI>();
        text.font=font;
        text.color=new Color(.95f,.95f,.93f);
        text.richText=true;
        text.enableWordWrapping=true;
        text.enableAutoSizing=true;
        text.alignment=TextAlignmentOptions.TopLeft;
        text.overflowMode=TextOverflowModes.Truncate;
        text.raycastTarget=false;
        Plugin.Logger.LogInfo("Native UI created; font: "+font.name);
    }

    [HideFromIl2Cpp]
    internal static string Clean(string value)=>System.Text.RegularExpressions.Regex.Replace(value??"Waffe","<[^>]*>","");
    [HideFromIl2Cpp]
    internal static string Number(float value)=>float.IsFinite(value)?value.ToString("0.##",System.Globalization.CultureInfo.GetCultureInfo("de-DE")):"–";
    [HideFromIl2Cpp]
    internal static string Percent(float value)=>float.IsFinite(value)?(value*100f).ToString("0.#",System.Globalization.CultureInfo.GetCultureInfo("de-DE"))+"%":"–";

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
    private static float? ResolveChannelShare(string moduleId,string kind,bool recent,float total)
    {
        if(total<=0 || string.IsNullOrWhiteSpace(moduleId)) return null;
        var value=recent?CombatTelemetry.Recent(moduleId):CombatTelemetry.Cumulative(moduleId);
        float moduleTotal=value.Active+value.Auto+value.Unknown;
        bool hasChannelSplit=value.Active+value.Auto>0;
        float affected=kind switch { "Aktiv" when hasChannelSplit=>value.Active, "Auto" when hasChannelSplit=>value.Auto, _=>moduleTotal };
        return Mathf.Clamp01(affected/total);
    }
}

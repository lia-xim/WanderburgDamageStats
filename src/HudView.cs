using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace WanderburgDamageHUD;

internal sealed record HudWeapon(string Name,float Share,float Dps,string Channels);

// Native, reusable UI. Only the coach buttons intercept clicks; the meter stays click-through.
internal sealed class HudView
{
    private static readonly Color Gold=new(.96f,.76f,.42f);
    private static readonly Color Ink=new(.045f,.055f,.07f,.48f);
    private static readonly Color Muted=new(.62f,.66f,.72f);
    private static readonly Color[] Palette={Gold,new(.40f,.72f,.93f),new(.93f,.48f,.39f),new(.56f,.81f,.61f),new(.72f,.58f,.94f)};
    private readonly GameObject canvas;
    private readonly CardHints cardHints;
    private readonly RectTransform root,stats,coach,coachBody,toggleRect;
    private readonly TMP_FontAsset font;
    private readonly TextMeshProUGUI heading,version,total,unit,period,weaponHeading,toggleLabel,coachText;
    private readonly List<(RectTransform Root,TextMeshProUGUI Name,TextMeshProUGUI Dps,TextMeshProUGUI Share,RectTransform Track,RectTransform Fill)> rows=new();
    private readonly RectTransform detailsRect;
    private readonly TextMeshProUGUI detailsLabel;

    internal HudView(TMP_FontAsset font,Action toggleCoach,Action toggleDetails)
    {
        this.font=font;
        canvas=new GameObject("WanderburgDamageStats.HUD");Object.DontDestroyOnLoad(canvas);
        var c=canvas.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=30000;
        canvas.AddComponent<GraphicRaycaster>();
        cardHints=new CardHints(canvas.transform,font);
        root=Rect("HUD",canvas.transform);
        stats=Plate("Damage meter",root);
        var accent=Box("Gold edge",stats,Gold);Place(accent,0,0,3,100); // resized in Render
        heading=Text("Title",stats,"DAMAGE STATS",13,Gold,FontStyles.Bold);
        version=Text("Version",stats,"v"+Plugin.ModVersion,9,Muted);
        total=Text("Total DPS",stats,"0",40,Color.white,FontStyles.Bold);
        unit=Text("DPS label",stats,"DPS",12,Gold,FontStyles.Bold);
        period=Text("Sample window",stats,"LAST 20 SECONDS",9,Muted);
        weaponHeading=Text("Weapons heading",stats,"DAMAGE BY WEAPON",9,Muted,FontStyles.Bold);
        toggleRect=Box("Coach toggle",stats,new(.14f,.16f,.20f));
        toggleLabel=Text("Toggle label",toggleRect,"",10,Gold,FontStyles.Bold);
        var button=toggleRect.gameObject.AddComponent<Button>();button.targetGraphic=toggleRect.GetComponent<Image>();
        button.onClick.AddListener((UnityEngine.Events.UnityAction)toggleCoach);
        toggleRect.GetComponent<Image>().raycastTarget=true;
        coach=Plate("Upgrade coach",root);
        detailsRect=Box("Details toggle",coach,new(.14f,.16f,.20f));
        detailsLabel=Text("Details label",detailsRect,"",9,Muted);
        var detailsButton=detailsRect.gameObject.AddComponent<Button>();detailsButton.targetGraphic=detailsRect.GetComponent<Image>();
        detailsButton.onClick.AddListener((UnityEngine.Events.UnityAction)toggleDetails);
        detailsRect.GetComponent<Image>().raycastTarget=true;
        // A separate scroll owner preserves the DPS meter even with lengthy model explanations.
        coachBody=Rect("Coach viewport",coach);coachBody.gameObject.AddComponent<RectMask2D>();
        var content=Rect("Coach content",coachBody);
        coachText=Text("Coach text",content,"",12,Color.white);
        var scroll=coachBody.gameObject.AddComponent<ScrollRect>();
        scroll.viewport=coachBody;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
        scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=22;
        var surface=coachBody.gameObject.AddComponent<Image>();surface.color=Color.clear;surface.raycastTarget=true;
    }

    internal void Hide()=>canvas.SetActive(false);

    internal void Render(HudWeapon[] weapons,float dps,bool hasDamage,bool upgrade,string coaching,bool details,CardHint[] hints)
    {
        canvas.SetActive(true);
        cardHints.Render(upgrade && Plugin.FutureEnabled.Value?hints:Array.Empty<CardHint>());
        stats.gameObject.SetActive(!upgrade);
        float scale=Mathf.Clamp(Screen.height/1080f,.5f,3f)*Mathf.Clamp(Plugin.Scale.Value,.7f,1.8f);
        float width=upgrade?Mathf.Min(232,Screen.width*.12f/scale):230;
        float x=Mathf.Clamp((upgrade?12:Plugin.Left.Value)*scale,0,Screen.width-width*scale);
        float y=Mathf.Clamp(Plugin.Top.Value*scale,12,Mathf.Max(12,Screen.height-300*scale));
        root.localScale=new Vector3(scale,scale,1);root.anchoredPosition=new(x,-y);
        float available=(Screen.height-y-16)/scale;
        float meterHeight=108+weapons.Length*(details?48:32);
        Place(stats,0,0,width,meterHeight);
        Place(stats.GetChild(1).Cast<RectTransform>(),0,0,3,meterHeight);
        Place(heading.rectTransform,15,14,width-72,19);Place(version.rectTransform,width-52,17,42,14);
        total.text=DamageOverlay.Number(dps);
        total.fontSize=30;Place(total.rectTransform,13,34,width-62,38);Place(unit.rectTransform,width-49,49,38,18);
        period.text=hasDamage?"LAST 20 SECONDS  ·  F8 HIDE":"WAITING FOR COMBAT  ·  F8 HIDE";
        Place(period.rectTransform,16,77,width-30,16);weaponHeading.gameObject.SetActive(false);
        while(rows.Count<weapons.Length)
        {
            var r=Rect("Weapon "+rows.Count,stats);
            var name=Text("Weapon name",r,"",12,Color.white,FontStyles.Bold);
            var value=Text("Weapon DPS",r,"",12,Color.white,FontStyles.Bold);value.alignment=TextAlignmentOptions.TopRight;
            var share=Text("Damage share",r,"",9,Muted);
            var track=Box("Track",r,new(.20f,.22f,.27f));var fill=Box("Fill",track,Gold);
            rows.Add((r,name,value,share,track,fill));
        }
        for(int i=0;i<rows.Count;i++)
        {
            var r=rows[i];r.Root.gameObject.SetActive(i<weapons.Length);if(i>=weapons.Length) continue;
            var w=weapons[i];Place(r.Root,16,101+i*(details?48:32),width-32,details?44:30);
            r.Name.text=w.Name;r.Dps.text=DamageOverlay.Number(w.Dps);r.Share.text=w.Channels;
            Place(r.Name.rectTransform,0,0,width-90,18);Place(r.Dps.rectTransform,width-84,0,52,18);
            Place(r.Track,0,21,width-32,2);Place(r.Fill,0,0,(width-32)*Mathf.Clamp01(w.Share),2);
            r.Fill.GetComponent<Image>().color=Palette[i%Palette.Length];r.Share.gameObject.SetActive(details);Place(r.Share.rectTransform,0,29,width-32,13);
        }
        // Control strip belongs to the meter, so recommendations can always be re-enabled.
        float controlsY=103+weapons.Length*(details?48:32);meterHeight=controlsY+38;
        stats.sizeDelta=new(width,meterHeight);
        Place(stats.GetChild(1).Cast<RectTransform>(),0,0,3,meterHeight);
        Place(toggleRect,14,controlsY,width-28,24);
        toggleLabel.text=Plugin.FutureEnabled.Value?"F7   UPGRADE COACH  ON   ›":"F7   UPGRADE COACH  OFF   ›";
        Place(toggleLabel.rectTransform,8,5,width-44,16);
        bool showCoach=details && Plugin.FutureEnabled.Value && upgrade && coaching.Length>0;
        coach.gameObject.SetActive(showCoach);
        if(showCoach)
        {
            float top=0,room=Math.Max(65,available-top);
            float contentWidth=width-28;
            if(coachText.text!=coaching) coachText.transform.parent.Cast<RectTransform>().anchoredPosition=Vector2.zero;
            coachText.text=coaching;coachText.fontSize=12;
            float preferred=coachText.GetPreferredValues(coaching,contentWidth,10000).y+8;
            float height=Math.Min(room,preferred+45);
            Place(coach,0,top,width,height);
            Place(detailsRect,14,10,width-28,21);
            detailsLabel.text=details?"F9   CLOSE DETAILS   ·   SCROLL ↓":"F9   MODEL DETAILS   +";
            Place(detailsLabel.rectTransform,7,4,width-42,15);
            Place(coachBody,14,39,contentWidth,height-44);
            var content=coachText.transform.parent.Cast<RectTransform>();
            content.sizeDelta=new(contentWidth,preferred);
            Place(coachText.rectTransform,0,0,contentWidth,preferred);
        }
    }

    private RectTransform Plate(string name,Transform parent)
    {
        var border=Box(name,parent,new(.20f,.21f,.24f,.35f));
        var inner=Box("Inner",border,Ink);inner.anchorMin=Vector2.zero;inner.anchorMax=Vector2.one;
        inner.offsetMin=new(1,1);inner.offsetMax=new(-1,-1);
        return border;
    }
    private static RectTransform Rect(string name,Transform parent)
    {
        var r=new GameObject(name).AddComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);return r;
    }
    private static RectTransform Box(string name,Transform parent,Color color)
    {var r=Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
    private TextMeshProUGUI Text(string name,Transform parent,string value,float size,Color color,FontStyles style=FontStyles.Normal)
    {
        var t=Rect(name,parent).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;
        t.fontStyle=style;t.color=color;t.richText=true;t.raycastTarget=false;t.enableWordWrapping=true;
        t.alignment=TextAlignmentOptions.TopLeft;t.overflowMode=TextOverflowModes.Ellipsis;return t;
    }
    private static void Place(RectTransform r,float x,float y,float w,float h)
    {r.anchoredPosition=new(x,-y);r.sizeDelta=new(w,h);}
}

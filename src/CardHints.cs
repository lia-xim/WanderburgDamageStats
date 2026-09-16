using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace WanderburgDamageHUD;
internal sealed record CardHint(ModuleUpgradeOption Option,string Text,bool Lead);

// Screen-space labels follow the actual card rectangles, without intercepting selection clicks.
internal sealed class CardHints
{
    readonly Transform parent;
    readonly TMP_FontAsset font;
    readonly List<TextMeshProUGUI> labels=new();
    readonly Il2CppStructArray<Vector3> corners=new(4);
    internal CardHints(Transform parent,TMP_FontAsset font){this.parent=parent;this.font=font;}
    internal void Render(CardHint[] hints)
    {
        while(labels.Count<hints.Length)
        {
            var r=new GameObject("Card damage hint").AddComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new(0,1);
            var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.richText=true;
            text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.enableWordWrapping=true;
            text.outlineWidth=.15f;text.outlineColor=new Color32(0,0,0,220);
            labels.Add(text);
        }
        for(int i=0;i<labels.Count;i++)
        {
            var label=labels[i];label.gameObject.SetActive(false);if(i>=hints.Length)continue;
            var hint=hints[i];if(!hint.Option || !hint.Option.gameObject.activeInHierarchy)continue;
            var rect=hint.Option.GetComponent<RectTransform>();if(!rect)continue;
            var cardCanvas=hint.Option.GetComponentInParent<Canvas>();
            Camera camera=cardCanvas && cardCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?cardCanvas.worldCamera:null;
            rect.GetWorldCorners(corners);
            var bottom=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
            var top=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            float scale=Mathf.Clamp(Screen.height/1080f,.5f,3f);
            float height=55*scale,width=Mathf.Min(top.x-bottom.x,420*scale);
            if(width<100*scale || top.y<0 || bottom.y>Screen.height)continue;
            float center=(bottom.x+top.x)*.5f;
            // Prefer the space above the card, otherwise the space below. Never cover its statistics.
            float y=Screen.height-top.y-height-6*scale;
            if(y<3*scale)y=Screen.height-bottom.y+6*scale;
            if(y+height>Screen.height-3*scale)continue;
            label.rectTransform.anchoredPosition=new(Mathf.Clamp(center-width*.5f,0,Screen.width-width),-y);
            label.rectTransform.sizeDelta=new(width,height);
            label.fontSize=12*scale;label.color=hint.Lead?new(.60f,.95f,.70f):new(.95f,.94f,.90f);
            label.text=hint.Text;label.gameObject.SetActive(true);
        }
    }
}

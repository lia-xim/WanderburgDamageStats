using System.Globalization;
using System.Text.RegularExpressions;

namespace WanderburgDamageHUD;

// Read the real rolled offer, not the cosmetic 3D preview (which uses rarity 0).
internal static class PreviewText
{
    internal static string Format(string raw)
    {
        var comparison=Regex.Match(raw,@"<s>(.*?)</s>.*?<b>(.*?)</b>");
        string result=Regex.Replace(raw,@"<sprite[^>]*>"," → ");
        result=Regex.Replace(result,@"<[^>]*>","").Trim();
        if(!comparison.Success) return result;
        var oldText=Regex.Replace(comparison.Groups[1].Value,@"<[^>]*>","").Trim();
        var newText=Regex.Replace(comparison.Groups[2].Value,@"<[^>]*>","").Trim();
        var number=@"^[+-]?\d+(?:[.,]\d+)?$";
        if(!Regex.IsMatch(oldText,number)||!Regex.IsMatch(newText,number)) return result;
        if(!float.TryParse(oldText.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var before)
            || !float.TryParse(newText.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var after)) return result;
        float delta=after-before;
        if(Math.Abs(delta)<.0001f) return result+" (=)";
        string sign=delta>0?"+":"";
        string extra=sign+delta.ToString("0.##",CultureInfo.InvariantCulture);
        if(Math.Abs(before)>.0001f) extra+=" / "+sign+(delta/before*100).ToString("0.#",CultureInfo.InvariantCulture)+"%";
        return result+" ("+extra+")";
    }
}

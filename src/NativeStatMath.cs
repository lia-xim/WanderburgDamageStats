namespace WanderburgDamageHUD;

internal static class NativeStatMath
{
    // Traced from Module2.RecalculateSpeed/Size and RecalculateCooldowns in this game build.
    internal static float Scaled(float basis,float maximum,float added,float artifactBonus) => basis>0 && maximum>0
        ? basis+(maximum-basis)*(1-MathF.Exp(-.00693f*Math.Max(0,added+Math.Max(0,artifactBonus)))) : 0;
    internal static float Cooldown(float basis,float added,float slot,bool active) =>
        Math.Max(active?.5f:.1f,Math.Max(0,basis)*Math.Max(0,slot)/Math.Max(.01f,1+added));
    internal static float Rolled(float[] values,int rarity)
    {
        if(values==null || rarity<0 || rarity>=values.Length || !float.IsFinite(values[rarity]))
            throw new InvalidOperationException("Invalid rarity values");
        return values[rarity];
    }
}

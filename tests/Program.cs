using WanderburgDamageHUD;

var cases = new (string Input,string Expected)[]
{
    ("Damage <s>20</s><sprite name=\"TestArrow\" tint=1><b>25</b>","Damage 20 → 25 (+5 / +25%)"),
    ("Cooldown <s>10</s><sprite name=\"TestArrow\"><b>8</b>","Cooldown 10 → 8 (-2 / -20%)"),
    ("Damage <s>0</s><sprite name=\"TestArrow\"><b>5</b>","Damage 0 → 5 (+5)"),
    ("Damage <s>2,5</s><sprite name=\"TestArrow\"><b>3,75</b>","Damage 2,5 → 3,75 (+1.25 / +50%)"),
    ("Damage <s>20</s><sprite name=\"TestArrow\"><b>20</b>","Damage 20 → 20 (=)"),
    ("Effect <s>Fire</s><sprite name=\"TestArrow\"><b>Ice</b>","Effect Fire → Ice"),
    ("Damage <s>10-20</s><sprite name=\"TestArrow\"><b>20-40</b>","Damage 10-20 → 20-40"),
    ("Damage <s><color=#777>1</color></s><sprite name=\"TestArrow\"><b><color=#69f>46</color></b>","Damage 1 → 46 (+45 / +4500%)"),
    ("<color=red>Special effect</color>","Special effect"),
};
foreach (var test in cases)
{
    var actual=PreviewText.Format(test.Input);
    if(actual!=test.Expected) throw new Exception($"Expected: {test.Expected}\nActual: {actual}");
}
Console.WriteLine($"PASS: {cases.Length} upgrade comparison cases (gain, reduction, zero, decimal comma, unchanged, nonnumeric, range, nested rarity markup, special).");

var damage=BuildCoachCore.Assess(new[]{"Damage <s>40</s><sprite name=\"TestArrow\"><b>60</b> DAMAGE!"},.4f,false);
if(Math.Abs(damage.UpgradeStrength-.5f)>.001f || Math.Abs(damage.EstimatedBuildGain!.Value-.2f)>.001f)
    throw new Exception($"Damage assessment incorrect: {damage}");

var cooldown=BuildCoachCore.Assess(new[]{"<s>30s</s><sprite name=\"TestArrow\"><b>26s</b> COOLDOWN ABILITY"},.5f,false);
if(Math.Abs(cooldown.UpgradeStrength-.061538f)>.001f || cooldown.MainReason!="COOLDOWN ABILITY: 30 → 26" || !cooldown.HasUnmodelledEffect)
    throw new Exception($"Cooldown assessment incorrect: {cooldown}");

var charges=BuildCoachCore.Assess(new[]{"<s>4</s><sprite name=\"TestArrow\"><b>7</b> CHARGES!"},1f,false);
if(Math.Abs(charges.UpgradeStrength-.3f)>.001f || !charges.HasUnmodelledEffect)
    throw new Exception($"Charge assessment incorrect: {charges}");

var special=BuildCoachCore.Assess(Array.Empty<string>(),.8f,true);
if(!special.HasUnmodelledEffect || special.EstimatedBuildGain.GetValueOrDefault()!=0)
    throw new Exception($"Special assessment must remain unmodelled: {special}");

var numericLegendary=BuildCoachCore.Assess(new[]{
    "<s>1</s><sprite name=\"TestArrow\"><b>71</b> FLAT DAMAGE! <s>4</s><sprite name=\"TestArrow\"><b>5</b> DAMAGE MULT",
    "<s>0</s><sprite name=\"TestArrow\"><b>30</b> DAMAGE MULT"
},.676f,true);
if(!numericLegendary.HasUnmodelledEffect || !numericLegendary.IsNumericallyComparable || numericLegendary.EstimatedBuildGain.GetValueOrDefault()<4.8f)
    throw new Exception($"Numeric legendary must remain rankable: {numericLegendary}");
var projected=numericLegendary.ProjectedDps(36.46f);
if(!projected.HasValue || projected.Value<210f)
    throw new Exception($"Projected DPS missing for numeric legendary: {projected}");

var abilityCooldownShare=BuildCoachCore.ResolveAffectedShare("Cooldown",new[]{"15 → 12 COOLDOWN ABILITY"},0f,20f,0f,31f);
if(abilityCooldownShare.GetValueOrDefault()!=0f)
    throw new Exception($"Ability cooldown must not claim auto-attack damage: {abilityCooldownShare}");
var autoCooldownShare=BuildCoachCore.ResolveAffectedShare("Cooldown",new[]{"15 → 12 AUTO COOLDOWN"},0f,20f,0f,31f);
if(Math.Abs(autoCooldownShare.GetValueOrDefault()-(20f/31f))>.001f)
    throw new Exception($"Auto cooldown must use observed auto damage: {autoCooldownShare}");
var unknownCooldownShare=BuildCoachCore.ResolveAffectedShare("Cooldown",new[]{"15 → 12 COOLDOWN ABILITY"},0f,0f,20f,31f);
if(unknownCooldownShare.GetValueOrDefault()!=0f)
    throw new Exception($"Unattributed cooldown damage must remain conservative: {unknownCooldownShare}");

var epicUtility=BuildCoachCore.Assess(new[]{
    "<s>1.6</s><sprite name=\"TestArrow\"><b>2</b> DAMAGE MULT",
    "<s>4s</s><sprite name=\"TestArrow\"><b>5.3s</b> DURATION",
    "<s>0</s><sprite name=\"TestArrow\"><b>50</b> PROJECTILE SPEED"
},.462f,false);
if(Math.Abs(epicUtility.UpgradeStrength-.25f)>.001f || !epicUtility.HasUnmodelledEffect)
    throw new Exception($"Epic utility must score direct damage only and remain conditional: {epicUtility}");

var profileBefore=new WeaponProfile(1.6f,1f,15f,1f,1f,1f,4f,1f,1f,1f,10f,10f);
var commonAfter=profileBefore with { AutoDamage=31f };
var epicAfter=profileBefore with { ActiveDamage=2f, ActiveDuration=5.3f, ActiveSpeed=60f };
var commonProjection=BuildCoachCore.AssessProjected(profileBefore,commonAfter,"Auto",new[]{"1 → 31 FLAT DAMAGE"},.462f,false);
var epicProjection=BuildCoachCore.AssessProjected(profileBefore,epicAfter,"Active",new[]{"1.6 → 2 DAMAGE MULT","4 → 5.3 DURATION","0 → 50 PROJECTILE SPEED"},.462f,true);
if(commonProjection.EstimatedBuildGain.GetValueOrDefault()<=epicProjection.EstimatedBuildGain.GetValueOrDefault())
    throw new Exception($"Full preview comparison should preserve the stronger modeled outcome: common={commonProjection}, epic={epicProjection}");
if(!epicProjection.MainReason.Contains("duration") || !epicProjection.MainReason.Contains("context only") || !epicProjection.MainReason.Contains("speed") || !epicProjection.MainReason.Contains("low confidence"))
    throw new Exception($"Projected utility breakdown is incomplete: {epicProjection.MainReason}");
if(Math.Abs(epicProjection.UpgradeStrength-.25f)>.001f)
    throw new Exception($"Duration and speed must not inflate projected DPS: {epicProjection}");

Console.WriteLine("PASS: 11 Damage Stats recommendation cases (damage share, conservative cooldown/charges, unmodelled special, numeric legendary projection, ability/auto/unknown cooldown channels, epic utility confidence, full preview comparison).");

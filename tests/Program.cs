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
    ("<color=red>Special effect</color>","Special effect"),
};
foreach (var test in cases)
{
    var actual=PreviewText.Format(test.Input);
    if(actual!=test.Expected) throw new Exception($"Expected: {test.Expected}\nActual: {actual}");
}
Console.WriteLine($"PASS: {cases.Length} upgrade comparison cases (gain, reduction, zero, decimal comma, unchanged, nonnumeric, range, special).");

var damage=BuildCoachCore.Assess(new[]{"Damage <s>40</s><sprite name=\"TestArrow\"><b>60</b> DAMAGE!"},.4f,false);
if(Math.Abs(damage.UpgradeStrength-.5f)>.001f || Math.Abs(damage.EstimatedBuildGain!.Value-.2f)>.001f)
    throw new Exception($"Damage assessment incorrect: {damage}");

var cooldown=BuildCoachCore.Assess(new[]{"<s>30s</s><sprite name=\"TestArrow\"><b>26s</b> COOLDOWN ABILITY"},.5f,false);
if(cooldown.UpgradeStrength<.15f || cooldown.UpgradeStrength>.16f || cooldown.MainReason!="COOLDOWN ABILITY: 30 → 26")
    throw new Exception($"Cooldown assessment incorrect: {cooldown}");

var charges=BuildCoachCore.Assess(new[]{"<s>4</s><sprite name=\"TestArrow\"><b>7</b> CHARGES!"},1f,false);
if(Math.Abs(charges.UpgradeStrength-.4875f)>.001f)
    throw new Exception($"Charge assessment incorrect: {charges}");

var special=BuildCoachCore.Assess(Array.Empty<string>(),.8f,true);
if(!special.HasUnmodelledEffect || special.EstimatedBuildGain.GetValueOrDefault()!=0)
    throw new Exception($"Special assessment must remain unmodelled: {special}");

Console.WriteLine("PASS: 4 Damage Stats recommendation cases (damage share, cooldown benefit, charge weighting, unmodelled special).");

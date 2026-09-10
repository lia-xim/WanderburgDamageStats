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

ModelChecks.Run();
FutureChecks.Run();

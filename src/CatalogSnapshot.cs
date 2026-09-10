using System.Reflection;
using System.Text.Json;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace WanderburgDamageHUD;

// Opt-in local diagnostics. Reads loaded assets and references, never applies upgrades.
internal static class CatalogSnapshot
{
    private static bool attempted;
    internal static void TryExport(GM gm)
    {
        if(attempted || !Plugin.ExportCatalog.Value || !gm || !gm.ms || Time.unscaledTime<5) return;
        if(gm.ms.baseModulePrefabPool==null || gm.ms.baseModulePrefabPool.Count==0) return;
        attempted=true;
        try
        {
            var upgrades=Resources.FindObjectsOfTypeAll<ModuleUpgrade>().Where(x=>x).ToArray();
            var modules=Resources.FindObjectsOfTypeAll<Module2>().Where(x=>x).ToArray();
            var effects=Resources.FindObjectsOfTypeAll<ProjectileV2>().Where(x=>x).ToArray();
            var snapshot=new { scope="Loaded assets only; pool membership is not proof of player unlocks or future offer probability",
                modules=modules.Select(Values).ToArray(),upgrades=upgrades.Select(Values).ToArray(),
                projectiles=effects.Select(Values).ToArray(),
                basePool=Names(gm.ms.baseModulePrefabPool),
                eternalPool=Names(gm.ms.baseModulePrefabEternalPool) };
            string path=Path.Combine(BepInEx.Paths.ConfigPath,"WanderburgDamageStats.catalog.json");
            File.WriteAllText(path,JsonSerializer.Serialize(snapshot,new JsonSerializerOptions{WriteIndented=true}));
            Plugin.Logger.LogInfo($"Catalog exported: {modules.Length} module objects, {upgrades.Length} upgrades, {effects.Length} projectiles to {path}");
            Plugin.ExportCatalog.Value=false;
        }
        catch(Exception ex) { Plugin.Logger.LogWarning("Catalog export failed: "+ex.Message); }
    }

    private static string[] Names<T>(Il2CppSystem.Collections.Generic.List<T> list) where T:UnityEngine.Object
    {
        var names=new List<string>();
        if(list!=null) foreach(var item in list) names.Add(item?item.name:"");
        return names.ToArray();
    }

    private static Dictionary<string,object> Values(UnityEngine.Object source)
    {
        var row=new Dictionary<string,object>{{"asset",source.name},{"instance",source.GetInstanceID()}};
        foreach(var property in source.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
        {
            var t=property.PropertyType;
            if(property.GetIndexParameters().Length!=0 || !(t.IsPrimitive || t.IsEnum || t==typeof(string) ||
                t==typeof(Il2CppStructArray<float>) || t==typeof(Il2CppStructArray<int>) ||
                t==typeof(Il2CppSystem.Collections.Generic.List<ModuleUpgrade>) ||
                t==typeof(GameObject) || t==typeof(ModuleUpgrade))) continue;
            try
            {
                object value=property.GetValue(source);
                row[property.Name]=value switch {
                    null=>null,
                    float f when !float.IsFinite(f)=>f.ToString(),
                    Il2CppStructArray<float> floats=>floats.ToArray(),
                    Il2CppStructArray<int> ints=>ints.ToArray(),
                    Il2CppSystem.Collections.Generic.List<ModuleUpgrade> list=>Names(list),
                    UnityEngine.Object reference=>reference?reference.name:null,
                    Enum enumeration=>enumeration.ToString(),
                    _=>value
                };
            }
            catch(Exception ex) { row[property.Name]="unreadable: "+ex.GetType().Name; }
        }
        return row;
    }
}

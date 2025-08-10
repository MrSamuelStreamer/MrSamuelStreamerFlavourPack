using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ResourceGenerator;
using UnityEngine;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

public class MSSFPResourceGeneratorMod : Mod
{
    public static Settings settings;

    public MSSFPResourceGeneratorMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from MSSFPResourceGeneratorMod");

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.ResourceGenerator.main");
        harmony.PatchAll();

        Type compResourceSpawnerType = AccessTools.TypeByName("ResourceGenerator.CompResourceSpawner");
        MethodInfo tryDoSpawnMethod = AccessTools.Method(compResourceSpawnerType, "tryDoSpawn");

        MethodInfo patch = AccessTools.Method(typeof(CompResourceSpawner_Patch), nameof(CompResourceSpawner_Patch.Transpiler));

        harmony.Patch(tryDoSpawnMethod, null, null, new HarmonyMethod(patch));
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "MSSFP_RGSettingsCategory".Translate();
    }

    public static void UpdateExtras()
    {
        if (Main.ValidResources.NullOrEmpty())
        {
            ModLog.Warn("ResourceGenerator.Main.ValidResources null or empty");
            return;
        }
        Main.ValidResources.Clear();

        Main.ValidResources.AddRange(DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsStuff));
        Main.ValidResources.AddRange(DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.mineable).Select(def => def.building.mineableThing));

        if (settings.ExtraBuildables != null)
        {
            Main.ValidResources.AddRange(settings.ExtraBuildables);
        }

        Main.ValidResources.RemoveWhere(t => t == null);

        ModLog.Log($"[ResourceGenerator]: Added {Main.ValidResources.Count} resources as possible to generate");
    }
}

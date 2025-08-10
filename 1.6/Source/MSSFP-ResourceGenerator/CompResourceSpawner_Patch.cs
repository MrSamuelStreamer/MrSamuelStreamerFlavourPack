using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

// [HarmonyPatch(typeof(CompResourceSpawner))]
public static class CompResourceSpawner_Patch
{
    // [HarmonyPatch("tryDoSpawn")]
    // [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo thingMakerMakeThingMethod = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing));
        MethodInfo spawnThingMethod = AccessTools.Method(typeof(CompResourceSpawner_Patch), nameof(SpawnThing));

        bool found = false;

        foreach (CodeInstruction instruction in instructions)
        {
            if (!found && instruction.Calls(thingMakerMakeThingMethod))
            {
                found = true;
                yield return new CodeInstruction(OpCodes.Call, spawnThingMethod);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    public static Thing SpawnThing(ThingDef def, ThingDef stuff = null)
    {
        if (def.MadeFromStuff && stuff == null)
        {
            stuff = GenStuff.RandomStuffByCommonalityFor(def);
        }
        return ThingMaker.MakeThing(def, stuff);
    }
}

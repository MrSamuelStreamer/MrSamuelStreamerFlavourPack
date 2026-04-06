using HarmonyLib;
using RimWorld;
using VanillaQuestsExpandedTheGenerator;

namespace MSSFP.GeneratorMod;

/// <summary>
/// Accelerates ARC timer progression 4x by adding 3 extra ticks to totalRunningTicks
/// whenever the generator would naturally increment it. Replaces 9 fragile iterator
/// transpilers that targeted compiler-generated MoveNext methods (ISSUE_007).
/// </summary>
[HarmonyPatch(typeof(Building_Genetron), "Tick")]
public static class Building_Genetron_Tick_ARCTimer_Patch
{
    public static void Postfix(Building_Genetron __instance)
    {
        if (((CompPowerTrader)__instance.compPower).PowerOn
            && (__instance.compRefuelable == null || __instance.compRefuelable.HasFuel)
            && (__instance.compBreakdownable == null || !__instance.compBreakdownable.BrokenDown))
        {
            __instance.totalRunningTicks += 3;
        }
    }
}

using HarmonyLib;
using MSSFP.Comps.Map;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Echolocation psycast (C4): pushes blip events for the sound-heatmap
/// (T8) into the caster map's EcholocationMapComponent. Each patch is a
/// no-op while no cast is active — EcholocationMapComponent.AddBlip
/// early-returns unless Active.
/// </summary>
[HarmonyPatch(typeof(Pawn_PathFollower))]
[HarmonyPatch("TryEnterNextPathCell")]
public static class PathFollower_Footstep_BlipPatch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn ___pawn)
    {
        if (___pawn?.Map == null)
            return;

        ___pawn.Map.GetComponent<EcholocationMapComponent>()?.AddBlip(___pawn.Position, 0.3f);
    }
}

[HarmonyPatch(typeof(Verb_LaunchProjectile))]
[HarmonyPatch("TryCastShot")]
public static class Verb_LaunchProjectile_Shot_BlipPatch
{
    [HarmonyPostfix]
    public static void Postfix(Verb __instance, bool __result)
    {
        if (!__result || __instance.caster?.Map == null)
            return;

        __instance.caster.Map.GetComponent<EcholocationMapComponent>()
            ?.AddBlip(__instance.caster.Position, 1f);
    }
}

[HarmonyPatch(typeof(Verb_MeleeAttack))]
[HarmonyPatch("TryCastShot")]
public static class Verb_MeleeAttack_BlipPatch
{
    [HarmonyPostfix]
    public static void Postfix(Verb __instance)
    {
        if (__instance.caster?.Map == null)
            return;

        __instance.caster.Map.GetComponent<EcholocationMapComponent>()
            ?.AddBlip(__instance.caster.Position, 0.6f);
    }
}

[HarmonyPatch(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))]
public static class GenExplosion_BlipPatch
{
    [HarmonyPostfix]
    public static void Postfix(IntVec3 center, Map map)
    {
        map?.GetComponent<EcholocationMapComponent>()?.AddBlip(center, 1.5f);
    }
}

[HarmonyPatch(typeof(Building_Door))]
[HarmonyPatch("DoorOpen")]
public static class Building_Door_Open_BlipPatch
{
    [HarmonyPostfix]
    public static void Postfix(Building_Door __instance)
    {
        __instance.Map?.GetComponent<EcholocationMapComponent>()
            ?.AddBlip(__instance.Position, 0.5f);
    }
}

[HarmonyPatch(typeof(Building_Door))]
[HarmonyPatch("DoorTryClose")]
public static class Building_Door_Close_BlipPatch
{
    [HarmonyPostfix]
    public static void Postfix(Building_Door __instance, bool __result)
    {
        if (!__result)
            return;

        __instance.Map?.GetComponent<EcholocationMapComponent>()
            ?.AddBlip(__instance.Position, 0.5f);
    }
}

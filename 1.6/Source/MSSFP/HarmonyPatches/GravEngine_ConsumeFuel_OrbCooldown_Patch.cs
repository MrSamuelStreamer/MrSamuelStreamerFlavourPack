using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Reduces post-jump cooldown on <see cref="Building_GravEngine"/> when a powered,
/// persona-loaded Pondering Orb sits on the engine's substructure at launch time.
///
/// Lore: the persona AI pre-spools the engine's coil dampers during the burn, contracting
/// the capacitor recharge window.
///
/// Prefix captures <c>cooldownCompleteTick</c> before vanilla runs; postfix reads it after.
/// If vanilla extended the cooldown this call (and only then), we shave the remaining delta
/// by <see cref="CooldownMultiplier"/>. The before/after delta capture survives any future
/// refactor that moves the cooldown write to a different line inside <c>ConsumeFuel</c> —
/// the postfix becomes a safe no-op if the value didn't grow.
///
/// Two defensive guards on the math:
///   1. Skip if <c>delta &lt;= 0</c> — should never happen on a real launch, but cheap insurance
///      against unusual quality inputs (dev cheats, future-content edge cases).
///   2. Skip if <c>reduced &gt;= delta</c> — guards the int-cast truncation fence-post where
///      a 1-tick delta could otherwise be rewritten to a value equal to or larger than the
///      vanilla one. Never lengthen.
/// </summary>
[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.ConsumeFuel))]
public static class GravEngine_ConsumeFuel_OrbCooldown_Patch
{
    public const float CooldownMultiplier = 0.7f;

    [HarmonyPrefix]
    public static void Prefix(Building_GravEngine __instance, out int __state)
    {
        __state = __instance.cooldownCompleteTick;
    }

    [HarmonyPostfix]
    public static void Postfix(Building_GravEngine __instance, int __state, PlanetTile tile)
    {
        int after = __instance.cooldownCompleteTick;
        if (after <= __state)
            return;

        if (!OrbGravshipAssist.TryGetActiveAssistOrb(__instance, out CompTrueAICore orb))
            return;

        int now = GenTicks.TicksGame;
        int delta = after - now;
        if (delta <= 0)
            return;

        int reduced = Mathf.Max(1, (int)(delta * CooldownMultiplier));
        if (reduced >= delta)
            return;

        __instance.cooldownCompleteTick = now + reduced;

        Messages.Message(
            "MSSFP_PonderingOrbCooldownReduced".Translate(orb.activePersonality.LabelCap),
            __instance,
            MessageTypeDefOf.PositiveEvent,
            historical: false);
    }
}

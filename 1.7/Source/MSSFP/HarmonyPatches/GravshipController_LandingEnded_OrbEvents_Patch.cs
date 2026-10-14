using System;
using HarmonyLib;
using MSSFP.Comps;
using MSSFP.Gravship;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Fires the rare "route-calculating AI did something" events on an orb-assisted gravship landing.
///
/// Postfix on <see cref="WorldComponent_GravshipController.LandingEnded"/> — the same vanilla hook
/// VGE postfixes for its own landing outcomes (multiple postfixes coexist). Running here (not at
/// launch) means damage / knockdowns / haunts land on the destination map the player is watching,
/// and the ship's buildings + colonists are already placed.
///
/// The entire body is wrapped in a single try/catch that logs and returns — a misfiring flavour
/// event must NEVER be able to break vanilla/VGE landing finalization. The gravship is read via
/// <see cref="Traverse"/> so the patch is robust regardless of field accessibility; the destination
/// map is simply <c>engine.Map</c> (the engine has been spawned there by now).
/// </summary>
[HarmonyPatch(typeof(WorldComponent_GravshipController), "LandingEnded")]
public static class GravshipController_LandingEnded_OrbEvents_Patch
{
    [HarmonyPostfix]
    public static void Postfix(WorldComponent_GravshipController __instance)
    {
        try
        {
            if (MSSFPMod.settings == null || !MSSFPMod.settings.EnableOrbJumpEvents)
                return;

            RimWorld.Planet.Gravship gravship = Traverse
                .Create(__instance)
                .Field("gravship")
                .GetValue<RimWorld.Planet.Gravship>();

            Building_GravEngine engine = gravship?.Engine;
            if (engine == null || engine.Map == null)
                return;
            if (engine.Faction != Faction.OfPlayer)
                return;

            // Relaxed power gate: destination PowerNet may not have recalced this tick; the orb
            // already proved powered at launch (it travelled with the ship).
            if (!OrbGravshipAssist.TryGetActiveAssistOrb(engine, out CompTrueAICore orb, requirePower: false))
                return;

            if (!Rand.Chance(MSSFPMod.settings.OrbJumpEventChance))
                return;

            OrbJumpEventManager.TryRunJumpEvent(gravship, engine, orb);
        }
        catch (Exception e)
        {
            Log.Error($"[MSSFP] Orb jump-event postfix failed (landing not affected): {e}");
        }
    }
}

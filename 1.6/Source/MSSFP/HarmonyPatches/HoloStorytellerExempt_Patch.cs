using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// M4 — exempt holo projections from storyteller intent inflation.
///
/// Holos are Player-faction humanlikes that report <c>IsFreeColonist=True</c> and
/// <c>IsColonist=True</c>. Without filtering, vanilla storyteller math
/// (<see cref="StorytellerUtility.DefaultThreatPointsNow"/> +
/// <see cref="StorytellerUtilityPopulation.AdjustedPopulation"/>) treats each holo as a full
/// colonist, inflating raid points and depressing population intent. Holos are projections —
/// they should not contribute to either signal.
///
/// PATCHES (3):
///   - <see cref="Map_PlayerPawnsForStoryteller_HoloExempt_Patch"/> — strips holos from the
///     per-map storyteller pawn enumeration.
///   - <see cref="World_PlayerPawnsForStoryteller_HoloExempt_Patch"/> — strips holos from the
///     world-aggregate enumeration. Without this, <c>World.PlayerPawnsForStoryteller</c>
///     bypasses the Map filter via <c>PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction</c>.
///   - <see cref="StorytellerUtilityPopulation_AdjustedPopulation_HoloExempt_Patch"/> —
///     subtracts holo contribution from the population getter.
///
/// OUT OF SCOPE (v1):
///   - <c>Caravan</c> + <c>Gravship</c> <c>PlayerPawnsForStoryteller</c> impls. Holos cannot
///     leave their projector's map (<see cref="CompHoloProjected.CompTickRare"/> cross-map
///     guard + projector-side recall on power loss), so those enumerations never see them
///     in practice.
///   - Wealth exclusion. Holo <c>MarketValue</c> already resolves to 0 (race statBase=0), so
///     unequipped holos contribute zero to <see cref="WealthWatcher"/>. RESIDUAL: if a player
///     equips weapons/apparel onto a holo, the gear is tallied via <c>WealthItems</c> (pawn-
///     held things) and inflates wealth — which feeds <c>PointsPerColonistByWealthCurve</c>.
///     Address with a separate equip/apparel gate (matches existing IsHolo gate pattern).
/// </summary>
[HarmonyPatch(typeof(Map), nameof(Map.PlayerPawnsForStoryteller), MethodType.Getter)]
public static class Map_PlayerPawnsForStoryteller_HoloExempt_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<Pawn> __result)
    {
        if (__result == null)
            return;
        __result = __result.Where(p => !MSSFPHoloUtil.IsHolo(p));
    }
}

/// <summary>
/// World-aggregate variant. <c>World.PlayerPawnsForStoryteller</c> returns
/// <c>PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction</c>, which
/// iterates pawn lists directly and bypasses <see cref="Map.PlayerPawnsForStoryteller"/>.
/// Storyteller paths that target the World (planetwide events) would otherwise still see holos.
/// </summary>
[HarmonyPatch(typeof(World), nameof(World.PlayerPawnsForStoryteller), MethodType.Getter)]
public static class World_PlayerPawnsForStoryteller_HoloExempt_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<Pawn> __result)
    {
        if (__result == null)
            return;
        __result = __result.Where(p => !MSSFPHoloUtil.IsHolo(p));
    }
}

/// <summary>
/// Subtract holo contribution from <see cref="StorytellerUtilityPopulation.AdjustedPopulation"/>.
///
/// Vanilla iterates <c>PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive</c>, filters
/// <c>IsColonist || IsPrisonerOfColony || IsSlaveOfColony</c>, sums a private
/// <c>AdjustedPopulationValue(pawn)</c>. We reproduce that scoring inline for holos only and
/// subtract from <c>__result</c>. <see cref="StorytellerUtilityPopulation.AdjustedPopulationIncludingQuests"/>
/// reads <c>AdjustedPopulation</c> and adds quest count, so the cascade is implicit.
///
/// IMPLEMENTATION NOTE: <c>AdjustedPopulationValue</c> is private. We re-implement the same
/// scoring rules (baby=0, adult=1, child=lerp, ×prisoner/slave factors). If vanilla scoring
/// changes between RimWorld versions, this needs updating.
/// </summary>
[HarmonyPatch(typeof(StorytellerUtilityPopulation), nameof(StorytellerUtilityPopulation.AdjustedPopulation), MethodType.Getter)]
public static class StorytellerUtilityPopulation_AdjustedPopulation_HoloExempt_Patch
{
    private const float PopulationValue_Prisoner = 0.5f;
    private const float PopulationValue_PrisonerUnrecruitable = 0.25f;
    private const float PopulationValue_Slave = 0.75f;
    private const float PopulationValue_Child = 0.3f;

    [HarmonyPostfix]
    public static void Postfix(ref float __result)
    {
        float holoSum = 0f;
        foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive)
        {
            if (!MSSFPHoloUtil.IsHolo(pawn))
                continue;
            if (!(pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony))
                continue;
            holoSum += AdjustedPopulationValueClone(pawn);
        }

        __result -= holoSum;
        if (__result < 0f)
            __result = 0f;
    }

    /// <summary>
    /// Mirror of vanilla private <c>StorytellerUtilityPopulation.AdjustedPopulationValue</c>.
    /// Kept in sync manually — flag for review on RimWorld version bumps.
    /// </summary>
    private static float AdjustedPopulationValueClone(Pawn pawn)
    {
        if (pawn.DevelopmentalStage.Baby())
            return 0f;

        float num = pawn.DevelopmentalStage.Adult()
            ? 1f
            : Mathf.Lerp(PopulationValue_Child, 1f, (float)pawn.ageTracker.AgeBiologicalYears / pawn.ageTracker.AdultMinAge);

        if (pawn.IsPrisonerOfColony)
        {
            num *= pawn.guest.Recruitable ? PopulationValue_Prisoner : PopulationValue_PrisonerUnrecruitable;
        }
        else if (pawn.IsSlaveOfColony)
        {
            num *= PopulationValue_Slave;
        }

        return num;
    }
}

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Defs;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Postfix on TryInteractWith — fires after any successful social interaction.
/// If both the initiator and recipient are haunted, evaluates haunt interactions.
/// Low-traffic: only executes when the interaction actually succeeded.
/// </summary>
[HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
public static class HauntSocialInteraction_Patch
{
    [HarmonyPostfix]
    public static void TryInteractWith_Postfix(
        Pawn ___pawn,
        Pawn recipient,
        bool __result
    )
    {
        if (!__result)
            return;

        Pawn initiator = ___pawn;
        if (initiator?.Map == null || recipient?.Map == null)
            return;

        HediffComp_Haunt hauntA = GetNamedHaunt(initiator);
        HediffComp_Haunt hauntB = GetNamedHaunt(recipient);
        if (hauntA == null || hauntB == null)
            return;

        HauntInteractionHandler.TryFireInteraction(initiator, hauntA, recipient, hauntB);
    }

    private static HediffComp_Haunt GetNamedHaunt(Pawn pawn)
    {
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is not HediffWithComps hwc)
                continue;
            foreach (HediffComp comp in hwc.comps)
            {
                if (comp is HediffComp_Haunt haunt && haunt.Props.archetype != null)
                    return haunt;
            }
        }
        return null;
    }
}

/// <summary>
/// Resolves and applies haunt interactions between two haunted pawns.
/// </summary>
public static class HauntInteractionHandler
{
    private const float AmplificationSeverityDelta = 0.05f;

    public static void TryFireInteraction(
        Pawn pawnA,
        HediffComp_Haunt hauntA,
        Pawn pawnB,
        HediffComp_Haunt hauntB
    )
    {
        // Priority 1: same haunt def → Amplification
        if (hauntA.parent.def == hauntB.parent.def)
        {
            if (Rand.Chance(0.3f))
                ApplyAmplification(hauntA, hauntB);
            return;
        }

        // Priority 2: iconic pair override
        HauntInteractionDef iconicDef = FindIconicPairDef(hauntA.parent.def, hauntB.parent.def);
        if (iconicDef != null)
        {
            if (Rand.Chance(iconicDef.chancePerSocialInteraction))
                ApplyInteraction(iconicDef, pawnA, hauntA, pawnB, hauntB);
            return;
        }

        // Priority 3: archetype pair
        HauntArchetypeDef archetypeA = hauntA.Props.archetype;
        HauntArchetypeDef archetypeB = hauntB.Props.archetype;
        if (archetypeA == null || archetypeB == null)
            return;

        HauntInteractionDef archetypeDef = FindArchetypePairDef(archetypeA, archetypeB);
        if (archetypeDef != null && Rand.Chance(archetypeDef.chancePerSocialInteraction))
            ApplyInteraction(archetypeDef, pawnA, hauntA, pawnB, hauntB);
    }

    private static void ApplyAmplification(HediffComp_Haunt hauntA, HediffComp_Haunt hauntB)
    {
        hauntA.parent.Severity = Mathf.Clamp(
            hauntA.parent.Severity + AmplificationSeverityDelta,
            0.01f,
            1f
        );
        hauntB.parent.Severity = Mathf.Clamp(
            hauntB.parent.Severity + AmplificationSeverityDelta,
            0.01f,
            1f
        );
    }

    /// <summary>
    /// Applies a HauntInteractionDef's effects. pawnA/hauntA correspond to defA/archetypeA;
    /// the pair is order-independent — we normalise before calling.
    /// </summary>
    private static void ApplyInteraction(
        HauntInteractionDef def,
        Pawn pawnA,
        HediffComp_Haunt hauntA,
        Pawn pawnB,
        HediffComp_Haunt hauntB
    )
    {
        // Normalise: ensure pawnA carries the haunt matching def's A side
        if (!MatchesSideA(def, hauntA))
        {
            (pawnA, pawnB) = (pawnB, pawnA);
            (hauntA, hauntB) = (hauntB, hauntA);
        }

        // Thoughts
        TryGiveThought(pawnA, def.thoughtForA, pawnB);
        TryGiveThought(pawnB, def.thoughtForB, pawnA);

        // Bystander thoughts
        if (def.thoughtForBystanders != null && def.bystanderRadius > 0f)
        {
            foreach (Pawn bystander in GetBystanders(pawnA, pawnB, def.bystanderRadius))
                TryGiveThought(bystander, def.thoughtForBystanders, null);
        }

        // Severity deltas
        if (def.severityDeltaA != 0f)
            hauntA.parent.Severity = Mathf.Clamp(
                hauntA.parent.Severity + def.severityDeltaA,
                0.01f,
                1f
            );
        if (def.severityDeltaB != 0f)
            hauntB.parent.Severity = Mathf.Clamp(
                hauntB.parent.Severity + def.severityDeltaB,
                0.01f,
                1f
            );

        // Temporary hediffs
        if (def.tempHediffForA != null)
            pawnA.health.AddHediff(def.tempHediffForA);
        if (def.tempHediffForB != null)
            pawnB.health.AddHediff(def.tempHediffForB);

        // Special: item teleport (Mischief)
        if (def.teleportNearbyItem)
            TryTeleportNearbyItem(pawnA, pawnB);
    }

    private static bool MatchesSideA(HauntInteractionDef def, HediffComp_Haunt haunt)
    {
        if (def.hauntDefA != null)
            return haunt.parent.def == def.hauntDefA;
        if (def.archetypeA != null)
            return haunt.Props.archetype == def.archetypeA;
        return true;
    }

    private static HauntInteractionDef FindIconicPairDef(HediffDef defA, HediffDef defB)
    {
        foreach (HauntInteractionDef d in DefDatabase<HauntInteractionDef>.AllDefsListForReading)
        {
            if (d.hauntDefA == null || d.hauntDefB == null)
                continue;
            if ((d.hauntDefA == defA && d.hauntDefB == defB)
                || (d.hauntDefA == defB && d.hauntDefB == defA))
                return d;
        }
        return null;
    }

    private static HauntInteractionDef FindArchetypePairDef(
        HauntArchetypeDef archetypeA,
        HauntArchetypeDef archetypeB
    )
    {
        foreach (HauntInteractionDef d in DefDatabase<HauntInteractionDef>.AllDefsListForReading)
        {
            if (d.archetypeA == null || d.archetypeB == null)
                continue;
            if ((d.archetypeA == archetypeA && d.archetypeB == archetypeB)
                || (d.archetypeA == archetypeB && d.archetypeB == archetypeA))
                return d;
        }
        return null;
    }

    private static void TryGiveThought(Pawn pawn, ThoughtDef thought, Pawn otherPawn)
    {
        if (thought == null)
            return;
        if (pawn.needs?.mood?.thoughts?.memories == null)
            return;
        Thought_Memory memory = (Thought_Memory)ThoughtMaker.MakeThought(thought);
        pawn.needs.mood.thoughts.memories.TryGainMemory(memory, otherPawn);
    }

    private static IEnumerable<Pawn> GetBystanders(Pawn pawnA, Pawn pawnB, float radius)
    {
        HashSet<Pawn> seen = new();
        foreach (Pawn centre in new[] { pawnA, pawnB })
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(centre.Position, radius, true))
            {
                foreach (Thing thing in centre.Map.thingGrid.ThingsAt(cell))
                {
                    if (thing is not Pawn p)
                        continue;
                    if (p == pawnA || p == pawnB)
                        continue;
                    if (!p.RaceProps.Humanlike || !p.IsColonist)
                        continue;
                    if (seen.Add(p))
                        yield return p;
                }
            }
        }
    }

    private static void TryTeleportNearbyItem(Pawn pawnA, Pawn pawnB)
    {
        Map map = pawnA.Map;
        const float searchRadius = 5f;
        const int teleportMin = 2;
        const int teleportMax = 3;

        // Gather candidate items near either pawn
        List<Thing> candidates = new();
        foreach (Pawn centre in new[] { pawnA, pawnB })
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(centre.Position, searchRadius, true))
            {
                if (!cell.InBounds(map))
                    continue;
                foreach (Thing thing in map.thingGrid.ThingsAt(cell))
                {
                    if (thing is not Pawn && thing.def.EverHaulable)
                        candidates.Add(thing);
                }
            }
        }

        if (candidates.Count == 0)
            return;

        Thing target = candidates.RandomElement();
        int dist = Rand.RangeInclusive(teleportMin, teleportMax);
        IntVec3 dest = target.Position + GenAdj.CardinalDirections[Rand.RangeInclusive(0, 3)] * dist;

        if (!dest.InBounds(map) || !dest.Walkable(map))
            return;

        target.DeSpawn();
        GenPlace.TryPlaceThing(target, dest, map, ThingPlaceMode.Near);
    }
}

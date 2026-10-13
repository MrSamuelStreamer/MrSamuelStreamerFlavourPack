using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Questing;

public class ThingSetMaker_TrekCharacter : ThingSetMaker
{
    private const float RelationWithColonistWeight = 20f;
    private const int MaxTries = 20;

    public static List<TraitDef> Traits =>
        [TraitDefOf.Bisexual, TraitDefOf.Joyous, TraitDefOf.GreatMemory];
    public static List<XenotypeDef> XenoTypes =>
        new List<XenotypeDef> { XenotypeDefOf.Baseliner, DefDatabase<XenotypeDef>.GetNamedSilentFail("Genie") }
            .Where(x => x != null)
            .ToList();

    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        List<XenotypeDef> xenoTypes = XenoTypes;
        if (xenoTypes.Count == 0)
        {
            Log.Warning("[MSSFP] TrekCharacter: no valid xenotypes available; skipping pawn generation.");
            return;
        }

        PawnGenerationRequest request = new(
            MSSFPDefOf.MSSFP_TrekCrasher,
            null,
            colonistRelationChanceFactor: 0f,
            allowPregnant: true,
            forceRecruitable: true,
            biologicalAgeRange: new FloatRange(18, 65),
            forcedTraits: Traits,
            allowedXenotypes: xenoTypes
        );
        int tries = 0;
        Pawn pawn = null;
        string reason = null;
        do
        {
            reason = null;
            if (pawn != null)
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
            pawn = PawnGenerator.GeneratePawn(request);
            HealthUtility.DamageUntilDowned(pawn);
        } while (tries++ < MaxTries && !ValidatePawn(pawn, out reason));

        if (pawn == null)
        {
            Log.Warning($"[MSSFP] TrekCharacter: failed to generate any pawn in {MaxTries} tries.");
            return;
        }

        if (reason != null)
            Log.Warning($"[MSSFP] TrekCharacter: using best-effort pawn after {MaxTries} tries ({reason}).");

        pawn.health.AddHediff(
            HediffDefOf.LoveEnhancer,
            pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Torso).FirstOrDefault()
        );
        outThings.Add(pawn);
    }

    public static bool ValidatePawn(Pawn pawn, out string reason)
    {
        reason = null;
        if (pawn is null)
        {
            reason = "Failed to generate a pawn - null pawn";
            return false;
        }

        if (!XenoTypes.Contains(pawn?.genes?.Xenotype))
        {
            reason = "Failed to generate a pawn with a valid xenotype";
            return false;
        }

        if (pawn.story?.Adulthood is not { defName: "MSSFP_Trek" })
        {
            reason = "Failed to generate a pawn with a valid backstory";
            return false;
        }

        if (!Traits.All(t => pawn.story.traits.allTraits.Select(trait => trait.def).Contains(t)))
        {
            reason = "Failed to generate a pawn with all required traits";
            return false;
        }

        return true;
    }

    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
    {
        yield return MSSFPDefOf.MSSFP_TrekCrasher.race;
    }
}

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

    public static List<TraitDef> Traits =>
        [TraitDefOf.Bisexual, TraitDefOf.Joyous, TraitDefOf.GreatMemory];
    public static List<XenotypeDef> XenoTypes =>
        [XenotypeDefOf.Baseliner, DefDatabase<XenotypeDef>.GetNamed("Genie")];

    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        PawnGenerationRequest request = new(
            MSSFPDefOf.MSSFP_TrekCrasher,
            null,
            colonistRelationChanceFactor: 0f,
            allowPregnant: true,
            forceRecruitable: true,
            biologicalAgeRange: new FloatRange(18, 65),
            forcedTraits: Traits,
            allowedXenotypes: XenoTypes
        );
        int tries = 0;
        Pawn pawn = null;
        string reason;
        do
        {
            reason = null;
            if (pawn != null)
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
            pawn = PawnGenerator.GeneratePawn(request);
            HealthUtility.DamageUntilDowned(pawn);
        } while (tries++ < 500 && !ValidatePawn(pawn, out reason));

        if (tries >= 500)
            throw new Exception("Failed to generate a pawn in 500 tries");

        if (reason != null)
            throw new Exception(reason);

        pawn?.health.AddHediff(
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

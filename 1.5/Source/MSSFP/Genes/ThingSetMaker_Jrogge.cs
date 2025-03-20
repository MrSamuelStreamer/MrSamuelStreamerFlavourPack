using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Genes;

public class ThingSetMaker_Jrogge : ThingSetMaker
{
    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        PawnKindDef jrogge = MSSFPDefOf.MSSFP_Jrogge;

        PawnGenerationRequest request = new(jrogge, Faction.OfPlayer, PawnGenerationContext.NonPlayer);
        Pawn pawn = PawnGenerator.GeneratePawn(request);

        outThings.Add(pawn);
    }

    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
    {
        throw new NotImplementedException();
    }
}

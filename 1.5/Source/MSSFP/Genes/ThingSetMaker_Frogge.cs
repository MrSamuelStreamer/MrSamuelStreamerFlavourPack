using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Genes;

public class ThingSetMaker_Frogge : ThingSetMaker
{
    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        PawnKindDef frogge = DefDatabase<PawnKindDef>.GetNamed("MSSFP_Frogge");

        PawnGenerationRequest request = new(frogge, Faction.OfPlayer, PawnGenerationContext.NonPlayer);
        Pawn pawn = PawnGenerator.GeneratePawn(request);

        outThings.Add(pawn);
    }

    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
    {
        throw new NotImplementedException();
    }
}

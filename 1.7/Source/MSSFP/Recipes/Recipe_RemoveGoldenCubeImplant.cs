using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Recipes;

/// <summary>
/// Surgical extraction of the MSSFP_GoldenCubeImplant hediff. Calls the standard
/// implant-removal pipeline (which removes the hediff and applies surgery damage),
/// then spawns a real <c>GoldenCube</c> ThingDef next to the patient.
/// </summary>
public class Recipe_RemoveGoldenCubeImplant : Recipe_RemoveImplant
{
    public override void ApplyOnPawn(
        Pawn pawn,
        BodyPartRecord part,
        Pawn billDoer,
        List<Thing> ingredients,
        Bill bill)
    {
        // Capture map+position before base call — base may down/kill on failure.
        Map map = pawn.MapHeld;
        IntVec3 pos = pawn.PositionHeld;

        base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

        if (!ModsConfig.AnomalyActive) return;

        ThingDef cubeDef = ThingDefOf.GoldenCube;
        if (cubeDef == null) return;

        Thing cube = ThingMaker.MakeThing(cubeDef);
        if (cube == null) return;

        if (map == null)
        {
            map = pawn.Corpse?.MapHeld ?? Find.CurrentMap;
            pos = pawn.Corpse?.PositionHeld ?? pos;
        }

        if (map == null || !pos.IsValid)
        {
            cube.Destroy();
            return;
        }

        GenPlace.TryPlaceThing(cube, pos, map, ThingPlaceMode.Near);

        Messages.Message(
            "MSSFP_GoldenCubeExtracted_Msg".Translate(pawn.LabelShort),
            new LookTargets(cube),
            MessageTypeDefOf.PositiveEvent,
            historical: false);
    }
}

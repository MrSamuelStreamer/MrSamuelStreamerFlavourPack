using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public class HediffComp_Haunt: HediffComp
{
    private HediffCompProperties_Haunt Props => props as HediffCompProperties_Haunt;

    public virtual void DrawAt(Vector3 drawPos)
    {
        if (Props.onlyRenderWhenDrafted && (Pawn.drafter == null || !Pawn.drafter.Drafted))
        {
            return;
        }

        Vector3 offset = new Vector3();

        if (Props.offsets != null)
        {
            if (Props.offsets.Count == 4)
            {
                offset = Props.offsets[Pawn.Rotation.AsInt];
            }
            else
            {
                offset = Props.offsets[0];
            }
        }

        Props.graphicData?.Graphic.Draw(new Vector3(drawPos.x, AltitudeLayer.Pawn.AltitudeFor(), drawPos.z) + offset, Pawn.Rotation, Pawn);
    }

    public override void CompPostMake()
    {
        base.CompPostMake();
        HauntsCache.AddHaunt(Pawn.thingIDNumber, this);
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        HauntsCache.RemoveHaunt(Pawn.thingIDNumber, this);
    }

    public override void CompExposeData()
    {
        base.CompExposeData();

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            HauntsCache.AddHaunt(Pawn.thingIDNumber, this);
        }
    }
}

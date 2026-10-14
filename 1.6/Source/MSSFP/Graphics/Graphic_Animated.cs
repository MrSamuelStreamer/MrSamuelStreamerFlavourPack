using UnityEngine;
using Verse;

namespace MSSFP;

public class Graphic_Animated : Graphic_Indexed
{
    private int currentFrame;
    private int ticksPerFrame = 6;
    private int ticksPrev;

    public override Material MatSingle => subGraphics[currentFrame].MatSingle;

    public override Material MatSingleFor(Thing thing)
    {
        Tick();
        return MatSingle;
    }

    public void Tick()
    {
        int ticksCurrent = Find.TickManager.TicksGame;
        if (ticksCurrent >= ticksPrev + ticksPerFrame)
        {
            ticksPrev = ticksCurrent;
            currentFrame++;
        }

        if (currentFrame >= subGraphics.Length)
            currentFrame = 0;
    }

    public override void DrawWorker(
        Vector3 loc,
        Rot4 rot,
        ThingDef thingDef,
        Thing thing,
        float extraRotation
    )
    {
        if (thingDef == null)
        {
            Log.Error("Graphic_Animated with null thingDef");
            return;
        }

        if (subGraphics == null)
        {
            Log.Error("Graphic_Animated has no subgraphics");
            return;
        }

        Tick();

        Graphic graphic = subGraphics[currentFrame];
        graphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        ShadowGraphic?.DrawWorker(loc, rot, thingDef, thing, extraRotation);
    }
}

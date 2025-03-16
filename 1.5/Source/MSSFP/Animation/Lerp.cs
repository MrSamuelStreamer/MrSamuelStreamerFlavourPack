using UnityEngine;
using Verse;

namespace MSSFP.Animation;

public class Lerp : AnimationNodeBase
{
    public bool done = false;

    public string endX;
    public string endY;
    public string endZ;

    public string ratio;

    public string endFrames;

    public int currentFrame;

    public string target;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref endX, "endX");
        Scribe_Values.Look(ref endY, "endY");
        Scribe_Values.Look(ref endZ, "endZ");
        Scribe_Values.Look(ref ratio, "ratio", "ratio");
        Scribe_Values.Look(ref endFrames, "endFrames", "60");
    }

    public Vector3 End(AnimationContext context) =>
        context.InitialPosition + new Vector3(context.GetOrParseFloat(endX), context.GetOrParseFloat(endY), context.GetOrParseFloat(endZ));

    public override bool Done => done;

    public override void Prepare(AnimationContext context)
    {
        base.Prepare(context);
        currentFrame = 0;
        done = false;
    }

    public override void DoFrame(AnimationContext context)
    {
        if (context.GetOrParseInt(endFrames) <= currentFrame)
        {
            done = true;
            return;
        }

        if (!context.TryGetOrParseFloat(ratio, out float ratioVal))
        {
            ratioVal = currentFrame / (float)context.GetOrParseInt(endFrames);
        }
        context.Position = Vector3.Lerp(context.InitialPosition, End(context), ratioVal);
        currentFrame++;
    }
}

using Verse;

namespace MSSFP.Animation;

public static class AnimationUtils
{
    public static bool TryAnimate(this Thing thing, ThingAnimationDef def)
    {
        if (thing is Pawn)
            return false;

        AnimatedThingHolder holder = (AnimatedThingHolder)ThingMaker.MakeThing(MSSFPDefOf.MSSFPThingAnimationHolder);
        GenSpawn.Spawn(holder, thing.Position, thing.Map);
        return holder.TryAnimateThing(MSSFPDefOf.MSSFPTestAnim, thing);
    }
}

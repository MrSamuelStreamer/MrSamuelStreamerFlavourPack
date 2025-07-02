using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompUseEffect_RimtonDevice : CompUseEffect
{
    public override AcceptanceReport CanBeUsedBy(Pawn p)
    {
        // if (!p.Downed)
        // return (AcceptanceReport)("MSS_Balloon_NotDowned".Translate(p.Named("PAWN")).Resolve().StripTags() ?? "");
        return true;
    }

    public override void DoEffect(Pawn usedBy)
    {
        base.DoEffect(usedBy);
        Verse.Map map = usedBy.Map;
        IntVec3 Position = usedBy.Position;
        Position.z = map.info.Size.z - 1;

        PawnFlyer newThing = PawnFlyer.MakeFlyer(MSSFPDefOf.MSS_PawnFlyer_Balloon, usedBy, Position, null, null);
        if (newThing == null)
            return;
        GenSpawn.Spawn(newThing, Position, map);

        // TaggedString text = HealthUtility.FixWorstHealthCondition(usedBy);
        // if (!PawnUtility.ShouldSendNotificationAbout(usedBy))
        //     return;
        // Messages.Message((string)text, (LookTargets)(Thing)usedBy, MessageTypeDefOf.PositiveEvent);
    }
}

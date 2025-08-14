using RimWorld;
using Verse;

namespace MSSFP.Hediffs;

public class HediffComp_BuildBase : HediffComp
{
    public bool hasBuildBase = false;

    public HediffCompProperties_BuildBase Props => (HediffCompProperties_BuildBase)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (Props.ConstructJobs.Contains(Pawn.CurJob.def))
        {
            hasBuildBase = true;
        }

        if (parent.pawn.IsHashIntervalTick(2500))
        {
            this.Pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(Props.GoodThought);
            this.Pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(Props.BadThought);

            if (hasBuildBase)
            {
                //positive buff
                Thought_Memory newThought = (Thought_Memory)
                    ThoughtMaker.MakeThought(Props.GoodThought);
                this.Pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(newThought);

                hasBuildBase = false;
            }
            else
            {
                //negative buff
                Thought_Memory newThought = (Thought_Memory)
                    ThoughtMaker.MakeThought(Props.BadThought);
                this.Pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(newThought);

                hasBuildBase = false;
            }
        }
    }
}

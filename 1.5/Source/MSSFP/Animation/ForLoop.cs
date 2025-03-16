using System.Collections.Generic;
using Verse;

namespace MSSFP.Animation;

public class ForLoop : AnimationNodeBase
{
    public AnimationNodeBase child;

    public string indexName;
    public string start;
    public string end;

    public IntRange randomStart;
    public IntRange randomEnd;

    protected int startVal;
    protected int endVal;
    protected int currentVal;

    protected bool done = false;

    public override void ExposeData()
    {
        Scribe_References.Look(ref child, "child");
        Scribe_Values.Look(ref indexName, "indexName");
        Scribe_Values.Look(ref start, "start");
        Scribe_Values.Look(ref end, "end");
        Scribe_Values.Look(ref randomStart, "randomStart");
        Scribe_Values.Look(ref randomEnd, "randomEnd");

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (randomStart != null && start != null)
            {
                ModLog.Warn("[ForLoop] - Both randomStart and start are set. Ignoring randomStart.");
                ;
            }
            if (randomEnd != null && end != null)
            {
                ModLog.Warn("[ForLoop] - Both randomEnd and end are set. Ignoring randomEnd.");
                ;
            }
            if (randomStart == null && start == null)
                start = "0";

            if (randomEnd == null && end == null)
            {
                ModLog.Warn("[ForLoop] - Neither randomEnd nor end are set. Defaulting to 0.");
                end = "0";
            }

            indexName ??= "i";
        }
    }

    public override void Prepare(AnimationContext context)
    {
        startVal = start == null ? randomStart.RandomInRange : context.GetOrParseInt(start);
        endVal = end == null ? randomEnd.RandomInRange : context.GetOrParseInt(end);
        currentVal = startVal;
        done = false;
        ModLog.Debug($"[ForLoop] - Starting at {startVal}, ending at {endVal}");
    }

    public override void DoFrame(AnimationContext context)
    {
        if (done)
            return;

        if (child.Done)
        {
            currentVal++;
            if (currentVal > endVal)
            {
                done = true;
                return;
            }
            context.Integers[indexName] = currentVal;
            child.Prepare(context);
        }

        child.DoFrame(context);
    }
}

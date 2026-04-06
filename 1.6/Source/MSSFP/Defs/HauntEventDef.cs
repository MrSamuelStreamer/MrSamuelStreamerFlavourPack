using System;
using MSSFP.Events;
using Verse;

namespace MSSFP.Defs;

public class HauntEventDef : Def
{
    /// <summary>Relative weight in the weighted random pick.</summary>
    public float weight = 1f;

    /// <summary>Minimum haunt severity that must exist somewhere on the map for this event to be eligible.</summary>
    public float minSeverity = 0f;

    /// <summary>Concrete worker class that executes the event.</summary>
    public Type workerClass = typeof(HauntEventWorker_GhostlyWail);

    [Unsaved(false)]
    private HauntEventWorker workerInt;

    public HauntEventWorker Worker
    {
        get
        {
            if (workerInt != null)
                return workerInt;
            workerInt = (HauntEventWorker)Activator.CreateInstance(workerClass);
            workerInt.def = this;
            return workerInt;
        }
    }
}

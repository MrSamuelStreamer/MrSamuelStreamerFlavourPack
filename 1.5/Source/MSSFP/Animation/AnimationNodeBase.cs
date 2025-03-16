using System;
using Verse;

namespace MSSFP.Animation;

public abstract class AnimationNodeBase : IExposable, ILoadReferenceable
{
    public int Id;

    public virtual bool Done => true;

    public virtual void Initialize() { }

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref Id, "Id", -1);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (Id == -1)
                Id = Find.UniqueIDsManager.GetNextAbilityID();
            Initialize();
        }
    }

    public virtual void Prepare(AnimationContext context) { }

    public virtual void DoFrame(AnimationContext context) { }

    public virtual string GetUniqueLoadID() => "MSSFP_AnimationNode_" + Id;
}

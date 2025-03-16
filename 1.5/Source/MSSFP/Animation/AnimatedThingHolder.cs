using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MSSFP.Animation;

public class AnimatedThingHolder : Thing, IThingHolder
{
    private ThingOwner<Thing> innerContainer;

    public AnimationContext AnimationContext;
    public AnimationNodeBase Animation;

    public override Graphic Graphic
    {
        get { return innerContainer.Count > 0 ? innerContainer.InnerListForReading[0].Graphic : base.Graphic; }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        innerContainer ??= new ThingOwner<Thing>(this);
    }

    public virtual bool TryAnimateThing(ThingAnimationDef animDef, Thing thing)
    {
        if (innerContainer.Count > 0)
            return false;

        thing.DeSpawn();
        if (!innerContainer.TryAdd(thing))
        {
            GenSpawn.Spawn(thing, Position, Map);
            return false;
        }
        AnimationContext = new AnimationContext(this);

        SetAnimation(animDef.root);

        return true;
    }

    public virtual void SetAnimation(AnimationNodeBase animation)
    {
        Animation = animation;
        Animation.Prepare(AnimationContext);
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
    }

    public Thing InnerThing => innerContainer.InnerListForReading[0];

    public override void Tick()
    {
        innerContainer.ThingOwnerTick();
        if (Animation.Done)
        {
            innerContainer.TryDrop(innerContainer.InnerListForReading[0], Position, Map, ThingPlaceMode.Direct, out Thing lastResultingThing, playDropSound: false);
            Destroy(DestroyMode.Vanish);
            return;
        }
        Animation.DoFrame(AnimationContext);
    }

    public override Vector3 DrawPos => AnimationContext.Position;
}

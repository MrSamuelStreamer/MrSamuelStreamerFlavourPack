using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public class HauntedThingFlyer : ThingWithComps, IThingHolder
{
    private ThingOwner<Thing> innerContainer;
    private Vector3 effectivePos;
    private Vector3 startPos = Vector3.zero;

    public Vector3 StartPosition => startPos;
    public IntVec3 IntStartPosition;

    private string shadowTexPath = "Things/Skyfaller/SkyfallerShadowCircle";

    private Material cachedShadowMaterial;
    public Material ShadowMaterial
    {
        get
        {
            if (cachedShadowMaterial == null && !shadowTexPath.NullOrEmpty())
                cachedShadowMaterial = MaterialPool.MatFrom(shadowTexPath, ShaderDatabase.Transparent);
            return cachedShadowMaterial;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref IntStartPosition, "IntStartPosition");
        Scribe_Values.Look(ref effectivePos, "effectivePos");
        Scribe_Values.Look(ref startPos, "startPos");
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        foreach (Thing thing in innerContainer.InnerListForReading.ToList())
        {
            innerContainer.TryDrop(thing, IntStartPosition, MapHeld, ThingPlaceMode.Direct, out Thing _, playDropSound: false);
        }
        base.Destroy(mode);
    }

    public void AddThing(Thing thing)
    {
        if (CarriedThing != null)
            return;
        IntStartPosition = thing.Position;
        thing.DeSpawn();
        if (!innerContainer.TryAdd(thing, true))
        {
            GenSpawn.Spawn(thing, IntStartPosition, Map);
            return;
        }

        effectivePos = thing.DrawPos;
        startPos = thing.DrawPos;
    }

    public void SetPositionDirectly(Vector3 pos)
    {
        effectivePos = pos;
    }

    protected Thing CarriedThing
    {
        get { return innerContainer.InnerListForReading.Count <= 0 ? null : innerContainer.InnerListForReading[0]; }
    }

    public override Vector3 DrawPos
    {
        get { return effectivePos; }
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public HauntedThingFlyer() => innerContainer = new ThingOwner<Thing>(this);

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    protected virtual void RespawnPawn()
    {
        innerContainer.TryDrop(CarriedThing, Position, CarriedThing.MapHeld, ThingPlaceMode.Direct, out Thing _, playDropSound: false);
    }

    public override void Tick()
    {
        innerContainer.ThingOwnerTick();
    }

    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        if (CarriedThing != null)
            CarriedThing.DynamicDrawPhaseAt(phase, effectivePos, false);
        else
            CarriedThing?.DynamicDrawPhaseAt(phase, effectivePos);
        base.DynamicDrawPhaseAt(phase, drawLoc, flip);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        DrawShadow(effectivePos, 1);
        CarriedThing?.DrawNowAt(effectivePos, false);
    }

    private void DrawShadow(Vector3 drawLoc, float height)
    {
        Material shadowMaterial = CarriedThing.def.pawnFlyer?.ShadowMaterial ?? ShadowMaterial;
        if (ShadowMaterial == null)
            return;
        float num = Mathf.Lerp(1f, 0.6f, height);
        Vector3 s = new(num, 1f, num);
        Matrix4x4 matrix = new();
        matrix.SetTRS(drawLoc, Quaternion.identity, s);
        Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
    }
}

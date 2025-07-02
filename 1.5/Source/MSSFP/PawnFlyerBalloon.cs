using System;
using System.Reflection;
using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class PawnFlyerBalloon : PawnFlyer
{
    private static Lazy<FieldInfo> _effectivePos = new(() => AccessTools.Field(typeof(PawnFlyer), "effectivePos"));
    private Vector3 effectivePos
    {
        get => (Vector3)_effectivePos.Value.GetValue(this);
        set => _effectivePos.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _groundPos = new(() => AccessTools.Field(typeof(PawnFlyer), "groundPos"));
    private Vector3 groundPos
    {
        get => (Vector3)_groundPos.Value.GetValue(this);
        set => _groundPos.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _positionLastComputedTick = new(() => AccessTools.Field(typeof(PawnFlyer), "positionLastComputedTick"));
    private int positionLastComputedTick
    {
        get => (int)_positionLastComputedTick.Value.GetValue(this);
        set => _positionLastComputedTick.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _effectiveHeight = new(() => AccessTools.Field(typeof(PawnFlyer), "effectiveHeight"));
    private float effectiveHeight
    {
        get => (float)_effectiveHeight.Value.GetValue(this);
        set => _effectiveHeight.Value.SetValue(this, value);
    }

    public override Vector3 DrawPos
    {
        get
        {
            RecomputePosition();
            return effectivePos;
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad)
            return;
        var dist = DestinationPos.z - startVec.z;
        var time = dist / 20;
        ticksFlightTime = time.SecondsToTicks();
        ticksFlying = 0;
    }

    private void RecomputePosition()
    {
        if (positionLastComputedTick == ticksFlying)
            return;
        positionLastComputedTick = ticksFlying;
        float t = def.pawnFlyer.Worker.AdjustedProgress(ticksFlying / (float)ticksFlightTime);
        effectiveHeight = def.pawnFlyer.Worker.GetHeight(t);
        groundPos = Vector3.Lerp(startVec, DestinationPos, t);
        effectivePos = groundPos + Altitudes.AltIncVect * effectiveHeight + Vector3.up * (def.pawnFlyer.heightFactor * effectiveHeight);
        Position = groundPos.ToIntVec3();
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        drawLoc.y = AltitudeLayer.Skyfaller.AltitudeFor();
        Graphic.Draw(drawLoc, Rotation, this);
        // if (def.drawerType == DrawerType.RealtimeOnly || !Spawned)
        // Graphic.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this);
        //
        // PawnRenderUtility.CalculateCarriedDrawPos(FlyingPawn, this, ref drawLoc, out flip);
        //
        // ext?.graphicData?.Graphic.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this);
    }
}

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

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad)
            return;
        float dist = DestinationPos.z - startVec.z;
        float time = dist / 20;
        ticksFlightTime = Math.Max(10, time.SecondsToTicks());
        ticksFlying = 0;
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        drawLoc.y = AltitudeLayer.Skyfaller.AltitudeFor();
        Graphic.Draw(drawLoc, Rotation, this);
    }

    protected override void RespawnPawn()
    {
        base.RespawnPawn();
        //TODO: move pawn to home map and drop onto a prisoner bed.
    }
}

using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Mabel;

public class Command_TargetRadius : Command
{
    public Action<LocalTargetInfo> action;
    public TargetingParameters targetingParams;
    public int radius;

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        Find.DesignatorManager.Deselect();
        Find.Targeter.BeginTargeting(targetingParams, target => action(target), highlightAction: info => GenDraw.DrawRadiusRing(info.Cell, radius), targetValidator: _ => true);
    }

    public override bool InheritInteractionsFrom(Gizmo other) => false;
}

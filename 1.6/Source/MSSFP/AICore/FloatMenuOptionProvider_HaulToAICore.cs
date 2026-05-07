using System;
using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.AICore;

/// <summary>
/// Right-click float-menu provider for buildings carrying <see cref="CompTrueAICore"/>.
/// Surfaces two options:
///   1. "Create AI art" / "Stop creating AI art" — toggles <c>comp.artRequested</c> (mirrors the
///      gizmo Command_Toggle in <see cref="CompTrueAICore.CompGetGizmosExtra"/>).
///   2. "Haul {input} to {core}" — issues a one-shot <c>MSSFP_HaulToAICore</c> job, same JobDef
///      as the WorkGiver. Only yielded when <c>comp.WantsHaul</c>.
///
/// Auto-discovered by <see cref="FloatMenuMakerMap.Init"/> via <c>AllSubclassesNonAbstract</c> —
/// no Def registration required.
///
/// Behavior parity with <see cref="WorkGiver_HaulToAICore"/> for the haul branch: same input filter,
/// same reservation semantics, same job. Float-menu = manual override; WorkGiver = background haul.
/// We do NOT implement <see cref="IHaulEnroute"/> — concurrent over-delivery is acceptable; the
/// comp self-trims on art completion.
///
/// Applicability:
/// - Undrafted only.
/// - Single pawn (Multiselect=false).
/// - RequiresManipulation — toggle is cheap but haul needs hands; gate at the provider level.
/// - Player-faction only — toggling <c>artRequested</c> on a non-player core is meaningless.
/// </summary>
public class FloatMenuOptionProvider_HaulToAICore : FloatMenuOptionProvider
{
    protected override bool Drafted => false;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;
    protected override bool MechanoidCanDo => false;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        if (!base.TargetThingValid(thing, context)) return false;
        if (!(thing is Building b)) return false;
        if (b.Faction != Faction.OfPlayer) return false;
        CompTrueAICore comp = thing.TryGetComp<CompTrueAICore>();
        if (comp == null) return false;
        // Provider applies if the building can either accept hauls right now OR be toggled into doing so.
        bool artConfigured = comp.Props.artOutputDef != null
            && comp.Props.artInputs != null
            && comp.Props.artInputs.Count > 0;
        return artConfigured || comp.WantsHaul;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        if (pawn == null) yield break;

        Building building = clickedThing as Building;
        if (building == null) yield break;
        CompTrueAICore comp = building.TryGetComp<CompTrueAICore>();
        if (comp == null) yield break;

        // ------------------------------------------------------------------
        // Option 1: Create AI art / Stop creating AI art — toggles artRequested.
        // Mirrors the toggleAction in CompTrueAICore.CompGetGizmosExtra.
        // ------------------------------------------------------------------
        bool artConfigured = comp.Props.artOutputDef != null
            && comp.Props.artInputs != null
            && comp.Props.artInputs.Count > 0;

        if (artConfigured)
        {
            FloatMenuOption toggleOpt = BuildToggleOption(pawn, building, comp);
            if (toggleOpt != null) yield return toggleOpt;
        }

        // ------------------------------------------------------------------
        // Option 2: Haul {input} to {core} — only when comp wants more inputs.
        // ------------------------------------------------------------------
        if (comp.WantsHaul)
        {
            FloatMenuOption haulOpt = BuildHaulOption(pawn, building, comp);
            if (haulOpt != null) yield return haulOpt;
        }
    }

    private static FloatMenuOption BuildToggleOption(Pawn pawn, Building building, CompTrueAICore comp)
    {
        string label = comp.artRequested
            ? "MSSFP_AICore_StopCreatingArt".Translate(building.LabelShort)
            : "MSSFP_AICore_StartCreatingArt".Translate(building.LabelShort);

        Action takeAction = () =>
        {
            comp.artRequested = !comp.artRequested;
            if (!comp.artRequested)
            {
                // Drop active reservations so colonists abandon enroute hauls cleanly — same as gizmo path.
                if (comp.parent.Spawned && comp.parent.Map != null)
                    comp.parent.Map.reservationManager.ReleaseAllForTarget(comp.parent);
            }
        };

        // No prioritised-task decoration: this is a UI toggle, not a job.
        return new FloatMenuOption(label, takeAction);
    }

    private static FloatMenuOption BuildHaulOption(Pawn pawn, Building building, CompTrueAICore comp)
    {
        string coreLabel = building.LabelShort;

        // Disabled-reason early-outs — keep visible but greyed so player knows why.
        if (building.IsBurning())
            return new FloatMenuOption("CannotHaulIsBurning".Translate(coreLabel), null);

        if (building.IsForbidden(pawn))
            return new FloatMenuOption("CannotPrioritizeForbidden".Translate(), null);

        if (!pawn.CanReserve(building, 1, 1))
        {
            Pawn reserver = building.Map.reservationManager.FirstRespectedReserver(building, pawn);
            string reason = reserver != null
                ? "ReservedBy".Translate(reserver.LabelShort, reserver)
                : "Reserved".Translate();
            return new FloatMenuOption("CannotGenericWork".Translate("HaulToContainer".Translate(coreLabel), reason), null);
        }

        Thing input = FindInput(pawn, comp, forced: true);
        if (input == null)
            return new FloatMenuOption("MSSFP_AICore_NoInputAvailable".Translate(coreLabel), null);

        string label = "MSSFP_AICore_HaulInputToCore".Translate(input.LabelShort, coreLabel);
        Action takeAction = () =>
        {
            // Re-resolve input at click time — world may have shifted since menu open.
            Thing live = FindInput(pawn, comp, forced: true);
            if (live == null) return;
            Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_HaulToAICore, live, building);
            job.count = Mathf.Clamp(comp.RemainingNeed, 1, live.stackCount);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        };

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(label, takeAction),
            pawn,
            building
        );
    }

    private static Thing FindInput(Pawn pawn, CompTrueAICore comp, bool forced)
    {
        List<ThingDef> defs = comp.Props.artInputs;
        if (defs == null || defs.Count == 0) return null;

        bool Validator(Thing th)
        {
            if (th == null || th.Destroyed) return false;
            if (!defs.Contains(th.def)) return false;
            if (th.IsForbidden(pawn)) return false;
            if (!pawn.CanReserve(th, 1, -1, null, forced)) return false;
            return true;
        }

        return GenClosest.ClosestThingReachable(
            comp.parent.Position,
            comp.parent.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            9999f,
            Validator
        );
    }
}

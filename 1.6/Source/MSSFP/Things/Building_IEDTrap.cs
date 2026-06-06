using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.Things;

/// <summary>
///     Hostile-spawned IED that can't be claim+deconstructed for free. Player must
///     designate the trap and a colonist with Intellectual ≥ <see cref="MinIntellectual"/>
///     walks adjacent and performs a timed disarm. Failure either springs the trap
///     instantly or starts a delayed wick whose length scales with blast radius —
///     player gets a paused letter and must manually flee.
///
///     UX SURFACES (all gated by <see cref="CanDisarm"/>):
///     - Designate-disarm gizmo on the selected trap → adds <c>MSSFP_DisarmIED</c>
///       designation; <c>WorkGiver_DisarmIED</c> picks it up by work priority.
///     - Float-menu entry on right-click with a colonist selected → direct order,
///       bypasses work-tab priority. Disabled with a red reason below skill 4 or
///       for non-humanlikes.
///
///     OUTCOME MATH:
///     - Success chance: <c>0.20 + 0.04 * Intellectual</c> (skill 4 = 36%, skill 20 = 100%).
///     - Success: 15% spawn a minified IED on the trap cell (player-faction after
///       placement → triggers on hostiles), else despawn no return. Awards XP.
///     - Failure: 50/50 instant vs delayed-fuse. Delayed wick ticks = max(60, radius × 23).
/// </summary>
public class Building_IEDTrap : Building_TrapExplosive
{
    public const int MinIntellectual = 4;

    // Success chance ramp — 36% at skill 4, 100% at skill 20.
    private const float SuccessChanceBase = 0.20f;
    private const float SuccessChancePerLevel = 0.04f;

    // Work duration ramp — 6000 ticks at skill 4 (100s), 1200 ticks at skill 20 (20s).
    private const int WorkTicksAtMinSkill = 6000;
    private const int WorkTicksAtMaxSkill = 1200;
    private const int SkillSpan = 20 - MinIntellectual;

    private const float MinifyDropChance = 0.15f;
    private const float SuccessXp = 125f;
    private const float FailureXp = 35f;

    // Doubled from initial 23/60 after playtest — 1.5s window at radius 3.9 was
    // too tight to draft, queue Goto, and unpause before the wick fired. 3s gives
    // realistic player reaction headroom while keeping skill-4 failure painful.
    private const float DelayedFuseTicksPerRadius = 46f;
    private const int DelayedFuseTicksFloor = 120;

    /// <summary>
    ///     Block the vanilla Claim designator on player-faction so the only way to
    ///     remove a hostile MSSFP IED is via the disarm job. Player-owned MSSFP IEDs
    ///     (re-deployed from minified-drop) still deconstruct normally because
    ///     <c>DeconstructibleBy</c> short-circuits on <c>Faction == player</c> before
    ///     reaching <c>ClaimableBy</c>.
    /// </summary>
    public override AcceptanceReport ClaimableBy(Faction by)
    {
        if (by == Faction.OfPlayer && Faction != Faction.OfPlayer)
        {
            return "MSSFP_IEDClaimBlocked".Translate();
        }
        return base.ClaimableBy(by);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo g in base.GetGizmos())
        {
            yield return g;
        }

        // Only show disarm-designate gizmo while the trap is still hostile. Player-
        // faction (re-deployed) traps fall back to vanilla deconstruct gizmos.
        if (Faction == Faction.OfPlayer || !Spawned)
        {
            yield break;
        }

        bool designated = Map.designationManager.DesignationOn(this, MSSFPDefOf.MSSFP_DisarmIEDDesignation) != null;
        Command_Action cmd = new Command_Action
        {
            defaultLabel = designated ? "MSSFP_CancelDisarmIEDGizmoLabel".Translate() : "MSSFP_DisarmIEDGizmoLabel".Translate(),
            defaultDesc = "MSSFP_DisarmIEDGizmoDesc".Translate(MinIntellectual),
            icon = TexCommand.Attack,
            action = () =>
            {
                if (designated)
                {
                    Map.designationManager.RemoveAllDesignationsOn(this, false);
                }
                else
                {
                    Map.designationManager.AddDesignation(new Designation(this, MSSFPDefOf.MSSFP_DisarmIEDDesignation));
                }
            },
        };
        yield return cmd;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption opt in base.GetFloatMenuOptions(selPawn))
        {
            yield return opt;
        }

        if (Faction == Faction.OfPlayer || !Spawned) yield break;

        // Non-humanlikes (animals/mechs) get no entry at all — there is no skill tracker
        // to roll against. Keeps the menu clean for mechanitor right-clicks.
        if (selPawn?.RaceProps == null || !selPawn.RaceProps.Humanlike || selPawn.skills == null)
        {
            yield break;
        }

        int skill = selPawn.skills.GetSkill(SkillDefOf.Intellectual).Level;
        float successPct = Mathf.Clamp01(SuccessChanceBase + SuccessChancePerLevel * skill) * 100f;
        int workTicks = WorkTicksFor(selPawn);
        string label = "MSSFP_DisarmIEDFloatMenuLabel".Translate(successPct.ToString("F0"), workTicks.ToStringTicksToPeriod());

        if (skill < MinIntellectual)
        {
            yield return new FloatMenuOption(
                "MSSFP_DisarmIEDFloatMenuLabelLowSkill".Translate(MinIntellectual),
                null);
            yield break;
        }

        if (selPawn.WorkTagIsDisabled(WorkTags.Intellectual))
        {
            yield return new FloatMenuOption(
                "MSSFP_DisarmIEDFloatMenuLabelIncapable".Translate(),
                null);
            yield break;
        }

        if (!selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
        {
            yield return new FloatMenuOption("MSSFP_DisarmIEDFloatMenuLabelUnreachable".Translate(), null);
            yield break;
        }

        if (!selPawn.CanReserve(this))
        {
            yield return new FloatMenuOption("MSSFP_DisarmIEDFloatMenuLabelReserved".Translate(), null);
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(label, () =>
            {
                Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_DisarmIED, this);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }),
            selPawn,
            this);
    }

    /// <summary>
    ///     Single gate used by every UX surface (gizmo, float menu, WorkGiver, JobDriver)
    ///     so the rule "humanlike + skills tracker + Intellectual ≥ 4 + work tag enabled"
    ///     lives in one place. <see cref="feedback_rimworld_humanlike_xml_comp_injection_nre"/>
    ///     for why the explicit null + Humanlike guard is required.
    /// </summary>
    public static bool CanDisarm(Pawn p)
    {
        if (p == null) return false;
        if (p.RaceProps == null || !p.RaceProps.Humanlike) return false;
        if (p.skills == null) return false;
        if (p.WorkTagIsDisabled(WorkTags.Intellectual)) return false;
        SkillRecord skill = p.skills.GetSkill(SkillDefOf.Intellectual);
        if (skill == null || skill.TotallyDisabled) return false;
        if (skill.Level < MinIntellectual) return false;
        return true;
    }

    public static int WorkTicksFor(Pawn p)
    {
        int level = p.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? MinIntellectual;
        float t = Mathf.Clamp01((level - MinIntellectual) / (float)SkillSpan);
        return Mathf.RoundToInt(Mathf.Lerp(WorkTicksAtMinSkill, WorkTicksAtMaxSkill, t));
    }

    public static float SuccessChanceFor(Pawn p)
    {
        int level = p.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
        return Mathf.Clamp01(SuccessChanceBase + SuccessChancePerLevel * level);
    }

    /// <summary>
    ///     Resolves a completed disarm attempt. Called by <c>JobDriver_DisarmIED</c>
    ///     when the wait toil expires. Pawn is assumed adjacent (not on trap cell)
    ///     so an instant spring catches them via blast radius, not via touch-trigger.
    /// </summary>
    public static void ResolveDisarm(Pawn pawn, Building_IEDTrap trap)
    {
        if (pawn == null || trap == null || trap.Destroyed || !trap.Spawned) return;

        pawn.skills?.Learn(SkillDefOf.Intellectual, FailureXp, false);

        bool success = Rand.Value < SuccessChanceFor(pawn);
        if (success)
        {
            pawn.skills?.Learn(SkillDefOf.Intellectual, SuccessXp - FailureXp, false);
            OnDisarmSuccess(pawn, trap);
            return;
        }

        // Failure path — 50/50 instant vs delayed fuse.
        bool delayed = Rand.Value < 0.5f;
        if (delayed)
        {
            TriggerDelayedFuse(pawn, trap);
        }
        else
        {
            TriggerInstantSpring(pawn, trap);
        }
    }

    private static void OnDisarmSuccess(Pawn pawn, Building_IEDTrap trap)
    {
        Map map = trap.Map;
        IntVec3 cell = trap.Position;

        // Stop any latent wick so a half-armed trap doesn't pop after despawn.
        trap.GetComp<CompExplosive>()?.StopWick();

        if (Rand.Value < MinifyDropChance)
        {
            // Clear faction BEFORE MakeMinified so the inner thing scribes as
            // unclaimed; when the player installs the minified item, vanilla
            // construction re-assigns player faction. Leaving the hostile faction
            // intact would land the re-deployed trap as still-hostile and unusable.
            trap.SetFactionDirect(null);

            // MakeMinified() despawns the trap and wraps it in a MinifiedThing.
            // The inner reference persists, so the faction-null state is what gets
            // scribed when the container saves.
            MinifiedThing minified = trap.MakeMinified();
            GenPlace.TryPlaceThing(minified, cell, map, ThingPlaceMode.Near);
            Messages.Message(
                "MSSFP_DisarmIEDSuccessMinified".Translate(pawn.LabelShort, trap.LabelShort),
                new TargetInfo(cell, map),
                MessageTypeDefOf.PositiveEvent,
                false);
        }
        else
        {
            if (!trap.Destroyed) trap.Destroy(DestroyMode.Vanish);
            Messages.Message(
                "MSSFP_DisarmIEDSuccess".Translate(pawn.LabelShort),
                new TargetInfo(cell, map),
                MessageTypeDefOf.PositiveEvent,
                false);
        }
    }

    private static void TriggerInstantSpring(Pawn pawn, Building_IEDTrap trap)
    {
        // Use the comp's own wick path so explosion type, sound, and overlays match
        // a normal pawn-touch trigger. wickTicksLeft is left at vanilla default
        // (15 ticks ≈ 0.25s) — fast enough to read as "instant" to the player.
        CompExplosive comp = trap.GetComp<CompExplosive>();
        if (comp == null)
        {
            // Defensive — every TrapIEDBase-derived def carries CompExplosive. If a
            // modded variant omits it, fall back to a manual blast so the failure
            // path still has consequences.
            FallbackExplosion(trap, pawn);
            return;
        }
        comp.StartWick(pawn);
    }

    private static void TriggerDelayedFuse(Pawn pawn, Building_IEDTrap trap)
    {
        CompExplosive comp = trap.GetComp<CompExplosive>();
        if (comp == null)
        {
            FallbackExplosion(trap, pawn);
            return;
        }

        comp.StartWick(pawn);

        // Overwrite the rolled wick length with our radius-scaled fuse so larger
        // blasts give proportionally more flee time. StartWick already set wickTicksLeft
        // via Props.wickTicks.RandomInRange — clobber it now.
        float radius = comp.ExplosiveRadius();
        int fuse = Mathf.Max(DelayedFuseTicksFloor, Mathf.RoundToInt(radius * DelayedFuseTicksPerRadius));
        comp.wickTicksLeft = fuse;

        Find.LetterStack.ReceiveLetter(
            "MSSFP_DisarmIEDDelayedFuseLetterLabel".Translate(),
            "MSSFP_DisarmIEDDelayedFuseLetterText".Translate(
                pawn.LabelShort,
                fuse.ToStringTicksToPeriod(),
                radius.ToString("F1")),
            LetterDefOf.ThreatBig,
            new TargetInfo(trap.Position, trap.Map));
        Find.TickManager.Pause();
    }

    private static void FallbackExplosion(Building_IEDTrap trap, Pawn pawn)
    {
        GenExplosion.DoExplosion(
            trap.Position,
            trap.Map,
            3.9f,
            DamageDefOf.Bomb,
            pawn,
            -1,
            -1f,
            null,
            null,
            null,
            null);
        if (!trap.Destroyed) trap.Destroy(DestroyMode.Vanish);
    }
}

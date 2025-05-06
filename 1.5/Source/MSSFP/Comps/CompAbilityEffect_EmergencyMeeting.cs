using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MSSFP.Comps;

public class CompAbilityEffect_EmergencyMeeting : CompAbilityEffect
{
    public static int lastCalledTick = -1;

    public override bool CanApplyOn(GlobalTargetInfo target) => !MSSFPMod.settings.disableMogus && Find.TickManager.TicksGame - lastCalledTick > (GenDate.TicksPerDay * 30);

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => CanApplyOn(target.ToGlobalTargetInfo(parent.pawn.Map));

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        Verse.Map map = parent.pawn?.Map;
        if (map == null)
            return;

        // Create a gathering point
        IntVec3 gatheringSpot = parent.pawn.Position;

        // Order all pawns to come to the meeting
        foreach (Pawn attendee in map.mapPawns.AllPawnsSpawned.Where(p => !p.Downed && !p.Dead))
        {
            Job job = JobMaker.MakeJob(JobDefOf.Goto, gatheringSpot);
            job.expiryInterval = 2000;
            attendee.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        // Visual and sound effects
        FleckMaker.ThrowLightningGlow(parent.pawn.Position.ToVector3(), map, 2f);
        MSSFPDefOf.MSSFP_EmergencyMeetingKlaxon.PlayOneShot(new TargetInfo(parent.pawn.Position, map));

        Dialog_MessageBox confirmation = Dialog_MessageBox.CreateConfirmation(
            "MSSFP_EmergencyMeeting".Translate(),
            (
                () =>
                {
                    if (CameraJumper.CanJump(parent.pawn))
                    {
                        CameraJumper.TryJumpAndSelect(parent.pawn);
                    }
                }
            )
        );
        Find.WindowStack.Add(confirmation);
    }
}

using RimWorld;
using Verse;
using Verse.Sound;
using VFETribals;

namespace MSSFP.VET;

public class RitualBehaviourWorker_ArchoGathering : RitualBehaviorWorker
{
    private Sustainer soundPlaying;

    public RitualBehaviourWorker_ArchoGathering() { }

    public RitualBehaviourWorker_ArchoGathering(RitualBehaviorDef def)
        : base(def) { }

    public override void Tick(LordJob_Ritual ritual)
    {
        base.Tick(ritual);
        if (ritual.StageIndex == 1)
        {
            if (soundPlaying == null || soundPlaying.Ended)
            {
                TargetInfo selectedTarget = ritual.selectedTarget;
                soundPlaying = VFET_DefOf.VFET_RitualSustainer_UltraGathering.TrySpawnSustainer(
                    SoundInfo.InMap(
                        new TargetInfo(selectedTarget.Cell, selectedTarget.Map, false),
                        MaintenanceType.PerTick
                    )
                );
            }
            Sustainer sustainer = soundPlaying;
            if (sustainer == null)
            {
                return;
            }
            sustainer.Maintain();
        }
    }
}

using MSSFP.Comps.Game;
using Verse;

namespace MSSFP.Genes;

public class Gene_Persistent : Gene
{
    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        base.Notify_PawnDied(dinfo, culprit);
        Current.Game.GetComponent<PersistentGeneGameComponent>().Notify_PawnDied(pawn);
    }
}

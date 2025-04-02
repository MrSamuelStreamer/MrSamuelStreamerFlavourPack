using Verse;

namespace MSSFP.Genes;

public class Gene_DRM : Gene
{
    public override void PostAdd()
    {
        base.PostAdd();

        Hediff hediff = HediffMaker.MakeHediff(MSSFPDefOf.MSSFP_Hediff_DRM, pawn);
        pawn.health.AddHediff(hediff);
    }
}

using Verse;

namespace MSSFP.PawnPortability
{
    /// <summary>
    /// Hidden marker recording which PawnTemplateDef a pawn was created from.
    /// Lives on the pawn so it saves, travels and dies with it — no external
    /// pawn references to go stale when WorldPawns GC discards the pawn.
    /// </summary>
    public class Hediff_TemplateOrigin : HediffWithComps
    {
        public string templateDefName;

        public override bool Visible => false;

        public override bool ShouldRemove => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref templateDefName, "templateDefName");
        }

        public static Hediff_TemplateOrigin Of(Pawn pawn) =>
            pawn?.health?.hediffSet?.GetFirstHediffOfDef(MSSFPDefOf.MSSFP_TemplateOrigin)
                as Hediff_TemplateOrigin;

        /// <summary>Adds the marker unless the pawn already carries one.</summary>
        public static void Apply(Pawn pawn, string defName)
        {
            if (pawn?.health == null || Of(pawn) != null) return;

            Hediff_TemplateOrigin marker =
                (Hediff_TemplateOrigin)HediffMaker.MakeHediff(MSSFPDefOf.MSSFP_TemplateOrigin, pawn);
            marker.templateDefName = defName;
            pawn.health.AddHediff(marker);
        }
    }
}

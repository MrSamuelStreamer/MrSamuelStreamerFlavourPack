using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using VFETribals;

namespace MSSFP.VET;

public class RitualOutcomeEffectWorker_AdvanceToArcho : RitualOutcomeEffectWorker
{
    public static Lazy<FieldInfo> jobRitualInfo = new Lazy<FieldInfo>(
        () => AccessTools.Field(typeof(LordJob_Ritual), "ritual")
    );
    public override bool SupportsAttachableOutcomeEffect => false;

    public PreceptDef MSSFP_AdvanceToArcho =>
        DefDatabase<PreceptDef>.GetNamed("MSSFP_AdvanceToArcho");
    public EraAdvancementDef MSSFP_FormArchoMind =>
        DefDatabase<EraAdvancementDef>.GetNamed("MSSFP_FormArchoMind");

    public RitualOutcomeEffectWorker_AdvanceToArcho() { }

    public RitualOutcomeEffectWorker_AdvanceToArcho(RitualOutcomeEffectDef def)
        : base(def) { }

    public override void Apply(
        float progress,
        Dictionary<Pawn, int> totalPresence,
        LordJob_Ritual jobRitual
    )
    {
        GameComponent_Tribals comp = Current.Game.GetComponent<GameComponent_Tribals>();
        LookTargets lookTargets = jobRitual.selectedTarget;

        if (comp != null)
        {
            comp.AdvanceToEra(MSSFP_FormArchoMind);
            Find.LetterStack.ReceiveLetter(
                MSSFP_FormArchoMind.label,
                MSSFP_FormArchoMind.description,
                LetterDefOf.RitualOutcomePositive,
                lookTargets,
                null,
                null,
                null,
                null
            );
            Precept_Ritual ritual = (Precept_Ritual)jobRitualInfo.Value.GetValue(jobRitual);
            ritual.completedObligations ??= new List<RitualObligation>();
            ritual.RemoveObligation(jobRitual.obligation, completed: true);
            ritual.activeObligations.Clear();

            try
            {
                ModLog.Log($"Starting on map {Find.AnyPlayerHomeMap}");
                IncidentParms parms = new() { forced = true, target = Find.AnyPlayerHomeMap };
            }
            catch (Exception ex)
            {
                ModLog.Error("Error firing LA", ex);
            }
        }
    }
}

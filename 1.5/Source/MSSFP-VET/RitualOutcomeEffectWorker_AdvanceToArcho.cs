using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MSSFP.VFE;
using RimWorld;
using Verse;
using VFETribals;

namespace MSSFP.VET;

public class RitualOutcomeEffectWorker_AdvanceToArcho: RitualOutcomeEffectWorker
{
    public static Lazy<FieldInfo> jobRitualInfo = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(LordJob_Ritual), "ritual"));
    public override bool SupportsAttachableOutcomeEffect => false;

    public RitualOutcomeEffectWorker_AdvanceToArcho()
    {
    }

    public RitualOutcomeEffectWorker_AdvanceToArcho(RitualOutcomeEffectDef def)
        : base(def)
    {
    }

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        GameComponent_Tribals comp = Current.Game.GetComponent<GameComponent_Tribals>();
        LookTargets lookTargets = jobRitual.selectedTarget;

        if (comp != null)
        {
            comp.AdvanceToEra(MSSFPVETDefOf.MSSFP_FormArchoMind);
            Find.LetterStack.ReceiveLetter(MSSFPVETDefOf.MSSFP_FormArchoMind.label, MSSFPVETDefOf.MSSFP_FormArchoMind.description, LetterDefOf.RitualOutcomePositive, lookTargets, null, null, null, null);
            Precept_Ritual ritual = (Precept_Ritual)jobRitualInfo.Value.GetValue(jobRitual);
            ritual.completedObligations ??= new List<RitualObligation>();
            ritual.RemoveObligation(jobRitual.obligation, completed: true);
            ritual.activeObligations.Clear();

            try
            {
                IncidentParms parms = new() { forced = true, target = Find.AnyPlayerHomeMap };

                if (MSSFPDefOf.MSS_LoversAdvance.Worker.CanFireNow(parms))
                {
                    MSSFPDefOf.MSS_LoversAdvance.Worker.TryExecute(parms);
                }
            }
            catch (Exception ex)
            {
                ModLog.Error("Error firing LA", ex);
            }
        }


    }




}

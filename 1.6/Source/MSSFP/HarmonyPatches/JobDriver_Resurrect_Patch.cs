using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver_Resurrect))]
public static class JobDriver_Resurrect_Patch
{
    public static SimpleCurve SeverityForDaysDead = new SimpleCurve()
    {
        new CurvePoint(0f, 0.01f),
        new CurvePoint(1f, 0.1f),
        new CurvePoint(15f, 0.5f),
        new CurvePoint(60f, 0.75f),
        new CurvePoint(120f, 1f)
    };

    public static FloatRange SeverityVariability = new(0.05f, 0.1f);

    public static float SeverityForTicks(int ticks)
    {
        float daysDead = (float)ticks / GenDate.TicksPerDay;
        float severity = SeverityForDaysDead.Evaluate(daysDead);
        float randomisedSeverity = severity + Rand.Range(-SeverityVariability.RandomInRange, SeverityVariability.RandomInRange);
        return Mathf.Clamp01(randomisedSeverity);
    }

    [HarmonyPatch("Resurrect")]
    [HarmonyPrefix]
    public static bool Prefix(JobDriver_Resurrect __instance)
    {
        Thing item = __instance.job.GetTarget(TargetIndex.B).Thing;
        ResurrectorModExtension ext = item?.def.GetModExtension<ResurrectorModExtension>();
        if (item == null || ext is not { EnableExtraSideEffects: true }) return true;

        LocalTargetInfo target = __instance.job.GetTarget(TargetIndex.A);
        if (target.Thing is not Corpse corpse) return true;
        float severity = SeverityForTicks(Find.TickManager.TicksAbs - corpse.timeOfDeath);
        Resurrect(corpse.InnerPawn, item, severity);
        return false;
    }

    public static bool TryResurrect(Pawn pawn, float severity)
    {
        if (!ResurrectionUtility.TryResurrect(pawn))
        {
            return false;
        }

        Hediff hediff = pawn.health.AddHediff(MSSFPDefOf.MSS_VoidInsanity);
        hediff.Severity = severity;
        return true;
    }

    public static void Resurrect(Pawn innerPawn, Thing item, float severity)
    {
        CompTargetEffect_Resurrect comp = item.TryGetComp<CompTargetEffect_Resurrect>();

        if (TryResurrect(innerPawn, severity))
        {
            SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap((TargetInfo) (Thing) innerPawn));
            Messages.Message("MessagePawnResurrected".Translate((NamedArgument) (Thing) innerPawn), (Thing) innerPawn, MessageTypeDefOf.PositiveEvent);
            if (comp.Props.moteDef != null)
                MoteMaker.MakeAttachedOverlay(innerPawn, comp.Props.moteDef, Vector3.zero);
            if (comp.Props.addsHediff != null)
                innerPawn.health.AddHediff(comp.Props.addsHediff);
        }
        item.SplitOff(1).Destroy();
    }
}

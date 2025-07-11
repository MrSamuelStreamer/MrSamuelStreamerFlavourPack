using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using VEE;
using Verse;

namespace MSSFP.VEE;

[HarmonyPatch(typeof(IncidentWorker_MakeGameConditionPurple))]
public static class IncidentWorker_MakeGameConditionPurple_Patch
{
    public static Lazy<MethodInfo> SendStandardLetter = new Lazy<MethodInfo>(
        () =>
            AccessTools.Method(
                typeof(IncidentWorker_MakeGameConditionPurple),
                "SendStandardLetter",
                [typeof(TaggedString), typeof(TaggedString), typeof(LetterDef), typeof(IncidentParms), typeof(LookTargets), typeof(NamedArgument[])]
            )
    );

    [HarmonyPatch("TryExecuteWorker")]
    [HarmonyPostfix]
    public static void TryExecuteWorker(IncidentWorker_MakeGameConditionPurple __instance, IncidentParms parms)
    {
        if (!__instance.def.HasModExtension<IncidentDefModExtension>())
            return;

        IncidentDefModExtension defModExtension = __instance.def.GetModExtension<IncidentDefModExtension>();

        if (defModExtension.extraConditions.NullOrEmpty())
            return;

        foreach (GameConditionDef extraGameCondition in defModExtension.extraConditions)
        {
            GameCondition cond = GameConditionMaker.MakeCondition(extraGameCondition, Mathf.RoundToInt(__instance.def.durationDays.RandomInRange * 60000f));
            parms.target.GameConditionManager.RegisterCondition(cond);
            parms.letterHyperlinkThingDefs = cond.def.letterHyperlinks;
            SendStandardLetter.Value.Invoke(
                __instance,
                [
                    (TaggedString)extraGameCondition.label,
                    (TaggedString)extraGameCondition.letterText,
                    extraGameCondition.letterDef,
                    parms,
                    LookTargets.Invalid,
                    Array.Empty<NamedArgument>(),
                ]
            );
        }

        return;
    }
}

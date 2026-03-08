using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Superpack_BrainwashPatch
{
    public static class Superpack_Brainwash
    {
        public static void ApplyRandomBrainwashToPawn(Pawn pawn)
        {
            if (pawn == null) return;
            if (!pawn.RaceProps.Humanlike) return;

            BrainwashLogic.RerollExactlyNTraits(pawn, 5);
            BrainwashLogic.RandomisePassions(pawn, 0.55f, 0.35f, 0.10f);
            BrainwashLogic.ApplyCatatonicBreakdown(pawn);
            BrainwashLogic.SendCatatonicLetter(pawn);
        }
    }

    public class Brainwash_Patch : Mod
    {
        public Brainwash_Patch(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("BrainwashPatch.RandomiseTraitsPassions");
            harmony.PatchAll();
        }
    }

    public static class BrainwashLogic
    {
        public static void RerollExactlyNTraits(Pawn pawn, int n)
        {
            if (pawn.story?.traits == null) return;
            for (int i = pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
            {
                Trait t = pawn.story.traits.allTraits[i];
                if (t.sourceGene is null)
                    pawn.story.traits.RemoveTrait(t);
            }

            List<TraitDef> pool = DefDatabase<TraitDef>.AllDefsListForReading
                .Where(td =>
                    td != null &&
                    td.degreeDatas != null && td.degreeDatas.Count > 0 &&
                    td.GetGenderSpecificCommonality(pawn.gender) > 0f)
                .ToList();

            for (int i = 0; i < n; i++)
            {
                TraitDef chosen = pool
                    .Where(td => !pawn.story.traits.HasTrait(td))
                    .InRandomOrder()
                    .FirstOrDefault();

                if (chosen == null)
                    break;

                int degree = chosen.degreeDatas.RandomElement().degree;
                pawn.story.traits.GainTrait(new Trait(chosen, degree, forced: true));
            }
        }

        public static void RandomisePassions(Pawn pawn, float noneChance, float minorChance, float majorChance)
        {
            if (pawn.skills?.skills == null) return;

            float total = Math.Max(0.0001f, noneChance + minorChance + majorChance);
            noneChance /= total;
            minorChance /= total;
            majorChance /= total;

            foreach (SkillRecord sr in pawn.skills.skills)
            {
                float r = Rand.Value;
                if (r < noneChance) sr.passion = Passion.None;
                else if (r < noneChance + minorChance) sr.passion = Passion.Minor;
                else sr.passion = Passion.Major;
            }
        }

        public static void ApplyCatatonicBreakdown(Pawn pawn)
        {
            if (pawn.health?.hediffSet == null) return;

            if (pawn.health.hediffSet.HasHediff(HediffDefOf.CatatonicBreakdown))
                return;

            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            if (brain != null)
            {
                pawn.health.AddHediff(HediffDefOf.CatatonicBreakdown, brain);
            }
            else
            {
                pawn.health.AddHediff(HediffDefOf.CatatonicBreakdown);
            }
        }

        public static void SendCatatonicLetter(Pawn pawn)
        {
            Find.LetterStack.ReceiveLetter(
                "Superpack_BrainwashPatch_CatatonicMessage_Label".Translate(pawn.Named("PAWN")),
                "Superpack_BrainwashPatch_CatatonicMessage_Text".Translate(pawn.Named("PAWN")),
                LetterDefOf.NegativeEvent,
                new LookTargets(pawn));
        }
    }

    [HarmonyPatch(typeof(Brainwash.CompChangePersonality))]
    [HarmonyPatch(nameof(Brainwash.CompChangePersonality.CompFloatMenuOptions))]
    public static class ChangePersonalityNoFloatMenu
    {
        public static bool Prefix(Brainwash.CompChangePersonality __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (__instance.parent is not Pawn targetPawn || selPawn == null)
            {
                __result = Enumerable.Empty<FloatMenuOption>();
                return false;
            }

            if (!targetPawn.RaceProps.Humanlike || targetPawn.Dead || targetPawn.Destroyed || selPawn.Dead)
            {
                __result = Enumerable.Empty<FloatMenuOption>();
                return false;
            }

            if (__instance.TryGetNearbyTelevisionAndChair(selPawn, out var televisionAndChair))
            {
                Action action = delegate
                {
                    Job job = JobMaker.MakeJob(
                        Brainwash.BrainwashDefOf.RedHorse_LeadToBrainwashChairForPersonalityChange,
                        targetPawn,
                        televisionAndChair.chair,
                        televisionAndChair.television);

                    job.count = 1;
                    selPawn.jobs.TryTakeOrderedJob(job);
                };

                __result = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Brainwash_TakeForBrainwashPersonality".Translate(), action)
                };
                return false;
            }

            __result = Enumerable.Empty<FloatMenuOption>();
            return false;
        }
    }

    [HarmonyPatch(typeof(Brainwash.JobDriver_StartBrainwashTelevision))]
    [HarmonyPatch(nameof(Brainwash.JobDriver_StartBrainwashTelevision.BrainwashEffect))]
    public static class BrainwashEffectRandomise
    {
        public static bool Prefix(Brainwash.JobDriver_StartBrainwashTelevision __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null) return true;
            if (!pawn.RaceProps.Humanlike) return true;

            try
            {
                Superpack_Brainwash.ApplyRandomBrainwashToPawn(pawn);

                return false;
            }
            catch (Exception e)
            {
                Log.Error($"[Brainwash_Patch] Exception while randomising pawn: {pawn}. Falling back to original BrainwashEffect. Exception: {e}");
                return true;
            }
        }
    }
}
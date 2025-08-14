using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;
using Verse;
using PawnGraphicUtils = MSSFP.Utils.PawnGraphicUtils;

namespace MSSFP.Comps;

public class CompAbilityEffect_BodyHopImproved : CompAbilityEffect
{
    public new CompProperties_AbilityBodyHopImproved Props =>
        (CompProperties_AbilityBodyHopImproved)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = parent.pawn;
        if (target.Thing is not Pawn host)
            return;

        Hediff hd = host.health.hediffSet.GetFirstHediffOfDef(Props.hediffOnSelf);
        if (hd != null)
        {
            host.health.RemoveHediff(hd);
        }

        string texPath = PawnGraphicUtils.SavePawnTexture(host);
        string originalName = host.NameFullColored;

        host.Name = NameTriple.FromString(caster.Name.ToString());
        host.story.title = caster.story.title;
        host.story.favoriteColor = caster.story.favoriteColor;
        host.story.Childhood = caster.story.Childhood;
        host.story.Adulthood = caster.story.Adulthood;

        SkillRecord origSkill = host.skills.skills.MaxBy(skill => skill.levelInt);
        SkillDef bestSkill = origSkill.def;
        int skillOffset = origSkill.levelInt / 4;

        int numTraits = host.story.traits.allTraits.Count();

        List<TraitDef> traits = host
            .story.traits.allTraits.Where(trait => trait.sourceGene == null)
            .InRandomOrder()
            .Take(numTraits * Rand.RangeInclusive(0, 3))
            .Select(t => t.def)
            .ToList();

        foreach (Trait trait in caster.story.traits.allTraits)
        {
            host.story.traits.GainTrait(trait);
        }

        host.skills.skills.Clear();

        foreach (SkillRecord skill in caster.skills.skills)
        {
            SkillRecord item = new SkillRecord(host, skill.def)
            {
                levelInt = skill.levelInt,
                passion = skill.passion,
                xpSinceLastLevel = skill.xpSinceLastLevel,
                xpSinceMidnight = skill.xpSinceMidnight,
            };
            host.skills.skills.Add(item);
        }

        foreach (Ability ability in caster.abilities.abilities)
        {
            if (host.abilities.GetAbility(ability.def) == null)
            {
                host.abilities.GainAbility(ability.def);
            }
        }

        List<Ability> abilities = host.abilities.abilities;
        for (int num = abilities.Count - 1; num >= 0; num--)
        {
            Ability ability = abilities[num];
            if (caster.abilities.GetAbility(ability.def) == null)
            {
                host.abilities.RemoveAbility(ability.def);
            }
        }

        if (caster.royalty != null)
        {
            foreach (RoyalTitle item in caster.royalty.AllTitlesForReading)
            {
                foreach (AbilityDef grantedAbility in item.def.grantedAbilities)
                {
                    if (host.abilities.GetAbility(grantedAbility) != null)
                    {
                        host.abilities.RemoveAbility(grantedAbility);
                    }
                }
            }
        }

        //Find.PawnDuplicator.CopyHediffs(caster, host);

        List<Hediff> hediffs = caster.health.hediffSet.hediffs;
        foreach (Hediff item in hediffs)
        {
            if (item.def == MSSFPDefOf.MSS_FP_PawnDisplayerPossession)
                continue;
            // if (!item.def.duplicationAllowed ||
            //     (item.Part != null && !host.health.hediffSet.HasBodyPart(item.Part)) ||
            //     (item is Hediff_AddedPart && !item.def.organicAddedBodypart) ||
            //     (item is Hediff_Implant && !item.def.organicAddedBodypart))
            // {
            //     continue;
            // }
            //
            // Hediff hediff = HediffMaker.MakeHediff(item.def, host, item.Part);
            // hediff.CopyFrom(item);
            // host.health.hediffSet.AddDirect(hediff);

            host.health.hediffSet.hediffs.Add(item);
            item.pawn = host;
            item.Notify_Spawned();
            item.PostAdd(null);
        }

        host.needs ??= new Pawn_NeedsTracker(host);
        host.needs.AllNeeds?.Clear();

        foreach (Need allNeed in caster.needs.AllNeeds)
        {
            Need need = (Need)Activator.CreateInstance(allNeed.def.needClass, host);
            need.def = allNeed.def;
            host.needs?.AllNeeds.Add(need);
            need.SetInitialLevel();
            need.CurLevel = allNeed.CurLevel;
            host.needs.BindDirectNeedFields();
        }

        for (int h = 0; h < 24; h++)
        {
            host.timetable.SetAssignment(h, TimeAssignmentDefOf.Work);
        }

        foreach (WorkTypeDef work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            host.workSettings.SetPriority(work, parent.pawn.workSettings.GetPriority(work));
        }

        host.guest.SetGuestStatus(caster.Faction, caster.guest.GuestStatus);
        if (caster.needs.mood != null)
        {
            List<Thought_Memory> memories = host.needs.mood.thoughts.memories.Memories;
            memories.Clear();
            foreach (Thought_Memory memory in caster.needs.mood.thoughts.memories.Memories)
            {
                Thought_Memory thought_Memory = (Thought_Memory)
                    ThoughtMaker.MakeThought(memory.def);
                thought_Memory.CopyFrom(memory);
                thought_Memory.pawn = host;
                memories.Add(thought_Memory);
            }
        }

        foreach (DirectPawnRelation relation in caster.relations.DirectRelations)
        {
            host.relations.AddDirectRelation(relation.def, relation.otherPawn);
        }

        caster.health.AddHediff(Props.hediffOnSelf);
        host.health.hediffSet.TryGetHediff(
            MSSFPDefOf.MSS_FP_PawnDisplayerPossession,
            out Hediff hostHediff
        );
        caster.health.hediffSet.TryGetHediff(
            MSSFPDefOf.MSS_FP_PawnDisplayerPossession,
            out Hediff casterHediff
        );

        hostHediff ??= host.health.AddHediff(MSSFPDefOf.MSS_FP_PawnDisplayerPossession);

        HediffComp_BodyHopHaunt haunt = hostHediff.TryGetComp<HediffComp_BodyHopHaunt>();

        if (haunt is null)
            return;

        HediffComp_BodyHopHaunt casterHauntComp =
            casterHediff?.TryGetComp<HediffComp_BodyHopHaunt>();
        if (casterHauntComp != null)
        {
            foreach (HediffComp_BodyHopHaunt.PawnInfo pawn in casterHauntComp.pawns.ToList())
            {
                haunt.AddNewPawn(pawn);
            }
        }

        haunt.AddNewPawn(originalName, "", bestSkill, skillOffset, traits, texturePath: texPath);

        if (caster.story is not null)
        {
            AccessTools.Field(typeof(Pawn_StoryTracker), "childhood").SetValue(caster.story, null);
            caster.story.Adulthood = null;
            caster.story.traits = new TraitSet(caster);
            caster.ChangePsylinkLevel(0);
            caster.abilities.abilities.Clear();
            caster.abilities.Notify_TemporaryAbilitiesChanged();
        }

        foreach (SkillRecord t in caster.skills.skills)
        {
            t.levelInt = 0;
            t.xpSinceLastLevel = 0;
            t.xpSinceMidnight = 0;
            t.passion = 0;
        }

        caster.needs = new Pawn_NeedsTracker(caster);
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

[StaticConstructorOnStartup]
public class HediffComp_Haunt: HediffComp
{
    public static Texture2D icon = ContentFinder<Texture2D>.Get("UI/MSS_FP_Haunts_Toggle");
    public Pawn pawnToDraw;
    public string name;
    public string texPath;
    public Texture2D pawnTexture;

    public virtual string PawnName => name;

    public virtual Texture2D PawnTexture
    {
        get
        {
            if (pawnTexture == null)
            {
                pawnTexture = PawnGraphicUtils.LoadTexture(Path.Combine(PawnGraphicUtils.SaveDataPath, TexPath));
            }
            return pawnTexture;
        }
    }

    public virtual string TexPath
    {
        get => texPath;
    }

    protected Dictionary<SkillDef, int> aptitudesCached = new Dictionary<SkillDef, int>();

    private HediffCompProperties_Haunt Props => props as HediffCompProperties_Haunt;


    public override string CompLabelInBracketsExtra
    {
        get
        {
            if(PawnName != null) return "MSS_FP_HauntedBy".Translate(PawnName);
            return pawnToDraw == null ? null : "MSS_FP_HauntedBy".Translate(pawnToDraw.NameShortColored);
        }
    }

    public override string CompDescriptionExtra
    {
        get
        {
            if (pawnToDraw == null) return null;
            return pawnToDraw == null ? null : "\n\n" + "MSS_FP_HauntedUnBuff".Translate(pawnToDraw.NameShortColored, parent.pawn.NameShortColored);

            // if (aptitudesCached.NullOrEmpty())
            // {
            //     foreach (SkillRecord skillsSkill in pawnToDraw.skills.skills)
            //     {
            //         AptitudeFor(skillsSkill.def);
            //     }
            // };
            // KeyValuePair<SkillDef, int> maxSkill = aptitudesCached.MaxBy(x => x.Value);
            //
            // return pawnToDraw == null ? null : "\n\n" + "MSS_FP_HauntedBuff".Translate(pawnToDraw.NameShortColored, maxSkill.Key.skillLabel, maxSkill.Value);
        }
    }

    public virtual void DrawAt(Vector3 drawPos)
    {
        if(!MSSFPMod.settings.ShowHaunts) return;
        if (Props.onlyRenderWhenDrafted && Pawn.drafter is not { Drafted: true })
        {
            return;
        }

        if(Props.graphicData.Graphic is PawnHauntGraphic && TexPath == null) return;

        if (Props.graphicData?.Graphic is PawnHauntGraphic gfx && TexPath != null)
        {
            gfx.SetOverrideMaterial(pawnTexture);
        }

        Vector3 offset = new();

        if (Props.offsets != null)
        {
            if (Props.offsets.Count == 4)
            {
                offset = Props.offsets[Pawn.Rotation.AsInt];
            }
            else
            {
                offset = Props.offsets[0];
            }
        }

        Rot4 rot = Pawn.Rotation;
        if (Props.graphicData is { Graphic: PawnHauntGraphic })
        {
            rot = Rot4.North;
        }
        Props.graphicData?.Graphic.Draw(new Vector3(drawPos.x, AltitudeLayer.Pawn.AltitudeFor(), drawPos.z) + offset, rot, pawnToDraw ?? parent.pawn);
    }

    public override void CompPostMake()
    {
        base.CompPostMake();
        HauntsCache.AddHaunt(Pawn.thingIDNumber, this);
    }

    public virtual void SetPawnToDraw(Pawn pawn)
    {
        pawnToDraw = pawn;
        aptitudesCached.Clear();
        if (Pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(Props.thought) is Thought_Memory thought)
        {
            thought.otherPawn = pawnToDraw;
        }
        else
        {
            TryAddMemory();
        }
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        HauntsCache.RemoveHaunt(Pawn.thingIDNumber, this);
        if(Props.thought != null) Pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(Props.thought);
    }

    public override void CompExposeData()
    {
        base.CompExposeData();

        Scribe_References.Look(ref pawnToDraw, "pawnToDraw");
        Scribe_Values.Look(ref texPath, "texPath");
        Scribe_Values.Look(ref name, "name");

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            HauntsCache.AddHaunt(Pawn.thingIDNumber, this);
        }
    }


    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }

        Command_Target showPawn = new Command_Target();
        showPawn.defaultLabel = "Select Pawn To Draw";
        showPawn.targetingParams = TargetingParameters.ForColonist();
        showPawn.icon = icon;
        showPawn.action = (info =>
        {
            SetPawnToDraw(info.Pawn);
        });

        yield return showPawn;
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        TryAddMemory();
    }

    public override void Notify_Spawned() => TryAddMemory();

    private void TryAddMemory()
    {
        if(pawnToDraw is null) return;
        if(Props.thought == null) return;
        if (Pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(Props.thought) != null)
            return;
        Thought_Memory newThought = (Thought_Memory) ThoughtMaker.MakeThought(Props.thought);
        newThought.permanent = true;
        Pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(newThought, pawnToDraw);
    }

    // public int AptitudeFor(SkillDef skill)
    // {
    //     ModLog.Debug("HediffComp_Haunt.AptitudeFor:Start");
    //     if(pawnToDraw is null) return 0;
    //
    //     if(aptitudesCached.TryGetValue(skill, out int aptitudes)) return aptitudes;
    //
    //     SkillRecord maxSkill = pawnToDraw.skills.skills.MaxBy(pawnSkill => pawnSkill.Level);
    //
    //     if (maxSkill.def != skill)
    //     {
    //         aptitudesCached[skill] = 0;
    //         return 0;
    //     }
    //
    //     int modifier = Mathf.CeilToInt(maxSkill.Level / 3f);
    //
    //     aptitudesCached[skill] = modifier;
    //
    //     ModLog.Debug("HediffComp_Haunt.AptitudeFor:End");
    //     return modifier;
    // }
}

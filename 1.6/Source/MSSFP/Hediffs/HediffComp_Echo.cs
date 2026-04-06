using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using PawnGraphicUtils = MSSFP.Utils.PawnGraphicUtils;

namespace MSSFP.Hediffs;

[StaticConstructorOnStartup]
public class HediffComp_Echo : HediffComp_Haunt
{
    public Dictionary<PawnInfo, Texture2D> pawnTextureCache = new Dictionary<PawnInfo, Texture2D>();

    public PawnInfo pawnToShow;

    private new HediffCompProperties_Echo Props => props as HediffCompProperties_Echo;
    public override string TexPath
    {
        get => pawnToShow?.texPath ?? null;
    }

    public override string PawnName
    {
        get => pawnToShow?.name ?? null;
    }

    public override Texture2D PawnTexture
    {
        get
        {
            if (pawnToShow == null)
                return null;
            if (!pawnTextureCache.ContainsKey(pawnToShow))
            {
                pawnTextureCache[pawnToShow] = PawnGraphicUtils.LoadTexture(
                    Path.Combine(PawnGraphicUtils.SaveDataPath, TexPath)
                );
            }

            return pawnTextureCache[pawnToShow];
        }
    }

    public class PawnInfo : IExposable
    {
        public string name;
        public string description;
        public SkillDef bestSkill;
        public int skillOffset;
        public List<TraitDef> passedTraits;
        public int swapTick = -1;
        public int id = Rand.Int;
        public string texPath = null;

        public PawnInfo Copy()
        {
            return new PawnInfo
            {
                name = name,
                description = description,
                bestSkill = bestSkill,
                skillOffset = skillOffset,
                passedTraits = passedTraits?.ToList(),
                swapTick = swapTick,
                id = id,
                texPath = texPath,
            };
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                name = name.Replace(",", "");
            }
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref description, "description");
            Scribe_Defs.Look(ref bestSkill, "bestSkill");
            Scribe_Values.Look(ref skillOffset, "skillOffset");
            Scribe_Collections.Look(ref passedTraits, "passedTraits", LookMode.Def);
            Scribe_Values.Look(ref swapTick, "swapTick");
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref texPath, "texPath");
        }

        public override string ToString()
        {
            return $"{name} is providing a boost to {bestSkill.skillLabel} of {skillOffset}.";
        }
    }

    public List<PawnInfo> pawns = new List<PawnInfo>();

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Deep.Look(ref pawnToShow, "pawnToShow");
        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Deep);
    }

    public virtual void AddNewPawn(
        string pawnName,
        string description,
        SkillDef bestSkill,
        int skillOffset,
        List<TraitDef> passedTraits,
        int swapTick = -1,
        string texturePath = null
    )
    {
        if (swapTick < 0)
            swapTick = Find.TickManager.TicksGame;

        AddNewPawn(
            new PawnInfo
            {
                name = pawnName,
                description = description,
                bestSkill = bestSkill,
                skillOffset = skillOffset,
                passedTraits = passedTraits,
                swapTick = swapTick,
                texPath = texturePath,
            }
        );
    }

    public virtual void AddNewPawn(PawnInfo pawnInfo)
    {
        PawnInfo existingPawn = pawns.FirstOrDefault(x => x.id == pawnInfo.id);

        if (existingPawn != null)
        {
            if (!existingPawn.passedTraits.NullOrEmpty())
            {
                foreach (TraitDef t in existingPawn.passedTraits)
                {
                    Trait trait = parent.pawn.story.traits.GetTrait(t);
                    if (trait != null)
                        parent.pawn.story.traits.RemoveTrait(trait);
                }
            }

            pawns.Remove(existingPawn);
        }

        if (pawnInfo.swapTick < 0)
            pawnInfo.swapTick = Find.TickManager.TicksGame;
        pawns.Add(pawnInfo);

        if (!pawnInfo.passedTraits.NullOrEmpty())
        {
            foreach (TraitDef def in pawnInfo.passedTraits)
            {
                if (!parent.pawn.story.traits.HasTrait(def))
                {
                    parent.pawn.story.traits.GainTrait(new Trait(def));
                }
            }
        }

        pawnToShow = pawnInfo;

        // Skill boosts are applied via SkillRecord_Patch + HauntsCache
        HauntsCache.RebuildCacheForPawn(parent.pawn);
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }

        Command_Action showPawn = new Command_Action();
        showPawn.defaultLabel = "Add New Pawn History";
        showPawn.icon = icon;
        showPawn.action = delegate
        {
            Find.WindowStack.Add(new PawnHistoryEditorWindow(this));
        };

        yield return showPawn;
    }

    public override IEnumerable<string> GetAllTexPaths()
    {
        foreach (PawnInfo p in pawns)
        {
            if (p.texPath != null)
                yield return p.texPath;
        }
    }

    /// <summary>
    /// Returns skill boosts from all echo history entries. The base class's
    /// skillToBoost/SkillBoostLevel is NOT used — all boosts come from the pawns list.
    /// EchoHost gene doubles all boosts.
    /// </summary>
    public override IEnumerable<(SkillDef skill, int boost)> GetSkillBoosts()
    {
        bool spiritHost =
            parent.pawn.genes?.HasActiveGene(MSSFPDefOf.MSS_FP_Gene_SpiritHost) == true;
        int multiplier = spiritHost ? 2 : 1;

        foreach (PawnInfo pawnInfo in pawns)
        {
            if (pawnInfo.bestSkill != null && pawnInfo.skillOffset > 0)
                yield return (pawnInfo.bestSkill, pawnInfo.skillOffset * multiplier);
        }
    }

    public override string CompDescriptionExtra
    {
        get
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                "This pawn is co-existing with the memories and personalities of the following:"
            );

            foreach (PawnInfo pawnInfo in pawns)
            {
                sb.AppendLine(pawnInfo.ToString());
            }

            return sb.ToString();
        }
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        // Re-apply traits from echo history on hediff add (e.g. after load)
        foreach (PawnInfo pawnInfo in pawns)
        {
            if (!pawnInfo.passedTraits.NullOrEmpty())
            {
                foreach (TraitDef def in pawnInfo.passedTraits)
                {
                    if (!parent.pawn.story.traits.HasTrait(def))
                    {
                        parent.pawn.story.traits.GainTrait(new Trait(def));
                    }
                }
            }
        }

        // Skill boosts are applied via SkillRecord_Patch + HauntsCache.
        // Cache is rebuilt by the base class CompPostPostAdd → RebuildCacheForPawn.
        HauntsCache.RebuildCacheForPawn(parent.pawn);
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 0)
        {
            pawnToShow = pawns.RandomElement();
        }
    }

    public override void CompPostPostRemoved()
    {
        // Remove traits from echo history
        foreach (PawnInfo pawnInfo in pawns)
        {
            if (!pawnInfo.passedTraits.NullOrEmpty())
            {
                foreach (TraitDef def in pawnInfo.passedTraits)
                {
                    Trait existing = parent.pawn.story.traits.GetTrait(def);
                    if (existing != null)
                        parent.pawn.story.traits.RemoveTrait(existing);
                }
            }
        }

        // Skill boost removal is automatic — base CompPostPostRemoved calls
        // RebuildCacheForPawn which will exclude this comp's contributions.
    }
}

using System.Collections.Generic;
using System.Linq;
using MSSFP.PawnPortability.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.PawnPortability.Defs
{
    public enum PawnTemplateMode
    {
        Template,
        Snapshot
    }

    public class ModRequirement
    {
        public string packageId;
        public string name;
    }

    public class PawnNameData
    {
        public string first;
        public string nick;
        public string last;
    }

    public class PawnAgeData
    {
        public long biologicalAgeTicks;
        public long chronologicalAgeTicks;
    }

    public class PawnStoryData
    {
        public BackstoryDef childhood;
        public BackstoryDef adulthood;
        public BodyTypeDef bodyType;
        public HeadTypeDef headType;
        public HairDef hairDef;
        public Color hairColor;
        public Color? skinColorOverride;
        public FurDef furDef;
        public ColorDef favoriteColor;
        public string birthLastName;
        public List<PawnTraitEntry> traits;
    }

    public class PawnTraitEntry
    {
        public TraitDef def;
        public int degree;
    }

    public class PawnSkillEntry
    {
        public SkillDef def;
        public int level;
        public Passion passion = Passion.None;
    }

    public class PawnGeneData
    {
        public XenotypeDef xenotype;
        public string xenotypeName;
        public List<GeneDef> endogenes;
        public List<GeneDef> xenogenes;
    }

    public class PawnHediffEntry
    {
        public HediffDef def;
        public float severity = -1f;
        public BodyPartDef bodyPart;
        public string bodyPartLabel;
        public bool permanent;
    }

    public class PawnItemEntry
    {
        public ThingDef def;
        public ThingDef stuff;
        public QualityCategory quality = QualityCategory.Normal;
        public int hitPoints = -1;
        public ColorDef color;
        public int stackCount = 1;
    }

    public class PawnMemoryEntry
    {
        public ThoughtDef def;
        public int stage;
    }

    public class PawnStyleData
    {
        public BeardDef beardDef;
        public TattooDef faceTattoo;
        public TattooDef bodyTattoo;
    }

    public class PawnIdeoData
    {
        public float certainty = 1f;
        public string ideoName;
    }

    public class PawnRoyaltyData
    {
        public List<PawnRoyalTitleEntry> titles;
        public int favor;
    }

    public class PawnRoyalTitleEntry
    {
        public RoyalTitleDef def;
        public FactionDef faction;
    }

    public class PawnWorkEntry
    {
        public WorkTypeDef def;
        public int priority;
    }

    public class PawnTemplateDef : Def
    {
        public int schemaVersion = 1;
        public PawnTemplateMode mode = PawnTemplateMode.Template;
        public List<ModRequirement> requiredMods;

        // Character metadata — no effect on spawning
        public string originSeries;
        public List<string> tags;
        public string narrativeNotes;

        // Pawn identity
        public PawnKindDef kindDef;
        public Gender gender = Gender.Male;
        public PawnNameData name;
        public PawnAgeData age;
        public PawnStoryData story;
        public List<PawnSkillEntry> skills;
        public PawnGeneData genes;
        public List<PawnHediffEntry> hediffs;
        public List<PawnItemEntry> equipment;
        public List<PawnItemEntry> apparel;
        public List<PawnItemEntry> inventory;
        public List<AbilityDef> abilities;
        public PawnStyleData style;

        // Snapshot-only fields (Phase 2)
        public List<PawnMemoryEntry> permanentMemories;
        public PawnIdeoData ideology;
        public PawnRoyaltyData royalty;
        public List<PawnWorkEntry> workPriorities;

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            bool logging = PawnPortabilitySettings.LoggingEnabled;

            // Rule 1: Remove wrapper objects where the primary Def is null (mod missing)
            FilterNullDefs(equipment, "Equipment", logging);
            FilterNullDefs(apparel, "Apparel", logging);
            FilterNullDefs(inventory, "Inventory", logging);
            FilterNullHediffs(hediffs, logging);
            FilterNullTraits(story?.traits, logging);
            FilterNullDefs(permanentMemories, logging);
            FilterNullTitles(royalty?.titles, logging);
            FilterNullWorkEntries(workPriorities, logging);

            // Rule 2: Filter null entries from simple Def lists
            FilterNullListEntries(genes?.endogenes, "Endogene", logging);
            FilterNullListEntries(genes?.xenogenes, "Xenogene", logging);
            FilterNullListEntries(abilities, "Ability", logging);

            // Rule 3: Fix stuff fallbacks — if stuff is null but def requires stuff
            FixStuffFallbacks(equipment, logging);
            FixStuffFallbacks(apparel, logging);
            FixStuffFallbacks(inventory, logging);

            // Rule 4: Apply critical fallbacks
            if (story?.bodyType == null && mode == PawnTemplateMode.Snapshot)
            {
                BodyTypeDef fallback = gender == Gender.Female
                    ? BodyTypeDefOf.Female
                    : BodyTypeDefOf.Male;
                if (logging)
                    ModLog.Log($"[PawnPortability] {defName}: bodyType missing, falling back to {fallback.defName}");
                story.bodyType = fallback;
            }

            if (genes?.xenotype == null && genes != null && ModsConfig.BiotechActive)
            {
                if (logging)
                    ModLog.Log($"[PawnPortability] {defName}: xenotype missing, falling back to Baseliner");
                genes.xenotype = XenotypeDefOf.Baseliner;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
                yield return error;

            if (mode == PawnTemplateMode.Snapshot && name == null)
                yield return "Snapshot mode requires a name";

            if (name != null && string.IsNullOrEmpty(name.first) && string.IsNullOrEmpty(name.last))
                yield return "Name must have at least a first or last name";
        }

        private static void FilterNullDefs(List<PawnItemEntry> list, string category, bool logging)
        {
            if (list == null) return;
            int removed = list.RemoveAll(e =>
            {
                if (e.def != null) return false;
                if (logging)
                    ModLog.Log($"[PawnPortability] Skipped {category} item: def resolved to null (mod not active)");
                return true;
            });
        }

        private static void FilterNullHediffs(List<PawnHediffEntry> list, bool logging)
        {
            if (list == null) return;
            list.RemoveAll(h =>
            {
                if (h.def != null) return false;
                if (logging)
                    ModLog.Log("[PawnPortability] Skipped hediff: def resolved to null (mod not active)");
                return true;
            });
        }

        private static void FilterNullTraits(List<PawnTraitEntry> list, bool logging)
        {
            if (list == null) return;
            list.RemoveAll(t =>
            {
                if (t.def != null) return false;
                if (logging)
                    ModLog.Log("[PawnPortability] Skipped trait: def resolved to null (mod not active)");
                return true;
            });
        }

        private static void FilterNullDefs(List<PawnMemoryEntry> list, bool logging)
        {
            if (list == null) return;
            list.RemoveAll(m =>
            {
                if (m.def != null) return false;
                if (logging)
                    ModLog.Log("[PawnPortability] Skipped memory: def resolved to null (mod not active)");
                return true;
            });
        }

        private static void FilterNullTitles(List<PawnRoyalTitleEntry> list, bool logging)
        {
            if (list == null) return;
            list.RemoveAll(t =>
            {
                if (t.def != null) return false;
                if (logging)
                    ModLog.Log("[PawnPortability] Skipped royal title: def resolved to null (mod not active)");
                return true;
            });
        }

        private static void FilterNullWorkEntries(List<PawnWorkEntry> list, bool logging)
        {
            if (list == null) return;
            list.RemoveAll(w =>
            {
                if (w.def != null) return false;
                if (logging)
                    ModLog.Log("[PawnPortability] Skipped work priority: def resolved to null (mod not active)");
                return true;
            });
        }

        private static void FilterNullListEntries<T>(List<T> list, string category, bool logging)
            where T : Def
        {
            if (list == null) return;
            int removed = list.RemoveAll(d =>
            {
                if (d != null) return false;
                if (logging)
                    ModLog.Log($"[PawnPortability] Skipped {category}: def resolved to null (mod not active)");
                return true;
            });
        }

        private static void FixStuffFallbacks(List<PawnItemEntry> list, bool logging)
        {
            if (list == null) return;
            foreach (PawnItemEntry item in list)
            {
                if (item.stuff == null && item.def != null && item.def.MadeFromStuff)
                {
                    item.stuff = GenStuff.DefaultStuffFor(item.def);
                    if (logging)
                        ModLog.Log(
                            $"[PawnPortability] Stuff missing for {item.def.defName}, " +
                            $"falling back to {item.stuff?.defName ?? "null"}");
                }
            }
        }
    }
}

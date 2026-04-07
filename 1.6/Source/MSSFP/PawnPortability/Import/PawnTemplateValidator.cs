using System.Collections.Generic;
using System.Linq;
using MSSFP.PawnPortability.Defs;
using RimWorld;
using Verse;

namespace MSSFP.PawnPortability.Import
{
    public class MissingItem
    {
        public string Category { get; }
        public string DefName { get; }
        public string RequiredMod { get; }

        public MissingItem(string category, string defName, string requiredMod)
        {
            Category = category;
            DefName = defName;
            RequiredMod = requiredMod;
        }

        public override string ToString() =>
            $"[{Category}] {DefName} (requires: {RequiredMod ?? "unknown"})";
    }

    public class FallbackDecision
    {
        public string Field { get; }
        public string Original { get; }
        public string FallbackValue { get; }
        public string Reason { get; }

        public FallbackDecision(string field, string original, string fallbackValue, string reason)
        {
            Field = field;
            Original = original;
            FallbackValue = fallbackValue;
            Reason = reason;
        }

        public override string ToString() =>
            $"{Field}: '{Original}' -> '{FallbackValue}' ({Reason})";
    }

    public class ValidationReport
    {
        public bool CanSpawn { get; }
        public bool IsFullFidelity => MissingItems.Count == 0 && Fallbacks.Count == 0;
        public List<Defs.ModRequirement> MissingMods { get; }
        public List<MissingItem> MissingItems { get; }
        public List<FallbackDecision> Fallbacks { get; }
        public string Summary { get; }

        public ValidationReport(
            bool canSpawn,
            List<Defs.ModRequirement> missingMods,
            List<MissingItem> missingItems,
            List<FallbackDecision> fallbacks)
        {
            CanSpawn = canSpawn;
            MissingMods = missingMods ?? new List<Defs.ModRequirement>();
            MissingItems = missingItems ?? new List<MissingItem>();
            Fallbacks = fallbacks ?? new List<FallbackDecision>();
            Summary = BuildSummary();
        }

        private string BuildSummary()
        {
            if (IsFullFidelity) return "Full fidelity — all content available.";

            var parts = new List<string>();
            if (MissingMods.Count > 0)
                parts.Add($"{MissingMods.Count} missing mod(s)");
            if (MissingItems.Count > 0)
                parts.Add($"{MissingItems.Count} missing item(s)");
            if (Fallbacks.Count > 0)
                parts.Add($"{Fallbacks.Count} fallback(s)");

            string status = CanSpawn ? "Can spawn with degraded fidelity" : "Cannot spawn";
            return $"{status}: {string.Join(", ", parts)}.";
        }
    }

    /// <summary>
    /// Validates a PawnTemplateDef against currently active mods.
    /// </summary>
    internal static class PawnTemplateValidator
    {
        public static ValidationReport Validate(PawnTemplateDef def)
        {
            if (def == null)
                return new ValidationReport(false, null, null, null);

            var missingMods = new List<Defs.ModRequirement>();
            var missingItems = new List<MissingItem>();
            var fallbacks = new List<FallbackDecision>();

            // Check requiredMods
            if (def.requiredMods != null)
            {
                foreach (Defs.ModRequirement req in def.requiredMods)
                {
                    if (!ModsConfig.IsActive(req.packageId))
                        missingMods.Add(req);
                }
            }

            // Check genes
            CheckDefList(def.genes?.endogenes, "Gene", missingItems);
            CheckDefList(def.genes?.xenogenes, "Gene", missingItems);

            if (def.genes?.xenotype == null && def.genes != null && ModsConfig.BiotechActive)
            {
                fallbacks.Add(new FallbackDecision(
                    "xenotype", "null", "Baseliner", "Xenotype not specified"));
            }

            // Check traits
            if (def.story?.traits != null)
            {
                foreach (PawnTraitEntry entry in def.story.traits.Where(e => e.def == null))
                    missingItems.Add(new MissingItem("Trait", "unknown", null));
            }

            // Check hediffs
            if (def.hediffs != null)
            {
                foreach (PawnHediffEntry entry in def.hediffs.Where(e => e.def == null))
                    missingItems.Add(new MissingItem("Hediff", "unknown", null));
            }

            // Check equipment
            CheckItemList(def.equipment, "Equipment", missingItems, fallbacks);
            CheckItemList(def.apparel, "Apparel", missingItems, fallbacks);
            CheckItemList(def.inventory, "Inventory", missingItems, fallbacks);

            // Check abilities
            CheckDefList(def.abilities, "Ability", missingItems);

            // Body type fallback
            if (def.story?.bodyType == null && def.mode == PawnTemplateMode.Snapshot)
            {
                string fallbackType = def.gender == Gender.Female ? "Female" : "Male";
                fallbacks.Add(new FallbackDecision(
                    "bodyType", "null", fallbackType, "Body type not specified in snapshot mode"));
            }

            // A pawn can always spawn in template mode — missing content is just skipped
            bool canSpawn = def.mode == PawnTemplateMode.Template || def.name != null;

            return new ValidationReport(canSpawn, missingMods, missingItems, fallbacks);
        }

        private static void CheckDefList<T>(List<T> list, string category,
            List<MissingItem> missingItems) where T : Def
        {
            if (list == null) return;
            foreach (T item in list.Where(d => d == null))
                missingItems.Add(new MissingItem(category, "unknown", null));
        }

        private static void CheckItemList(List<PawnItemEntry> list, string category,
            List<MissingItem> missingItems, List<FallbackDecision> fallbacks)
        {
            if (list == null) return;
            foreach (PawnItemEntry entry in list)
            {
                if (entry.def == null)
                {
                    missingItems.Add(new MissingItem(category, "unknown", null));
                    continue;
                }

                if (entry.stuff == null && entry.def.MadeFromStuff)
                {
                    ThingDef defaultStuff = GenStuff.DefaultStuffFor(entry.def);
                    fallbacks.Add(new FallbackDecision(
                        $"{category}.stuff", "null",
                        defaultStuff?.defName ?? "null",
                        "Stuff material missing, using default"));
                }
            }
        }
    }
}

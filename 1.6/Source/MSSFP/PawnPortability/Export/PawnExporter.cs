using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using MSSFP.PawnPortability.Defs;
using MSSFP.PawnPortability.Settings;
using MSSFP.PawnPortability.Xml;
using RimWorld;
using Verse;

namespace MSSFP.PawnPortability.Export
{
    /// <summary>
    /// Metadata provided by the export dialog to override auto-generated values.
    /// </summary>
    public class ExportMetadata
    {
        public string DefName;
        public string OriginSeries;
        public string NarrativeNotes;
    }

    /// <summary>
    /// Lightweight mod dependency info for display in the export dialog.
    /// </summary>
    internal class RequiredModInfo
    {
        public string PackageId { get; }
        public string Name { get; }

        public RequiredModInfo(string packageId, string name)
        {
            PackageId = packageId;
            Name = name;
        }
    }

    /// <summary>
    /// Exports a live Pawn to PawnTemplateDef XML.
    /// Template mode: exports identity fields only (appearance, traits, skills, genes,
    /// identity hediffs, abilities, equipment, apparel, style).
    /// </summary>
    internal static class PawnExporter
    {
        private const string CorePackageId = "Ludeon.RimWorld";
        private static readonly Regex SafeDefNameChars = new Regex("[^a-zA-Z0-9_-]");

        public static bool Export(Pawn pawn, string filePath, PawnTemplateMode mode,
            ExportMetadata metadata = null)
        {
            try
            {
                string xml = ExportToString(pawn, mode, metadata);
                if (xml == null) return false;

                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, xml);

                if (PawnPortabilitySettings.LoggingEnabled)
                    ModLog.Log($"[PawnPortability] Exported {pawn.LabelShort} to {filePath}");

                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to export pawn {pawn?.LabelShort}", ex);
                return false;
            }
        }

        /// <summary>
        /// Pre-scan a pawn to collect required (non-Core) mod dependencies.
        /// Used by the export dialog to show the mods list before export.
        /// </summary>
        public static List<RequiredModInfo> CollectRequiredMods(Pawn pawn)
        {
            if (pawn == null) return new List<RequiredModInfo>();

            var packageIds = new HashSet<string>();

            void Collect(Def def)
            {
                string pid = GetPackageId(def);
                if (pid != null && !string.Equals(pid, CorePackageId, StringComparison.OrdinalIgnoreCase))
                    packageIds.Add(pid);
            }

            // KindDef
            if (pawn.kindDef != null) Collect(pawn.kindDef);

            // Story
            if (pawn.story != null)
            {
                if (pawn.story.bodyType != null) Collect(pawn.story.bodyType);
                if (pawn.story.headType != null) Collect(pawn.story.headType);
                if (pawn.story.hairDef != null) Collect(pawn.story.hairDef);
                if (pawn.story.Childhood != null) Collect(pawn.story.Childhood);
                if (pawn.story.Adulthood != null) Collect(pawn.story.Adulthood);
                if (pawn.story.favoriteColor != null) Collect(pawn.story.favoriteColor);
                if (pawn.story.furDef != null) Collect(pawn.story.furDef);
                if (pawn.story.traits?.allTraits != null)
                    foreach (Trait t in pawn.story.traits.allTraits)
                        Collect(t.def);
            }

            // Genes
            if (pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null) Collect(pawn.genes.Xenotype);
                if (pawn.genes.Endogenes != null)
                    foreach (Gene g in pawn.genes.Endogenes) Collect(g.def);
                if (pawn.genes.Xenogenes != null)
                    foreach (Gene g in pawn.genes.Xenogenes) Collect(g.def);
            }

            // Hediffs
            if (pawn.health?.hediffSet != null)
                foreach (Hediff h in pawn.health.hediffSet.hediffs)
                    Collect(h.def);

            // Equipment
            if (pawn.equipment?.AllEquipmentListForReading != null)
                foreach (ThingWithComps t in pawn.equipment.AllEquipmentListForReading)
                {
                    Collect(t.def);
                    if (t.Stuff != null) Collect(t.Stuff);
                }

            // Apparel
            if (pawn.apparel?.WornApparel != null)
                foreach (Apparel a in pawn.apparel.WornApparel)
                {
                    Collect(a.def);
                    if (a.Stuff != null) Collect(a.Stuff);
                }

            // Abilities
            if (pawn.abilities?.abilities != null)
                foreach (Ability a in pawn.abilities.abilities)
                    Collect(a.def);

            // Style
            if (pawn.style != null)
            {
                if (pawn.style.beardDef != null) Collect(pawn.style.beardDef);
                if (pawn.style.FaceTattoo != null) Collect(pawn.style.FaceTattoo);
                if (pawn.style.BodyTattoo != null) Collect(pawn.style.BodyTattoo);
            }

            return packageIds
                .OrderBy(pid => pid)
                .Select(pid =>
                {
                    ModContentPack mcp = LoadedModManager.RunningMods
                        .FirstOrDefault(m => m.PackageId == pid);
                    return new RequiredModInfo(pid, mcp?.Name ?? pid);
                })
                .ToList();
        }

        /// <summary>
        /// Generate a default defName for a pawn. Public so the dialog can pre-populate.
        /// </summary>
        public static string GenerateDefaultDefName(Pawn pawn, PawnTemplateMode mode)
        {
            return GenerateDefName(pawn, mode);
        }

        public static string ExportToString(Pawn pawn, PawnTemplateMode mode,
            ExportMetadata metadata = null)
        {
            if (pawn == null)
            {
                ModLog.Error("[PawnPortability] Cannot export: pawn is null");
                return null;
            }

            try
            {
                bool logging = PawnPortabilitySettings.LoggingEnabled;
                var collectedPackageIds = new HashSet<string>();

                XmlDocument doc = new XmlDocument();
                XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(declaration);

                XmlElement root = doc.CreateElement(
                    "MSSFP.PawnPortability.Defs.PawnTemplateDef");
                doc.AppendChild(root);

                // defName
                string defName = metadata?.DefName ?? GenerateDefName(pawn, mode);
                AppendElement(doc, root, "defName", defName);
                AppendElement(doc, root, "label", pawn.LabelShort);
                AppendElement(doc, root, "description",
                    $"Exported {mode.ToString().ToLower()} of {pawn.LabelShort}.");

                AppendElement(doc, root, PawnTemplateXmlTags.SchemaVersion, "1");
                AppendElement(doc, root, PawnTemplateXmlTags.Mode, mode.ToString());

                // Character metadata
                string originSeries = metadata?.OriginSeries ?? "";
                string narrativeNotes = metadata?.NarrativeNotes ?? "";
                if (string.IsNullOrEmpty(originSeries) && string.IsNullOrEmpty(narrativeNotes))
                    root.AppendChild(doc.CreateComment(
                        " Character metadata — edit manually after export "));
                AppendElement(doc, root, PawnTemplateXmlTags.OriginSeries, originSeries);
                XmlElement tagsEl = doc.CreateElement(PawnTemplateXmlTags.Tags);
                root.AppendChild(tagsEl);
                AppendElement(doc, root, PawnTemplateXmlTags.NarrativeNotes, narrativeNotes);

                // KindDef
                if (pawn.kindDef != null)
                    AppendDefElement(doc, root, PawnTemplateXmlTags.KindDef,
                        pawn.kindDef, collectedPackageIds);

                // Gender
                AppendElement(doc, root, PawnTemplateXmlTags.Gender, pawn.gender.ToString());

                // Name
                if (pawn.Name is NameTriple nameTriple)
                {
                    XmlElement nameEl = doc.CreateElement(PawnTemplateXmlTags.Name);
                    AppendElement(doc, nameEl, PawnTemplateXmlTags.First, nameTriple.First);
                    AppendElement(doc, nameEl, PawnTemplateXmlTags.Nick, nameTriple.Nick);
                    AppendElement(doc, nameEl, PawnTemplateXmlTags.Last, nameTriple.Last);
                    root.AppendChild(nameEl);
                }

                // Age
                if (pawn.ageTracker != null)
                {
                    XmlElement ageEl = doc.CreateElement(PawnTemplateXmlTags.Age);
                    AppendElement(doc, ageEl, PawnTemplateXmlTags.BiologicalAgeTicks,
                        pawn.ageTracker.AgeBiologicalTicks.ToString());
                    if (pawn.ageTracker.AgeChronologicalTicks != pawn.ageTracker.AgeBiologicalTicks)
                        AppendElement(doc, ageEl, PawnTemplateXmlTags.ChronologicalAgeTicks,
                            pawn.ageTracker.AgeChronologicalTicks.ToString());
                    root.AppendChild(ageEl);
                }

                // Story
                ExportStory(doc, root, pawn, collectedPackageIds);

                // Skills
                ExportSkills(doc, root, pawn);

                // Genes
                ExportGenes(doc, root, pawn, collectedPackageIds);

                // Hediffs (Template: identity only)
                ExportHediffs(doc, root, pawn, mode, collectedPackageIds, logging);

                // Equipment
                ExportItems(doc, root, PawnTemplateXmlTags.Equipment,
                    pawn.equipment?.AllEquipmentListForReading?.Cast<Thing>(),
                    collectedPackageIds);

                // Apparel
                ExportItems(doc, root, PawnTemplateXmlTags.Apparel,
                    pawn.apparel?.WornApparel?.Cast<Thing>(),
                    collectedPackageIds);

                // Abilities
                ExportAbilities(doc, root, pawn, collectedPackageIds);

                // Style
                ExportStyle(doc, root, pawn, collectedPackageIds);

                // requiredMods (informational, based on collected packageIds)
                ExportRequiredMods(doc, root, collectedPackageIds);

                return FormatXml(doc);
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to export pawn {pawn?.LabelShort} to string", ex);
                return null;
            }
        }

        // ── Export Sections ──────────────────────────────────────────────

        private static void ExportStory(XmlDocument doc, XmlElement root, Pawn pawn,
            HashSet<string> packageIds)
        {
            if (pawn.story == null) return;

            XmlElement storyEl = doc.CreateElement(PawnTemplateXmlTags.Story);

            if (pawn.story.bodyType != null)
                AppendDefElement(doc, storyEl, PawnTemplateXmlTags.BodyType,
                    pawn.story.bodyType, packageIds);

            if (pawn.story.headType != null)
                AppendDefElement(doc, storyEl, PawnTemplateXmlTags.HeadType,
                    pawn.story.headType, packageIds);

            if (pawn.story.hairDef != null)
                AppendDefElement(doc, storyEl, PawnTemplateXmlTags.HairDef,
                    pawn.story.hairDef, packageIds);

            AppendElement(doc, storyEl, PawnTemplateXmlTags.HairColor,
                ColorToString(pawn.story.HairColor));

            if (pawn.story.skinColorOverride.HasValue)
                AppendElement(doc, storyEl, PawnTemplateXmlTags.SkinColorOverride,
                    ColorToString(pawn.story.skinColorOverride.Value));

            if (pawn.story.Childhood != null)
                AppendDefElement(doc, storyEl, PawnTemplateXmlTags.Childhood,
                    pawn.story.Childhood, packageIds);

            if (pawn.story.Adulthood != null)
                AppendDefElement(doc, storyEl, PawnTemplateXmlTags.Adulthood,
                    pawn.story.Adulthood, packageIds);

            if (pawn.story.birthLastName != null)
                AppendElement(doc, storyEl, PawnTemplateXmlTags.BirthLastName,
                    pawn.story.birthLastName);

            if (pawn.story.favoriteColor != null)
            {
                AppendDefElement(doc, storyEl, PawnTemplateXmlTags.FavoriteColor,
                    pawn.story.favoriteColor, packageIds);
            }

            // Traits
            if (pawn.story.traits?.allTraits?.Count > 0)
            {
                XmlElement traitsEl = doc.CreateElement(PawnTemplateXmlTags.Traits);
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    XmlElement li = doc.CreateElement(PawnTemplateXmlTags.Li);
                    AppendDefElement(doc, li, PawnTemplateXmlTags.Def, trait.def, packageIds);
                    AppendElement(doc, li, PawnTemplateXmlTags.Degree, trait.Degree.ToString());
                    traitsEl.AppendChild(li);
                }
                storyEl.AppendChild(traitsEl);
            }

            root.AppendChild(storyEl);
        }

        private static void ExportSkills(XmlDocument doc, XmlElement root, Pawn pawn)
        {
            if (pawn.skills?.skills == null) return;

            XmlElement skillsEl = doc.CreateElement(PawnTemplateXmlTags.Skills);
            foreach (SkillRecord rec in pawn.skills.skills)
            {
                XmlElement li = doc.CreateElement(PawnTemplateXmlTags.Li);
                AppendElement(doc, li, PawnTemplateXmlTags.Def, rec.def.defName);
                AppendElement(doc, li, PawnTemplateXmlTags.Level, rec.levelInt.ToString());
                if (rec.passion != Passion.None)
                    AppendElement(doc, li, PawnTemplateXmlTags.Passion, rec.passion.ToString());
                skillsEl.AppendChild(li);
            }
            root.AppendChild(skillsEl);
        }

        private static void ExportGenes(XmlDocument doc, XmlElement root, Pawn pawn,
            HashSet<string> packageIds)
        {
            if (pawn.genes == null) return;

            XmlElement genesEl = doc.CreateElement(PawnTemplateXmlTags.Genes);

            if (pawn.genes.Xenotype != null)
                AppendDefElement(doc, genesEl, PawnTemplateXmlTags.Xenotype,
                    pawn.genes.Xenotype, packageIds);

            if (pawn.genes.Endogenes?.Count > 0)
            {
                XmlElement endoEl = doc.CreateElement(PawnTemplateXmlTags.Endogenes);
                foreach (Gene gene in pawn.genes.Endogenes)
                    AppendDefLi(doc, endoEl, gene.def, packageIds);
                genesEl.AppendChild(endoEl);
            }

            if (pawn.genes.Xenogenes?.Count > 0)
            {
                XmlElement xenoEl = doc.CreateElement(PawnTemplateXmlTags.Xenogenes);
                foreach (Gene gene in pawn.genes.Xenogenes)
                    AppendDefLi(doc, xenoEl, gene.def, packageIds);
                genesEl.AppendChild(xenoEl);
            }

            root.AppendChild(genesEl);
        }

        private static void ExportHediffs(XmlDocument doc, XmlElement root, Pawn pawn,
            PawnTemplateMode mode, HashSet<string> packageIds, bool logging)
        {
            if (pawn.health?.hediffSet == null) return;

            List<Hediff> allHediffs = pawn.health.hediffSet.hediffs.ToList();

            if (logging)
            {
                ModLog.Log($"[PawnPortability] Hediff export diagnostics for {pawn.LabelShort} " +
                           $"(mode: {mode}, total hediffs: {allHediffs.Count}):");
                foreach (Hediff h in allHediffs)
                {
                    string partLabel = h.Part != null ? $" on {h.Part.Label}" : " (whole body)";
                    ModLog.Log($"[PawnPortability]   {h.def.defName}{partLabel} — " +
                               $"isBad={h.def.isBad}, IsPermanent={h.IsPermanent()}, " +
                               $"class={h.GetType().Name}, severity={h.Severity:F2}");
                }
            }

            List<Hediff> hediffsToExport;
            if (mode == PawnTemplateMode.Template)
            {
                // Template: identity hediffs only
                // Include where def.isBad == false (implants, bionics) OR permanent == true (scars)
                hediffsToExport = allHediffs
                    .Where(h => !h.def.isBad || h.IsPermanent())
                    .ToList();

                if (logging)
                    ModLog.Log($"[PawnPortability] Template filter: {hediffsToExport.Count}/{allHediffs.Count} hediffs passed");
            }
            else
            {
                hediffsToExport = allHediffs;
            }

            if (hediffsToExport.Count == 0) return;

            XmlElement hediffsEl = doc.CreateElement(PawnTemplateXmlTags.Hediffs);
            foreach (Hediff hediff in hediffsToExport)
            {
                XmlElement li = doc.CreateElement(PawnTemplateXmlTags.Li);
                AppendDefElement(doc, li, PawnTemplateXmlTags.Def, hediff.def, packageIds);

                if (hediff.Severity > 0)
                    AppendElement(doc, li, PawnTemplateXmlTags.Severity,
                        hediff.Severity.ToString("F2"));

                if (hediff.Part != null)
                {
                    AppendDefElement(doc, li, PawnTemplateXmlTags.BodyPart,
                        hediff.Part.def, packageIds);
                    AppendElement(doc, li, PawnTemplateXmlTags.BodyPartLabel, hediff.Part.Label);
                }

                if (hediff.IsPermanent())
                    AppendElement(doc, li, PawnTemplateXmlTags.Permanent, "true");

                hediffsEl.AppendChild(li);

                if (logging)
                    ModLog.Log(
                        $"[PawnPortability] Exporting hediff: {hediff.def.defName}" +
                        (hediff.Part != null ? $" on {hediff.Part.Label}" : ""));
            }
            root.AppendChild(hediffsEl);
        }

        private static void ExportItems(XmlDocument doc, XmlElement root, string tagName,
            IEnumerable<Thing> items, HashSet<string> packageIds)
        {
            if (items == null) return;
            List<Thing> itemList = items.ToList();
            if (itemList.Count == 0) return;

            XmlElement listEl = doc.CreateElement(tagName);
            foreach (Thing thing in itemList)
            {
                XmlElement li = doc.CreateElement(PawnTemplateXmlTags.Li);
                AppendDefElement(doc, li, PawnTemplateXmlTags.Def, thing.def, packageIds);

                if (thing.Stuff != null)
                    AppendDefElement(doc, li, PawnTemplateXmlTags.Stuff, thing.Stuff, packageIds);

                if (thing.TryGetComp<CompQuality>() is CompQuality qual)
                    AppendElement(doc, li, PawnTemplateXmlTags.Quality,
                        qual.Quality.ToString());

                AppendElement(doc, li, PawnTemplateXmlTags.HitPoints,
                    thing.HitPoints.ToString());

                listEl.AppendChild(li);
            }
            root.AppendChild(listEl);
        }

        private static void ExportAbilities(XmlDocument doc, XmlElement root, Pawn pawn,
            HashSet<string> packageIds)
        {
            if (pawn.abilities?.abilities == null || pawn.abilities.abilities.Count == 0) return;

            XmlElement abilitiesEl = doc.CreateElement(PawnTemplateXmlTags.Abilities);
            foreach (Ability ability in pawn.abilities.abilities)
                AppendDefLi(doc, abilitiesEl, ability.def, packageIds);
            root.AppendChild(abilitiesEl);
        }

        private static void ExportStyle(XmlDocument doc, XmlElement root, Pawn pawn,
            HashSet<string> packageIds)
        {
            if (pawn.style == null) return;

            XmlElement styleEl = doc.CreateElement(PawnTemplateXmlTags.Style);

            if (pawn.style.beardDef != null)
                AppendDefElement(doc, styleEl, PawnTemplateXmlTags.BeardDef,
                    pawn.style.beardDef, packageIds);

            if (pawn.style.FaceTattoo != null)
                AppendDefElement(doc, styleEl, PawnTemplateXmlTags.FaceTattoo,
                    pawn.style.FaceTattoo, packageIds);

            if (pawn.style.BodyTattoo != null)
                AppendDefElement(doc, styleEl, PawnTemplateXmlTags.BodyTattoo,
                    pawn.style.BodyTattoo, packageIds);

            root.AppendChild(styleEl);
        }

        private static void ExportRequiredMods(XmlDocument doc, XmlElement root,
            HashSet<string> packageIds)
        {
            // Remove core — it's always present
            packageIds.Remove(CorePackageId);
            if (packageIds.Count == 0) return;

            XmlElement modsEl = doc.CreateElement(PawnTemplateXmlTags.RequiredMods);
            foreach (string pid in packageIds.OrderBy(p => p))
            {
                XmlElement li = doc.CreateElement(PawnTemplateXmlTags.Li);
                AppendElement(doc, li, PawnTemplateXmlTags.PackageId, pid);

                // Try to find a human-readable name
                ModContentPack mcp = LoadedModManager.RunningMods
                    .FirstOrDefault(m => m.PackageId == pid);
                AppendElement(doc, li, "name", mcp?.Name ?? pid);

                modsEl.AppendChild(li);
            }
            // Insert after mode, before identity fields
            XmlNode insertBefore = root.SelectSingleNode(PawnTemplateXmlTags.OriginSeries)
                                   ?? root.FirstChild;
            root.InsertBefore(modsEl, insertBefore);
        }

        // ── XML Helpers ──────────────────────────────────────────────────

        private static void AppendElement(XmlDocument doc, XmlElement parent,
            string name, string value)
        {
            XmlElement el = doc.CreateElement(name);
            el.InnerText = value ?? "";
            parent.AppendChild(el);
        }

        private static void AppendDefElement(XmlDocument doc, XmlElement parent,
            string name, Def def, HashSet<string> packageIds)
        {
            XmlElement el = doc.CreateElement(name);
            el.InnerText = def.defName;

            string packageId = GetPackageId(def);
            if (packageId != null && packageId != CorePackageId)
            {
                el.SetAttribute(PawnTemplateXmlTags.MayRequire, packageId);
                packageIds.Add(packageId);
            }

            parent.AppendChild(el);
        }

        private static void AppendDefLi(XmlDocument doc, XmlElement parent,
            Def def, HashSet<string> packageIds)
        {
            XmlElement li = doc.CreateElement(PawnTemplateXmlTags.Li);
            li.InnerText = def.defName;

            string packageId = GetPackageId(def);
            if (packageId != null && packageId != CorePackageId)
            {
                li.SetAttribute(PawnTemplateXmlTags.MayRequire, packageId);
                packageIds.Add(packageId);
            }

            parent.AppendChild(li);
        }

        private static string GetPackageId(Def def)
        {
            return def?.modContentPack?.PackageId;
        }

        private static string GenerateDefName(Pawn pawn, PawnTemplateMode mode)
        {
            string firstName = "Unknown";
            if (pawn.Name is NameTriple nt && !string.IsNullOrEmpty(nt.First))
                firstName = SafeDefNameChars.Replace(nt.First, "");

            if (string.IsNullOrEmpty(firstName))
                firstName = "Unknown";

            string baseName = mode == PawnTemplateMode.Template
                ? $"MSSFP_Pawn_{firstName}"
                : $"MSSFP_Snapshot_{firstName}_{GenTicks.TicksAbs}";

            // Check for collision
            if (DefDatabase<PawnTemplateDef>.GetNamedSilentFail(baseName) != null)
            {
                baseName = $"{baseName}_{GenTicks.TicksAbs}";
                ModLog.Warn($"[PawnPortability] defName collision, using: {baseName}");
            }

            return baseName;
        }

        private static string ColorToString(UnityEngine.Color color)
        {
            return $"({color.r:F3}, {color.g:F3}, {color.b:F3})";
        }

        private static string FormatXml(XmlDocument doc)
        {
            using (var sw = new StringWriter())
            using (var xw = XmlWriter.Create(sw, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineOnAttributes = false,
                OmitXmlDeclaration = false
            }))
            {
                doc.WriteTo(xw);
                xw.Flush();
                return sw.ToString();
            }
        }
    }
}

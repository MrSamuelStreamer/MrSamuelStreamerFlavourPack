using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSSFP.PawnPortability.Defs;
using MSSFP.PawnPortability.Import;
using Verse;

namespace MSSFP.PawnPortability
{
    /// <summary>
    /// Holds PawnTemplateDef instances loaded at runtime from the user's ExportedPawns folder.
    /// These are kept separate from the DefDatabase so they don't interfere with the normal
    /// def loading pipeline. Use PawnPortability.AllDefsIncludingUser to access both pools.
    /// </summary>
    public static class UserPawnTemplateRegistry
    {
        private static readonly List<PawnTemplateDef> _userDefs = new();

        public static IReadOnlyList<PawnTemplateDef> UserDefs => _userDefs;

        public static int Count => _userDefs.Count;

        public static string ExportedPawnsDir =>
            Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedPawns");

        /// <summary>
        /// Scans ExportedPawns/ and loads all valid template XML files.
        /// Safe to call multiple times — clears and rebuilds the list each time.
        ///
        /// Mirrors vanilla's two-stage def load: every file is parsed first, then the
        /// cross-reference queue is flushed once for the whole batch, then each def resolves
        /// its references. Parsing alone leaves all Def fields null (DirectXmlToObject defers
        /// them to DirectXmlCrossRefLoader), so ResolveReferences must not run until after
        /// the flush or the null-filtering will strip the entire loadout.
        /// </summary>
        public static void LoadAll()
        {
            _userDefs.Clear();

            if (!Directory.Exists(ExportedPawnsDir))
            {
                ModLog.Debug($"[PawnPortability] ExportedPawns directory not found at {ExportedPawnsDir}, skipping user template load");
                return;
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(ExportedPawnsDir, "*.xml");
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to enumerate ExportedPawns directory", ex);
                return;
            }

            ModLog.Debug($"[PawnPortability] Scanning {files.Length} file(s) in {ExportedPawnsDir}");

            // Stage 1 — parse. Every accepted def has registered wanted cross-refs but its
            // Def fields are still null. Only string-based validation is valid at this point.
            List<PawnTemplateDef> staged = new();
            foreach (string filePath in files)
            {
                PawnTemplateDef def = TryParse(filePath, staged);
                if (def != null)
                    staged.Add(def);
            }

            if (staged.Count == 0)
            {
                ModLog.Debug("[PawnPortability] Loaded 0 user template(s)");
                return;
            }

            // Stage 2 — flush the cross-reference queue once for the whole batch. Note this
            // queue is process-wide static state; the flush also resolves anything another
            // caller left pending. In practice it is empty outside of def load.
            // FailMode.Silent because MayRequire-guarded refs are skipped by the resolver
            // itself, and genuine misses are reported with better context by
            // PawnTemplateDef.ResolveReferences below.
            try
            {
                DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.Silent);
            }
            catch (Exception ex)
            {
                ModLog.Error("[PawnPortability] Failed to resolve cross-references for user templates", ex);
                return;
            }

            // Stage 3 — resolve references per def. A def that throws is dropped, not fatal
            // to the rest of the batch.
            foreach (PawnTemplateDef def in staged)
            {
                try
                {
                    def.ResolveReferences();
                    _userDefs.Add(def);
                    ModLog.Debug($"[PawnPortability] Loaded user template: {def.defName} ({def.label ?? def.defName})");
                }
                catch (Exception ex)
                {
                    ModLog.Error($"[PawnPortability] Failed to resolve references for user template '{def.defName}'", ex);
                }
            }

            ModLog.Debug($"[PawnPortability] Loaded {_userDefs.Count} user template(s)");
        }

        /// <summary>Clears the registry and reloads from disk.</summary>
        public static void Refresh() => LoadAll();

        /// <summary>
        /// Parses one file and applies the identity checks that are valid before cross-refs
        /// resolve (all of them compare strings). Returns null if the file should be skipped.
        /// </summary>
        private static PawnTemplateDef TryParse(string filePath, List<PawnTemplateDef> staged)
        {
            try
            {
                PawnTemplateDef def = PawnTemplateXmlReader.ParseFromFile(filePath);
                if (def == null) return null;

                if (string.IsNullOrEmpty(def.defName))
                {
                    ModLog.Warn($"[PawnPortability] Skipping user template with no defName: {Path.GetFileName(filePath)}");
                    return null;
                }

                // Mod-provided templates take priority over user exports with the same defName.
                if (DefDatabase<PawnTemplateDef>.GetNamedSilentFail(def.defName) != null)
                {
                    ModLog.Warn($"[PawnPortability] Skipping '{def.defName}' — defName already exists in DefDatabase (mod-provided template takes priority)");
                    return null;
                }

                // Guard against duplicate files in the same scan.
                if (staged.Any(d => d.defName == def.defName))
                {
                    ModLog.Warn($"[PawnPortability] Skipping duplicate user template '{def.defName}'");
                    return null;
                }

                return def;
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to load user template from {Path.GetFileName(filePath)}", ex);
                return null;
            }
        }
    }
}

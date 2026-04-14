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

            foreach (string filePath in files)
                TryLoad(filePath);

            ModLog.Debug($"[PawnPortability] Loaded {_userDefs.Count} user template(s)");
        }

        /// <summary>Clears the registry and reloads from disk.</summary>
        public static void Refresh() => LoadAll();

        private static void TryLoad(string filePath)
        {
            try
            {
                PawnTemplateDef def = PawnTemplateXmlReader.ReadFromFile(filePath);
                if (def == null) return;

                if (string.IsNullOrEmpty(def.defName))
                {
                    ModLog.Warn($"[PawnPortability] Skipping user template with no defName: {Path.GetFileName(filePath)}");
                    return;
                }

                // Mod-provided templates take priority over user exports with the same defName.
                if (DefDatabase<PawnTemplateDef>.GetNamedSilentFail(def.defName) != null)
                {
                    ModLog.Warn($"[PawnPortability] Skipping '{def.defName}' — defName already exists in DefDatabase (mod-provided template takes priority)");
                    return;
                }

                // Guard against duplicate files in the same scan.
                if (_userDefs.Any(d => d.defName == def.defName))
                {
                    ModLog.Warn($"[PawnPortability] Skipping duplicate user template '{def.defName}'");
                    return;
                }

                _userDefs.Add(def);
                ModLog.Debug($"[PawnPortability] Loaded user template: {def.defName} ({def.label ?? def.defName})");
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to load user template from {Path.GetFileName(filePath)}", ex);
            }
        }
    }
}

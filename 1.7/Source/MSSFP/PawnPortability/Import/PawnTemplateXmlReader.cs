using System;
using System.IO;
using System.Xml;
using MSSFP.PawnPortability.Defs;
using Verse;

namespace MSSFP.PawnPortability.Import
{
    /// <summary>
    /// Parses an exported PawnTemplateDef XML file into a live PawnTemplateDef instance.
    /// Uses RimWorld's own DirectXmlToObject so field mapping, Def resolution, MayRequire
    /// attribute handling, and colour/enum parsing all work identically to the normal load path.
    /// </summary>
    internal static class PawnTemplateXmlReader
    {
        /// <summary>
        /// Parses the file into a PawnTemplateDef. Non-Def fields (strings, enums, colours,
        /// IntVec3, nested objects) are populated immediately.
        ///
        /// Def-typed fields are NOT populated here. DirectXmlToObject hands every Def field to
        /// DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef and leaves the field null; the
        /// values only appear once DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences()
        /// runs. Vanilla def loading does this as a separate stage after parsing every file.
        ///
        /// Callers MUST therefore resolve cross-references and then call def.ResolveReferences()
        /// before using the result — otherwise every Def is null and PawnTemplateDef's
        /// null-filtering will silently strip the entire loadout.
        /// </summary>
        internal static PawnTemplateDef ParseFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ModLog.Warn($"[PawnPortability] File not found: {filePath}");
                return null;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlNode root = doc.DocumentElement;
                if (root == null)
                {
                    ModLog.Warn($"[PawnPortability] No root element in: {filePath}");
                    return null;
                }

                // DirectXmlToObject maps child element names to PawnTemplateDef field names.
                // The root element name (the full type name) is irrelevant — only children matter.
                // doPostLoad:true calls def.PostLoad() automatically after construction.
                PawnTemplateDef def = DirectXmlToObject.ObjectFromXml<PawnTemplateDef>(root, doPostLoad: true);
                if (def == null)
                    ModLog.Warn($"[PawnPortability] ObjectFromXml returned null for: {filePath}");

                return def;
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to read pawn template from {filePath}", ex);
                return null;
            }
        }

        /// <summary>
        /// Reads a single template file and returns a fully resolved def.
        ///
        /// Convenience wrapper for one-off imports. When loading several files prefer staging
        /// them with ParseFromFile and flushing the cross-reference queue once for the whole
        /// batch — see UserPawnTemplateRegistry.LoadAll.
        /// </summary>
        public static PawnTemplateDef ReadFromFile(string filePath)
        {
            PawnTemplateDef def = ParseFromFile(filePath);
            if (def == null) return null;

            try
            {
                // Fills in every Def field queued during parsing. FailMode.Silent because refs
                // guarded by MayRequire are skipped by the resolver itself, and genuine misses
                // are already reported with far better context by PawnTemplateDef's own
                // null-filtering in ResolveReferences.
                DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.Silent);

                // Resolves cross-def lookups and applies null-filtering for any defs that
                // belong to inactive mods.
                def.ResolveReferences();
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to resolve pawn template from {filePath}", ex);
                return null;
            }

            return def;
        }
    }
}

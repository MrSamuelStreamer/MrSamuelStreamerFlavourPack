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
        public static PawnTemplateDef ReadFromFile(string filePath)
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
                {
                    ModLog.Warn($"[PawnPortability] ObjectFromXml returned null for: {filePath}");
                    return null;
                }

                // ResolveReferences resolves cross-def lookups and applies null-filtering
                // for any defs that belong to inactive mods.
                def.ResolveReferences();

                return def;
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to read pawn template from {filePath}", ex);
                return null;
            }
        }
    }
}

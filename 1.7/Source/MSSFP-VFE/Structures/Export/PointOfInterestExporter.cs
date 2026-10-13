using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using KCSG;
using MSSFP.PawnPortability.Defs;
using MSSFP.PawnPortability.Export;
using RimWorld;
using Verse;
using StructureLayoutDef = KCSG.StructureLayoutDef;

namespace MSSFP.VFE.Structures.Export;

/// <summary>
/// Wraps KCSG's structure export so one call produces a single ready-to-ship XML file: the layout,
/// its symbols, the viewer-structure mod extension, and the pawns and corpses exported through the
/// pawn exporter. Humanlike pawns and corpses are pulled out of KCSG's cell list first, so they are
/// never duplicated as KCSG pawn-kind symbols. Animals and mechs stay with KCSG.
/// </summary>
public static class PointOfInterestExporter
{
    private const string ExtensionClass = "MSSFP.ModExtensions.StructureDefModExtension";
    private static readonly Regex BadDefNameChars = new("[^A-Za-z0-9_]");
    private static readonly Regex ValidDefName = new("^[A-Za-z][A-Za-z0-9_]*$");

    private sealed class Capture
    {
        public Pawn Pawn;
        public IntVec3 Offset;
        public bool Dead;
        public bool Rotten;
        public string DefName;
    }

    public static bool IsValidDefName(string defName) => !string.IsNullOrEmpty(defName) && ValidDefName.IsMatch(defName);

    public static PoiExportResult Export(Map map, List<IntVec3> cells, PoiExportOptions options)
    {
        if (!IsValidDefName(options.defName))
            throw new InvalidOperationException("defName must start with a letter and use only letters, digits and underscores.");
        if (cells.NullOrEmpty())
            throw new InvalidOperationException("No cells selected.");

        var result = new PoiExportResult();
        CellRect bounds = CellRect.FromCellList(cells);
        List<Capture> captures;
        string layoutXml;
        List<string> symbolXmls;

        try
        {
            SetKcsgState(map, cells, options);
            captures = ExtractPawns(options, bounds);
            Dialog_ExportWindow.exportedSymbolsDef = ExportUtils.CreateSymbolIfNeeded(null);
            layoutXml = ExportUtils.CreateStructureDef(map, null).ToXMLString();
            symbolXmls = BuildSymbolXml();
        }
        finally
        {
            ClearKcsgState();
        }

        XmlDocument doc = Assemble(map, options, bounds, captures, layoutXml, symbolXmls, result);
        result.Path = WriteToDesktop(doc, options.defName);
        return result;
    }

    // ── KCSG state ────────────────────────────────────────────────────────────

    private static void SetKcsgState(Map map, List<IntVec3> cells, PoiExportOptions o)
    {
        Dialog_ExportWindow.cells = cells.OrderBy(c => c.z).ToList();
        CreateKcsgSymbols();
        Dialog_ExportWindow.pairsCellThingList = ExportUtils.FillCellThingsList(map);

        Dialog_ExportWindow.defName = o.defName;
        Dialog_ExportWindow.tags = new HashSet<string>();
        Dialog_ExportWindow.exportNatural = o.exportNatural;
        Dialog_ExportWindow.exportFilth = o.exportFilth;
        Dialog_ExportWindow.exportPlant = o.exportPlant;
        Dialog_ExportWindow.forceGenerateRoof = o.forceGenerateRoof;
        Dialog_ExportWindow.spawnConduits = o.spawnConduits;
        Dialog_ExportWindow.isStorage = false;
        Dialog_ExportWindow.needRoofClearance = false;
        Dialog_ExportWindow.randomizeWallStuffAtGen = false;
        Dialog_ExportWindow.saveFuel = false;
        Dialog_ExportWindow.savePower = false;
        // KCSG's layout generation rotates by this flag; pawn offsets assume no rotation.
        Dialog_ExportWindow.randomRotation = false;
    }

    /// <summary>KCSG.StartupActions is internal, so call CreateSymbols by reflection, as its own dialog does.</summary>
    private static void CreateKcsgSymbols()
    {
        System.Reflection.MethodInfo method = typeof(ExportUtils)
            .Assembly.GetType("KCSG.StartupActions")
            ?.GetMethod("CreateSymbols", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (method == null)
            throw new InvalidOperationException("KCSG.StartupActions.CreateSymbols not found; the KCSG version may have changed.");
        method.Invoke(null, null);
    }

    private static void ClearKcsgState()
    {
        Dialog_ExportWindow.cells = new List<IntVec3>();
        Dialog_ExportWindow.pairsCellThingList = new Dictionary<IntVec3, List<Thing>>();
        Dialog_ExportWindow.exportedSymbolsDef = new List<SymbolDef>();
        Dialog_ExportWindow.defName = "";
    }

    /// <summary>Removes humanlike pawns and corpses from KCSG's list; returns those to export.</summary>
    private static List<Capture> ExtractPawns(PoiExportOptions o, CellRect bounds)
    {
        var captures = new List<Capture>();
        foreach (KeyValuePair<IntVec3, List<Thing>> pair in Dialog_ExportWindow.pairsCellThingList)
        {
            IntVec3 offset = new(pair.Key.x - bounds.minX, 0, pair.Key.z - bounds.minZ);
            foreach (Thing thing in pair.Value.ToList())
            {
                Capture capture = ToCapture(thing, offset, o);
                if (capture == null && !IsHumanlike(thing))
                    continue;

                pair.Value.Remove(thing);
                if (capture != null)
                    captures.Add(capture);
            }
        }

        for (int i = 0; i < captures.Count; i++)
            captures[i].DefName = $"{o.defName}_{BadDefNameChars.Replace(captures[i].Pawn.LabelShort, "")}_{i + 1}";
        return captures;
    }

    private static bool IsHumanlike(Thing thing) =>
        (thing is Pawn p && p.RaceProps.Humanlike) || (thing is Corpse c && c.InnerPawn?.RaceProps.Humanlike == true);

    private static Capture ToCapture(Thing thing, IntVec3 offset, PoiExportOptions o)
    {
        if (thing is Pawn pawn && pawn.RaceProps.Humanlike && o.exportPawns)
            return new Capture { Pawn = pawn, Offset = offset };

        if (thing is Corpse corpse && corpse.InnerPawn?.RaceProps.Humanlike == true && o.exportCorpses)
        {
            CompRottable rot = corpse.GetComp<CompRottable>();
            return new Capture
            {
                Pawn = corpse.InnerPawn,
                Offset = offset,
                Dead = true,
                Rotten = rot != null && rot.Stage != RotStage.Fresh,
            };
        }

        return null;
    }

    /// <summary>Same symbol trimming as KCSG's "Copy symbols" button.</summary>
    private static List<string> BuildSymbolXml()
    {
        var symbols = new List<SymbolDef>();
        foreach (SymbolDef symbol in Dialog_ExportWindow.exportedSymbolsDef)
        {
            SymbolDef toAdd = symbol;
            if (HasRotationSuffix(symbol.defName))
            {
                string trimmed = ExportUtils.TrimSymbolName(symbol.defName);
                if (DefDatabase<SymbolDef>.GetNamedSilentFail(trimmed) != null)
                    continue;
                symbol.defName = trimmed;
            }

            if (!symbols.Any(s => s.defName == toAdd.defName))
                symbols.Add(toAdd);
        }

        return symbols.Select(s => s.ToXMLString()).ToList();
    }

    private static bool HasRotationSuffix(string name) =>
        name.Contains("_North") || name.Contains("_South") || name.Contains("_East") || name.Contains("_West");

    // ── XML assembly ──────────────────────────────────────────────────────────

    private static XmlDocument Assemble(
        Map map,
        PoiExportOptions o,
        CellRect bounds,
        List<Capture> captures,
        string layoutXml,
        List<string> symbolXmls,
        PoiExportResult result
    )
    {
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
        XmlElement defs = doc.CreateElement("Defs");
        doc.AppendChild(defs);

        XmlElement layoutEl = ImportElement(doc, layoutXml);
        var pawnDefs = new List<XmlElement>();
        var entries = new List<Capture>();
        var mods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Capture capture in captures)
        {
            XmlElement pawnEl = ExportPawn(doc, capture, o, mods, result);
            if (pawnEl == null)
                continue;

            pawnDefs.Add(pawnEl);
            entries.Add(capture);
            if (capture.Dead)
                result.CorpseCount++;
            else
                result.PawnCount++;
        }

        AddModRequirements(doc, layoutEl, mods);
        AddExtension(doc, layoutEl, o, map, bounds, entries);

        defs.AppendChild(layoutEl);
        foreach (string symbolXml in symbolXmls)
            defs.AppendChild(ImportElement(doc, symbolXml));
        foreach (XmlElement pawnEl in pawnDefs)
            defs.AppendChild(pawnEl);
        return doc;
    }

    private static XmlElement ExportPawn(
        XmlDocument doc,
        Capture capture,
        PoiExportOptions o,
        HashSet<string> mods,
        PoiExportResult result
    )
    {
        try
        {
            PawnTemplateMode mode = o.snapshotMode ? PawnTemplateMode.Snapshot : PawnTemplateMode.Template;
            string xml = PawnExporter.ExportToString(capture.Pawn, mode, new ExportMetadata { DefName = capture.DefName });
            if (xml == null)
            {
                result.Warnings.Add($"{capture.Pawn.LabelShort}: exporter returned nothing, skipped");
                return null;
            }

            foreach (RequiredModInfo mod in PawnExporter.CollectRequiredMods(capture.Pawn))
                mods.Add(mod.PackageId);
            return ImportElement(doc, xml);
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"{capture.Pawn.LabelShort}: export failed ({ex.Message}), skipped");
            return null;
        }
    }

    private static void AddModRequirements(XmlDocument doc, XmlElement layoutEl, HashSet<string> extraMods)
    {
        if (extraMods.Count == 0)
            return;

        XmlElement req = layoutEl["modRequirements"];
        if (req == null)
        {
            req = doc.CreateElement("modRequirements");
            layoutEl.AppendChild(req);
        }

        var existing = new HashSet<string>(
            req.ChildNodes.Cast<XmlNode>().Select(n => n.InnerText.Trim()),
            StringComparer.OrdinalIgnoreCase
        );
        foreach (string mod in extraMods.Where(m => !string.IsNullOrEmpty(m) && !existing.Contains(m)))
            AppendText(doc, req, "li", mod.ToLowerInvariant());
    }

    private static void AddExtension(
        XmlDocument doc,
        XmlElement layoutEl,
        PoiExportOptions o,
        Map map,
        CellRect bounds,
        List<Capture> entries
    )
    {
        XmlElement mods = layoutEl["modExtensions"] ?? layoutEl.AppendChild(doc.CreateElement("modExtensions")) as XmlElement;
        XmlElement ext = doc.CreateElement("li");
        ext.SetAttribute("Class", ExtensionClass);
        mods.AppendChild(ext);

        if (!string.IsNullOrWhiteSpace(o.author))
            AppendText(doc, ext, "author", o.author.Trim());
        AppendText(doc, ext, "standalone", ToXmlBool(o.standalone));
        AppendText(doc, ext, "doLoot", ToXmlBool(o.doLoot));
        AppendText(doc, ext, "anyHostile", ToXmlBool(o.anyHostile));
        AppendText(doc, ext, "excludeFromRandomGen", ToXmlBool(o.excludeFromRandomGen));
        AppendText(doc, ext, "size", $"({bounds.Width}, {bounds.Height})");
        if (o.restrictToCurrentBiome)
            AppendText(doc, ext, "biome", map.Biome.defName);

        if (entries.Count == 0)
            return;

        XmlElement list = doc.CreateElement("pawnTemplates");
        ext.AppendChild(list);
        foreach (Capture entry in entries)
        {
            XmlElement li = doc.CreateElement("li");
            AppendText(doc, li, "template", entry.DefName);
            AppendText(doc, li, "offset", $"({entry.Offset.x}, 0, {entry.Offset.z})");
            AppendText(doc, li, "dead", ToXmlBool(entry.Dead));
            AppendText(doc, li, "rotten", ToXmlBool(entry.Rotten));
            list.AppendChild(li);
        }
    }

    private static string ToXmlBool(bool value) => value ? "true" : "false";

    private static void AppendText(XmlDocument doc, XmlElement parent, string name, string value)
    {
        XmlElement el = doc.CreateElement(name);
        el.InnerText = value;
        parent.AppendChild(el);
    }

    private static XmlElement ImportElement(XmlDocument target, string xml)
    {
        var temp = new XmlDocument();
        temp.LoadXml(xml);
        return (XmlElement)target.ImportNode(temp.DocumentElement!, true);
    }

    // ── Output ────────────────────────────────────────────────────────────────

    private static string WriteToDesktop(XmlDocument doc, string defName)
    {
        string path = Path.Combine(ResolveOutputDirectory(), defName + ".xml");
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (XmlWriter writer = XmlWriter.Create(path, settings))
            doc.Save(writer);
        return path;
    }

    private static string ResolveOutputDirectory()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
            return desktop;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string fallback = Path.Combine(home, "Desktop");
        return Directory.Exists(fallback) ? fallback : home;
    }
}

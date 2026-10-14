using System;
using System.Collections.Generic;
using KCSG;
using RimWorld;
using UnityEngine;
using Verse;
using StructureLayoutDef = KCSG.StructureLayoutDef;

namespace MSSFP.VFE.Structures.Export;

/// <summary>Collects export options for a selected area, then writes one XML file to the Desktop.</summary>
public class Dialog_ExportPointOfInterest : Window
{
    private const float RowHeight = 30f;

    private readonly Map map;
    private readonly List<IntVec3> cells;
    private readonly PoiExportOptions options = new();

    public override Vector2 InitialSize => new(520f, 640f);

    public Dialog_ExportPointOfInterest(Map map, List<IntVec3> cells)
    {
        this.map = map;
        this.cells = cells;
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var list = new Listing_Standard();
        list.Begin(new Rect(inRect.x, inRect.y, inRect.width, inRect.height - RowHeight - 10f));

        Text.Font = GameFont.Medium;
        list.Label("Export point of interest");
        Text.Font = GameFont.Small;
        list.Label($"{cells.Count} cells selected");
        list.GapLine();

        options.defName = list.TextEntryLabeled("defName", options.defName);
        options.author = list.TextEntryLabeled("Author", options.author);
        WarnIfDefNameTaken(list);
        list.GapLine();

        list.CheckboxLabeled("Standalone (whole-map structure)", ref options.standalone);
        list.CheckboxLabeled("Scatter loot", ref options.doLoot);
        list.CheckboxLabeled("Occupants hostile", ref options.anyHostile);
        list.CheckboxLabeled("Exclude from random generation", ref options.excludeFromRandomGen);
        list.CheckboxLabeled($"Only generate in {map.Biome.LabelCap}", ref options.restrictToCurrentBiome);
        list.GapLine();

        list.CheckboxLabeled("Export colonists and other humanlike pawns", ref options.exportPawns);
        list.CheckboxLabeled("Export humanlike corpses", ref options.exportCorpses);
        list.CheckboxLabeled("Snapshot pawns (gear, hediffs, inventory)", ref options.snapshotMode);
        list.Label("Unticked pawn or corpse options omit those pawns. Animals and mechs export as normal.");
        list.GapLine();

        list.CheckboxLabeled("Export natural terrain", ref options.exportNatural);
        list.CheckboxLabeled("Export filth", ref options.exportFilth);
        list.CheckboxLabeled("Export plants", ref options.exportPlant);
        list.CheckboxLabeled("Force-generate roof", ref options.forceGenerateRoof);
        list.CheckboxLabeled("Spawn conduits", ref options.spawnConduits);
        list.End();

        DrawButtons(inRect);
    }

    private void WarnIfDefNameTaken(Listing_Standard list)
    {
        if (options.defName.NullOrEmpty())
            return;

        if (!PointOfInterestExporter.IsValidDefName(options.defName))
        {
            list.Label("defName must start with a letter and use only letters, digits and _".Colorize(ColorLibrary.RedReadable));
            return;
        }

        if (DefDatabase<StructureLayoutDef>.GetNamedSilentFail(options.defName) != null)
            list.Label("A structure with this defName already exists.".Colorize(ColorLibrary.Yellow));
    }

    private void DrawButtons(Rect inRect)
    {
        float y = inRect.height - RowHeight;
        if (Widgets.ButtonText(new Rect(0f, y, 160f, RowHeight), "Cancel"))
            Close();

        if (Widgets.ButtonText(new Rect(inRect.width - 160f, y, 160f, RowHeight), "Export to Desktop"))
            RunExport();
    }

    private void RunExport()
    {
        try
        {
            PoiExportResult result = PointOfInterestExporter.Export(map, cells, options);
            foreach (string warning in result.Warnings)
                Log.Warning($"[MSSFP] POI export: {warning}");

            Messages.Message(
                $"Exported {result.PawnCount} pawns, {result.CorpseCount} corpses to {result.Path}",
                MessageTypeDefOf.TaskCompletion,
                false
            );
            Close();
        }
        catch (InvalidOperationException ex)
        {
            Messages.Message(ex.Message, MessageTypeDefOf.RejectInput, false);
        }
        catch (Exception ex)
        {
            Log.Error($"[MSSFP] POI export failed: {ex}");
            Messages.Message("Export failed, see the log.", MessageTypeDefOf.NegativeEvent, false);
        }
    }
}

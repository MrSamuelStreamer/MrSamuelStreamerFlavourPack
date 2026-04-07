using System;
using System.Collections.Generic;
using System.IO;
using MSSFP.PawnPortability.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.PawnPortability.Export
{
    internal class Dialog_ExportPawnTemplate : Window
    {
        private readonly Pawn pawn;
        private readonly PawnTemplateMode mode;
        private readonly List<RequiredModInfo> requiredMods;

        private string defName;
        private string originSeries = "";
        private string narrativeNotes = "";
        private Vector2 modsScrollPosition = Vector2.zero;

        private const float LabelWidth = 110f;
        private const float RowHeight = 30f;
        private const float RowGap = 6f;
        private const float ButtonHeight = 35f;
        private const float ColumnGap = 20f;

        public override Vector2 InitialSize => new Vector2(750f, 450f);

        public Dialog_ExportPawnTemplate(Pawn pawn, PawnTemplateMode mode = PawnTemplateMode.Template)
        {
            this.pawn = pawn;
            this.mode = mode;

            defName = PawnExporter.GenerateDefaultDefName(pawn, mode);
            requiredMods = PawnExporter.CollectRequiredMods(pawn);

            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float leftWidth = (inRect.width - ColumnGap) * 0.6f;
            float rightWidth = inRect.width - leftWidth - ColumnGap;

            Rect leftCol = new Rect(inRect.x, inRect.y, leftWidth, inRect.height);
            Rect rightCol = new Rect(inRect.x + leftWidth + ColumnGap, inRect.y,
                rightWidth, inRect.height);

            DrawFormColumn(leftCol);
            DrawModsColumn(rightCol);
        }

        private void DrawFormColumn(Rect rect)
        {
            float curY = rect.y;

            // Title
            Text.Font = GameFont.Medium;
            string title = $"Export: {pawn.LabelShort}";
            Widgets.Label(new Rect(rect.x, curY, rect.width, 36f), title);
            Text.Font = GameFont.Small;
            curY += 40f;

            // DefName
            Widgets.Label(new Rect(rect.x, curY, LabelWidth, RowHeight), "Def Name:");
            defName = Widgets.TextField(
                new Rect(rect.x + LabelWidth, curY, rect.width - LabelWidth, RowHeight),
                defName);
            curY += RowHeight + RowGap;

            // Origin Series
            Widgets.Label(new Rect(rect.x, curY, LabelWidth, RowHeight), "Origin Series:");
            originSeries = Widgets.TextField(
                new Rect(rect.x + LabelWidth, curY, rect.width - LabelWidth, RowHeight),
                originSeries);
            curY += RowHeight + RowGap;

            // Narrative Notes label
            Widgets.Label(new Rect(rect.x, curY, rect.width, RowHeight), "Narrative Notes:");
            curY += RowHeight;

            // Narrative Notes text area — fill remaining space above buttons
            float notesBottom = rect.yMax - ButtonHeight - RowGap;
            float notesHeight = notesBottom - curY;
            if (notesHeight > 40f)
            {
                narrativeNotes = Widgets.TextArea(
                    new Rect(rect.x, curY, rect.width, notesHeight),
                    narrativeNotes);
            }

            // Buttons
            float buttonY = rect.yMax - ButtonHeight;
            float buttonWidth = (rect.width - RowGap) / 2f;

            if (Widgets.ButtonText(
                    new Rect(rect.x, buttonY, buttonWidth, ButtonHeight),
                    "Cancel"))
            {
                Close();
            }

            if (Widgets.ButtonText(
                    new Rect(rect.x + buttonWidth + RowGap, buttonY, buttonWidth, ButtonHeight),
                    "Export"))
            {
                DoExport();
            }
        }

        private void DrawModsColumn(Rect rect)
        {
            float curY = rect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, curY, rect.width, 36f), "Required Mods");
            Text.Font = GameFont.Small;
            curY += 40f;

            if (requiredMods.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, RowHeight), "Core only");
                GUI.color = Color.white;
                return;
            }

            // Scrollable mod list
            Rect listRect = new Rect(rect.x, curY, rect.width, rect.yMax - curY);
            float viewHeight = requiredMods.Count * (RowHeight + RowGap + 18f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(listRect, ref modsScrollPosition, viewRect);
            float entryY = 0f;
            foreach (RequiredModInfo mod in requiredMods)
            {
                // Mod name (bold-ish via font)
                Widgets.Label(new Rect(4f, entryY, viewRect.width - 8f, 20f), mod.Name);
                entryY += 20f;

                // Package ID (gray, smaller)
                GUI.color = Color.gray;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(12f, entryY, viewRect.width - 16f, 16f), mod.PackageId);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                entryY += 16f + RowGap;
            }
            Widgets.EndScrollView();
        }

        private void DoExport()
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                Messages.Message("DefName cannot be empty.", MessageTypeDefOf.RejectInput);
                return;
            }

            try
            {
                string exportDir = Path.Combine(
                    GenFilePaths.SaveDataFolderPath, "ExportedPawns");

                string sanitizedDefName = defName.Replace(" ", "_");
                string fileName = $"{sanitizedDefName}.xml";
                string filePath = Path.Combine(exportDir, fileName);

                var metadata = new ExportMetadata
                {
                    DefName = defName,
                    OriginSeries = originSeries,
                    NarrativeNotes = narrativeNotes
                };

                bool success = PawnExporter.Export(pawn, filePath, mode, metadata);

                if (success)
                {
                    Messages.Message(
                        $"Exported {pawn.LabelShort} to:\n{filePath}",
                        new LookTargets(pawn),
                        MessageTypeDefOf.PositiveEvent);
                    Close();
                }
                else
                {
                    Messages.Message(
                        $"Failed to export {pawn.LabelShort}.",
                        MessageTypeDefOf.RejectInput);
                }
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Dialog export failed for {pawn.LabelShort}", ex);
                Messages.Message(
                    $"Export error: {ex.Message}",
                    MessageTypeDefOf.RejectInput);
            }
        }
    }
}

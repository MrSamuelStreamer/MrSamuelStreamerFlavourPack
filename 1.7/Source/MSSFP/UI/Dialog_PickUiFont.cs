using System;
using System.Collections.Generic;
using MSSFP.Fonts;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Dialogs;

/// <summary>
/// Searchable list of UI fonts: Vanilla pinned at the top, then every installed system font
/// (see <see cref="FontCatalog"/>). Clicking a row applies that font at once
/// and the window stays open, so fonts can be compared. The list can hold thousands of rows, so
/// only the rows in view are drawn.
/// </summary>
public class Dialog_PickUiFont : Window
{
    private const float RowHeight = 28f;
    private const float SearchHeight = 30f;
    private const float Gap = 8f;

    private readonly record struct Row(string Id, string Label, string Tooltip);

    private readonly List<Row> allRows = [];
    private readonly List<Row> shownRows = [];
    private string search = "";
    private string shownFor = null;
    private Vector2 scrollPos;

    public Dialog_PickUiFont()
    {
        doCloseButton = true;
        doCloseX = true;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
        draggable = true;
        resizeable = true;
        forcePause = false;

        allRows.Add(new Row(UiFont.VanillaId, UiFont.DisplayName(UiFont.VanillaId), null));
        foreach (FontEntry entry in FontCatalog.Entries)
            allRows.Add(new Row(entry.Path, entry.Label, entry.Path));
    }

    public override Vector2 InitialSize => new(520f, 640f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Rect titleRect = new(inRect.x, inRect.y, inRect.width, 36f);
        Widgets.Label(titleRect, "MSS_FP_Settings_UiFont_DialogTitle".Translate());
        Text.Font = GameFont.Small;

        Rect searchRect = new(inRect.x, titleRect.yMax + Gap, inRect.width, SearchHeight);
        search = Widgets.TextField(searchRect, search);
        if (string.IsNullOrEmpty(search))
        {
            GUI.color = Color.gray;
            Widgets.Label(searchRect.ContractedBy(6f, 0f), "MSS_FP_Settings_UiFont_SearchHint".Translate());
            GUI.color = Color.white;
        }
        RefreshShownRows();

        float listTop = searchRect.yMax + Gap;
        Rect outRect = new(inRect.x, listTop, inRect.width, inRect.yMax - listTop - CloseButSize.y - Gap);
        Rect viewRect = new(0f, 0f, outRect.width - 18f, shownRows.Count * RowHeight);

        Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
        int first = Mathf.Max(0, (int)(scrollPos.y / RowHeight));
        int last = Mathf.Min(shownRows.Count - 1, (int)((scrollPos.y + outRect.height) / RowHeight));
        for (int i = first; i <= last; i++)
            DrawRow(new Rect(0f, i * RowHeight, viewRect.width, RowHeight), shownRows[i]);
        Widgets.EndScrollView();

        if (shownRows.Count == 0)
            Widgets.Label(outRect.ContractedBy(6f), "MSS_FP_Settings_UiFont_NoMatches".Translate());
    }

    private void RefreshShownRows()
    {
        if (search == shownFor)
            return;

        shownFor = search;
        shownRows.Clear();
        foreach (Row row in allRows)
        {
            if (string.IsNullOrWhiteSpace(search) || row.Label.Contains(search, StringComparison.OrdinalIgnoreCase))
                shownRows.Add(row);
        }
        scrollPos = Vector2.zero;
    }

    private void DrawRow(Rect rect, Row row)
    {
        if (row.Id == UiFont.Current)
            Widgets.DrawHighlightSelected(rect);
        else
            Widgets.DrawHighlightIfMouseover(rect);

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height), row.Label);
        Text.Anchor = TextAnchor.UpperLeft;

        if (row.Tooltip != null)
            TooltipHandler.TipRegion(rect, row.Tooltip);
        if (Widgets.ButtonInvisible(rect))
            Select(row);
    }

    private static void Select(Row row)
    {
        Settings settings = MSSFPMod.settings;
        if (!UiFont.Apply(row.Id))
        {
            settings.UiFontId = UiFont.VanillaId;
            Messages.Message(
                "MSS_FP_Settings_UiFont_ApplyFailed".Translate(row.Label),
                MessageTypeDefOf.RejectInput,
                historical: false
            );
        }
        else
        {
            settings.UiFontId = row.Id;
        }
        settings.Write();
    }
}

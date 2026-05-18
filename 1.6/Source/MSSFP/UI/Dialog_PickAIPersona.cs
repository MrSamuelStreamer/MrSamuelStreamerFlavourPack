using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps;
using MSSFP.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Dialogs;

/// <summary>
/// Forced modal that asks the player to install an AI persona into a freshly-built
/// Pondering Orb. Cannot be dismissed without picking — <c>forcePause</c> halts time and
/// <c>absorbInputAroundWindow</c> swallows clicks outside the dialog. Closes only via
/// <see cref="CompTrueAICore.SetPersonality"/> on row click.
///
/// Save-during-popup safety: <see cref="CompTrueAICore.PostSpawnSetup"/> re-queues this
/// dialog on load when <c>personaChosen</c> is still false. The pre-rename migration
/// heuristic in <see cref="CompTrueAICore.PostExposeData"/> exempts existing dev-spawn
/// cores that already have a persona.
/// </summary>
public class Dialog_PickAIPersona : Window
{
    private const float MinRowHeight = 64f;
    private const float IconSize = 48f;
    private const float Padding = 8f;
    private const float TitleLineHeight = 22f;
    private const float RowVerticalPadding = 8f;

    private readonly CompTrueAICore comp;
    private Vector2 scrollPos;

    public Dialog_PickAIPersona(CompTrueAICore comp)
    {
        this.comp = comp;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        closeOnCancel = false;
        closeOnClickedOutside = false;
        doCloseButton = false;
        doCloseX = false;
        preventCameraMotion = true;
        draggable = false;
    }

    public override Vector2 InitialSize => new Vector2(560f, 640f);

    public override void DoWindowContents(Rect inRect)
    {
        if (comp == null || comp.parent == null || comp.parent.Destroyed)
        {
            Close(doCloseSound: false);
            return;
        }

        Text.Font = GameFont.Medium;
        Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 36f);
        Widgets.Label(titleRect, "MSSFP_PonderingOrb_PickPersona_Title".Translate());
        Text.Font = GameFont.Small;

        Rect blurbRect = new Rect(inRect.x, titleRect.yMax + 4f, inRect.width, 48f);
        Widgets.Label(blurbRect, "MSSFP_PonderingOrb_PickPersona_Blurb".Translate());

        // Filter out hideFromSelector personas — those are assigned programmatically by a
        // host building/comp (Autodoc, Mr Beans, etc.) and must never be pickable here.
        List<AIPersonalityDef> defs = DefDatabase<AIPersonalityDef>.AllDefsListForReading
            .Where(d => !d.hideFromSelector)
            .ToList();
        if (defs == null || defs.Count == 0)
        {
            // Misconfigured load — no personas defined. Bail with a null persona so the
            // popup doesn't lock the player out of their save. Inspect-string will show
            // "no personality" until dev gizmo intervenes.
            Rect emptyRect = new Rect(inRect.x, blurbRect.yMax + 12f, inRect.width, 32f);
            Widgets.Label(emptyRect, "MSSFP_PonderingOrb_PickPersona_NoneAvailable".Translate());
            Rect okRect = new Rect(inRect.x + inRect.width / 2f - 60f, inRect.yMax - 36f, 120f, 28f);
            if (Widgets.ButtonText(okRect, "OK"))
            {
                // Force-flip personaChosen via the public setter on the comp by passing the
                // first existing AIPersonalityDef IF the database recovers — otherwise we
                // still need to mark chosen so the popup doesn't fire forever.
                comp.personaChosen = true;
                Close(doCloseSound: true);
            }
            return;
        }

        float listTop = blurbRect.yMax + 8f;
        float listHeight = inRect.height - (listTop - inRect.y);
        Rect outRect = new Rect(inRect.x, listTop, inRect.width, listHeight);
        float contentWidth = outRect.width - 18f;

        // Pre-pass: compute per-row heights so long descriptions don't truncate.
        // Must set Tiny font BEFORE CalcHeight — font metrics drive measurement.
        float[] rowHeights = new float[defs.Count];
        float totalHeight = 0f;
        GameFont prevFont = Text.Font;
        bool prevWrap = Text.WordWrap;
        Text.WordWrap = true;
        for (int i = 0; i < defs.Count; i++)
        {
            rowHeights[i] = ComputeRowHeight(defs[i], contentWidth);
            totalHeight += rowHeights[i] + Padding;
        }
        Text.Font = prevFont;
        Text.WordWrap = prevWrap;

        Rect viewRect = new Rect(0f, 0f, contentWidth, totalHeight);

        Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
        float y = 0f;
        for (int i = 0; i < defs.Count; i++)
        {
            float h = rowHeights[i];
            Rect row = new Rect(0f, y, viewRect.width, h);
            DrawPersonaRow(row, defs[i]);
            y += h + Padding;
        }
        Widgets.EndScrollView();
    }

    private static float ComputeRowHeight(AIPersonalityDef def, float contentWidth)
    {
        float descWidth = contentWidth - IconSize - Padding * 3f;
        if (descWidth < 1f)
        {
            return MinRowHeight;
        }
        Text.Font = GameFont.Tiny;
        float descHeight = string.IsNullOrEmpty(def.description)
            ? 0f
            : Text.CalcHeight(def.description, descWidth);
        float needed = RowVerticalPadding + TitleLineHeight + descHeight;
        return Mathf.Max(MinRowHeight, needed);
    }

    private void DrawPersonaRow(Rect rect, AIPersonalityDef def)
    {
        bool hover = Mouse.IsOver(rect);
        Widgets.DrawOptionBackground(rect, hover);

        Rect iconRect = new Rect(
            rect.x + Padding,
            rect.y + Mathf.Max(0f, (MinRowHeight - IconSize) / 2f),
            IconSize,
            IconSize
        );
        if (!string.IsNullOrEmpty(def.iconPath))
        {
            Texture2D tex = ContentFinder<Texture2D>.Get(def.iconPath, false);
            if (tex != null)
            {
                GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit);
            }
        }

        Rect textRect = new Rect(
            iconRect.xMax + Padding,
            rect.y + 4f,
            rect.width - iconRect.width - Padding * 3f,
            rect.height - 8f
        );
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(
            new Rect(textRect.x, textRect.y, textRect.width, TitleLineHeight),
            def.LabelShortOrLabel
        );
        Text.Font = GameFont.Tiny;
        bool prevWrap = Text.WordWrap;
        Text.WordWrap = true;
        GUI.color = new Color(0.8f, 0.8f, 0.8f);
        Widgets.Label(
            new Rect(textRect.x, textRect.y + TitleLineHeight, textRect.width, textRect.height - TitleLineHeight),
            def.description ?? string.Empty
        );
        GUI.color = Color.white;
        Text.WordWrap = prevWrap;
        Text.Font = GameFont.Small;

        if (Widgets.ButtonInvisible(rect))
        {
            comp.SetPersonality(def);
            Close(doCloseSound: true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Custom color-picker window for the holo projector tint gizmo. Vanilla 1.6 does NOT
/// expose <c>Dialog_ColorPicker</c>; only <see cref="Widgets.ColorSelector"/> exists as a
/// reusable widget. This window wraps that widget plus Accept/Cancel buttons.
///
/// Palette source: <c>DefDatabase&lt;ColorDef&gt;</c>. Built once, cached statically.
/// </summary>
/// <remarks>
/// Layout (520×420 inRect):
///   [title 0..30]
///   [preview swatch 36..76]
///   [Widgets.ColorSelector 86..bottom-40]
///   [Cancel / Accept buttons bottom-32..bottom]
/// </remarks>
public class Dialog_HoloTint : Window
{
    private Color current;
    private readonly Action<Color> onAccept;
    private static List<Color> cachedPalette;

    public override Vector2 InitialSize => new Vector2(520f, 420f);

    public Dialog_HoloTint(Color initial, Action<Color> onAccept)
    {
        current = initial;
        this.onAccept = onAccept;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        closeOnCancel = true;
    }

    /// <summary>Distinct colors from every <see cref="ColorDef"/> in the game.</summary>
    private static List<Color> Palette
    {
        get
        {
            if (cachedPalette == null)
            {
                cachedPalette = DefDatabase<ColorDef>.AllDefsListForReading
                    .Select(d => d.color)
                    .Distinct()
                    .ToList();
                // White at front so the default tint is the first option.
                if (cachedPalette.Remove(Color.white))
                    cachedPalette.Insert(0, Color.white);
                else
                    cachedPalette.Insert(0, Color.white);
            }
            return cachedPalette;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "Holo tint");
        Text.Font = GameFont.Small;

        // Preview swatch
        Rect swatch = new Rect(0f, 36f, 40f, 40f);
        Widgets.DrawBoxSolid(swatch, current);
        Widgets.DrawBox(swatch);
        Widgets.Label(new Rect(48f, 42f, inRect.width - 48f, 30f), "Preview");

        // Color grid (vanilla widget — same one Ideo styling uses)
        float gridTop = 86f;
        float gridBottom = inRect.height - 40f;
        Rect selRect = new Rect(0f, gridTop, inRect.width, gridBottom - gridTop);
        Widgets.ColorSelector(selRect, ref current, Palette, out float _, null, 22, 2, null);

        // Buttons
        float btnY = inRect.height - 32f;
        if (Widgets.ButtonText(new Rect(0f, btnY, 120f, 28f), "Cancel"))
        {
            Close();
        }
        if (Widgets.ButtonText(new Rect(inRect.width - 120f, btnY, 120f, 28f), "Accept"))
        {
            onAccept?.Invoke(current);
            Close();
        }
    }
}

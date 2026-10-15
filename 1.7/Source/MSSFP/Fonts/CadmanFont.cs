using System;
using System.IO;
using System.Reflection;
using MSSFP.Utils;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace MSSFP.Fonts;

/// <summary>
/// Swaps RimWorld's UI font for Cadman (dyslexia-friendly, SIL OFL 1.1, shipped in
/// <c>1.7/Fonts/</c>). 1.7 only: it relies on Waylith's <see cref="IGuiBackend.RegisterFont"/>.
///
/// HOW IT WORKS: Waylith's GUIStyle reads <c>font.GameFontIndex</c> at draw time, so
/// swapping <see cref="GUIStyle.font"/> on Verse.Text's shared styles changes the font on
/// the next frame. Cadman is registered once per vanilla size at startup; a toggle then only
/// swaps references and recomputes Text's cached line heights.
///
/// LIMITS: styles copied from Text before a toggle (e.g. cached bubble styles) keep the
/// old font until rebuilt. With no GUI backend (-Headless) or a missing/unreadable TTF the
/// feature reports <see cref="Available"/> = false and never touches Text.
/// </summary>
[StaticConstructorOnStartup]
public static class CadmanFont
{
    private const string FontFile = "Fonts/Cadman-Regular.ttf";
    private const int NumFonts = 3;

    private static readonly GUIStyle[][] StyleSets =
    [
        Text.fontStyles,
        Text.textFieldStyles,
        Text.textAreaStyles,
        Text.textAreaReadOnlyStyles,
    ];

    // [styleSet][GameFont index]
    private static readonly Font[][] VanillaFonts = new Font[StyleSets.Length][];
    private static readonly Font[] CadmanFonts = new Font[NumFonts];

    public static bool Available { get; private set; }

    static CadmanFont()
    {
        try
        {
            Available = Register();
        }
        catch (Exception e)
        {
            ModLog.Error("[CadmanFont] Failed to register the Cadman font; the font setting is disabled.", e);
            Available = false;
        }

        if (Available && MSSFPMod.settings.UseCadmanFont)
            SetEnabled(true);
    }

    private static bool Register()
    {
        IGuiBackend backend = GuiBackend.Current;
        if (backend == null)
        {
            ModLog.Debug("[CadmanFont] No GUI backend (headless?); font swap unavailable.");
            return false;
        }

        string path = FindFontFile();
        if (path == null)
        {
            ModLog.Error($"[CadmanFont] {FontFile} not found in any MSSFP load folder; font swap unavailable.");
            return false;
        }

        byte[] data = File.ReadAllBytes(path);

        for (int set = 0; set < StyleSets.Length; set++)
        {
            VanillaFonts[set] = new Font[NumFonts];
            for (int i = 0; i < NumFonts; i++)
                VanillaFonts[set][i] = StyleSets[set][i].font;
        }

        for (int i = 0; i < NumFonts; i++)
        {
            // Distinct names: the backend dedupes registrations on (name, byte length).
            string name = $"Cadman_{i}";
            int index = backend.RegisterFont(name, data, VanillaFonts[0][i].fontSize, 0);
            if (index < 0)
            {
                ModLog.Error($"[CadmanFont] The GUI backend rejected {path}; font swap unavailable.");
                return false;
            }
            CadmanFonts[i] = new Font(name) { GameFontIndex = index };
        }

        return true;
    }

    private static string FindFontFile()
    {
        foreach (string folder in MSSFPMod.Mod.Content.foldersToLoadDescendingOrder)
        {
            string candidate = Path.Combine(folder, FontFile);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>Apply Cadman (true) or restore the vanilla fonts (false). No-op when unavailable.</summary>
    public static void SetEnabled(bool enabled)
    {
        if (!Available)
            return;

        float oldSmall = Text.LineHeightOf(GameFont.Small);

        for (int set = 0; set < StyleSets.Length; set++)
        {
            for (int i = 0; i < NumFonts; i++)
                StyleSets[set][i].font = enabled ? CadmanFonts[i] : VanillaFonts[set][i];
        }

        RecalculateLineHeights();
        ModLog.Log(
            $"[CadmanFont] {(enabled ? "Cadman" : "Vanilla")} UI font applied "
                + $"(small line height {oldSmall:0.#} -> {Text.LineHeightOf(GameFont.Small):0.#})."
        );
    }

    /// <summary>
    /// Mirrors Verse.Text's static constructor. Measures each style directly rather than via
    /// the Text.Font setter, which silently maps Tiny to Small when tiny text is unsupported.
    /// </summary>
    private static void RecalculateLineHeights()
    {
        float[] lineHeights = PrivateTextArray("lineHeights");
        float[] spaceBetweenLines = PrivateTextArray("spaceBetweenLines");
        if (lineHeights == null || spaceBetweenLines == null)
            return;

        GUIContent one = new("W");
        GUIContent two = new("W\nW");
        for (int i = 0; i < NumFonts; i++)
        {
            GUIStyle style = Text.fontStyles[i];
            float single = style.CalcHeight(one, 999f);
            lineHeights[i] = single;
            spaceBetweenLines[i] = style.CalcHeight(two, 999f) - single * 2f;
        }
    }

    private static float[] PrivateTextArray(string fieldName)
    {
        FieldInfo field = typeof(Text).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is float[] array && array.Length >= NumFonts)
            return array;

        ModLog.Warn($"[CadmanFont] Verse.Text.{fieldName} not found; line heights not recalculated.");
        return null;
    }
}

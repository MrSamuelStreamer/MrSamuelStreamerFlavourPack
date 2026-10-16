using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MSSFP.Utils;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace MSSFP.Fonts;

/// <summary>
/// Swaps RimWorld's UI font for any font file installed on the system. 1.7 only: it relies on
/// Waylith's <see cref="IGuiBackend.RegisterFont"/>.
///
/// A font is named by an id: <see cref="VanillaId"/> (empty) for the game's own font, otherwise
/// the absolute path of a font file.
///
/// HOW IT WORKS: Waylith's GUIStyle reads <c>font.GameFontIndex</c> at draw time, so swapping
/// <see cref="GUIStyle.font"/> on Verse.Text's shared styles changes the font on the next frame.
/// A font is registered once per vanilla size the first time it is used and then cached, so
/// switching back and forth only swaps references and recomputes Text's cached line heights.
/// The engine cannot unregister a font, so every font tried stays in memory until restart.
///
/// LIMITS: styles copied from Text before a switch (e.g. cached bubble styles) keep the old font
/// until rebuilt. With no GUI backend (-Headless) the feature reports <see cref="Available"/> =
/// false and never touches Text. A font that fails to load leaves the vanilla font in place.
/// <see cref="UiFontBootGuard"/> stops a font that crashes the game from crashing every launch.
/// </summary>
[StaticConstructorOnStartup]
public static class UiFont
{
    public const string VanillaId = "";

    private const int NumFonts = 3;

    // A font with absurdly tall lines must not be allowed to blow layouts up with it.
    private const float MaxLineHeightScale = 1.5f;

    private static readonly GUIStyle[][] StyleSets =
    [
        Text.fontStyles,
        Text.textFieldStyles,
        Text.textAreaStyles,
        Text.textAreaReadOnlyStyles,
    ];

    // [styleSet][GameFont index]
    private static readonly Font[][] VanillaFonts = new Font[StyleSets.Length][];

    // id -> one registered font per GameFont size
    private static readonly Dictionary<string, Font[]> Registered = new();

    private static float vanillaSmallLineHeight;

    public static bool Available { get; private set; }

    /// <summary>The id of the font currently on screen.</summary>
    public static string Current { get; private set; } = VanillaId;

    /// <summary>
    /// How much taller the current font's lines are than vanilla's (1 = same or shorter). Layouts
    /// built for a fixed height, such as RimHUD's docked pane, grow by this factor so their rows
    /// still fit. Capped at <see cref="MaxLineHeightScale"/>.
    /// </summary>
    public static float LineHeightScale
    {
        get
        {
            if (!Available || Current == VanillaId || vanillaSmallLineHeight <= 0f)
                return 1f;
            return Mathf.Clamp(Text.LineHeightOf(GameFont.Small) / vanillaSmallLineHeight, 1f, MaxLineHeightScale);
        }
    }

    static UiFont()
    {
        try
        {
            Available = CaptureVanillaFonts();
        }
        catch (Exception e)
        {
            ModLog.Error("[UiFont] Failed to initialise; the font setting is disabled.", e);
            Available = false;
        }

        if (Available)
            ApplyConfiguredFont();
    }

    private static void ApplyConfiguredFont()
    {
        Settings settings = MSSFPMod.settings;
        string wanted = settings.UiFontId ?? VanillaId;

        string interrupted = UiFontBootGuard.ReadInterrupted();
        if (interrupted != null)
        {
            UiFontBootGuard.Disarm();
            if (wanted != VanillaId && interrupted == wanted)
            {
                ModLog.Warn(
                    $"[UiFont] The game stopped before '{wanted}' finished drawing last time. "
                        + "Going back to the vanilla font and clearing the setting."
                );
                settings.UiFontId = VanillaId;
                settings.Write();
                return;
            }
        }

        if (wanted != VanillaId)
            Apply(wanted);
    }

    private static bool CaptureVanillaFonts()
    {
        if (GuiBackend.Current == null)
        {
            ModLog.Debug("[UiFont] No GUI backend (headless?); font swap unavailable.");
            return false;
        }

        for (int set = 0; set < StyleSets.Length; set++)
        {
            VanillaFonts[set] = new Font[NumFonts];
            for (int i = 0; i < NumFonts; i++)
                VanillaFonts[set][i] = StyleSets[set][i].font;
        }
        vanillaSmallLineHeight = Text.LineHeightOf(GameFont.Small);
        return true;
    }

    /// <summary>
    /// Switches the UI font. Returns false when the font could not be loaded, in which case the
    /// vanilla font is restored. Never throws. No-op (false) when unavailable.
    /// </summary>
    public static bool Apply(string id)
    {
        if (!Available)
            return false;

        id ??= VanillaId;
        if (id == Current)
            return true;

        Font[] target = null;
        bool loaded = true;
        if (id == VanillaId)
        {
            UiFontBootGuard.Disarm();
        }
        else
        {
            UiFontBootGuard.Arm(id);
            loaded = TryGetFonts(id, out target);
            if (!loaded)
            {
                UiFontBootGuard.Disarm();
                id = VanillaId;
            }
        }

        float oldSmall = Text.LineHeightOf(GameFont.Small);
        for (int set = 0; set < StyleSets.Length; set++)
        {
            for (int i = 0; i < NumFonts; i++)
                StyleSets[set][i].font = target != null ? target[i] : VanillaFonts[set][i];
        }
        RecalculateLineHeights();
        Current = id;

        ModLog.Log(
            $"[UiFont] {DisplayName(id)} UI font applied "
                + $"(small line height {oldSmall:0.#} -> {Text.LineHeightOf(GameFont.Small):0.#})."
        );
        return loaded;
    }

    /// <summary>A short name for the font an id refers to.</summary>
    public static string DisplayName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "MSS_FP_Settings_UiFont_Vanilla".Translate();
        return FontCatalog.Find(id)?.Label ?? Path.GetFileNameWithoutExtension(id);
    }

    /// <summary>Called by the Root.OnGUI patch.</summary>
    internal static void OnFrameDrawn() => UiFontBootGuard.OnFrameDrawn();

    private static bool TryGetFonts(string id, out Font[] fonts)
    {
        if (Registered.TryGetValue(id, out fonts))
            return true;

        string path = id;
        if (!File.Exists(path))
        {
            ModLog.Warn($"[UiFont] Font file '{path}' not found; keeping the vanilla font.");
            return false;
        }

        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ModLog.Warn($"[UiFont] Could not read {path}: {e.Message}");
            return false;
        }

        IGuiBackend backend = GuiBackend.Current;
        Font[] result = new Font[NumFonts];
        for (int i = 0; i < NumFonts; i++)
        {
            // The id is in the name because the backend dedupes registrations on (name, byte length).
            string name = $"MSSFP_UiFont:{id}:{i}";
            int index = backend.RegisterFont(name, data, VanillaFonts[0][i].fontSize, 0);
            if (index < 0)
            {
                ModLog.Warn($"[UiFont] The GUI backend rejected {path}; keeping the vanilla font.");
                fonts = null;
                return false;
            }
            result[i] = new Font(name) { GameFontIndex = index };
        }

        ModLog.Log($"[UiFont] Registered {path} ({data.Length / 1024} KB, {NumFonts} sizes).");
        Registered[id] = result;
        fonts = result;
        return true;
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

        ModLog.Warn($"[UiFont] Verse.Text.{fieldName} not found; line heights not recalculated.");
        return null;
    }
}

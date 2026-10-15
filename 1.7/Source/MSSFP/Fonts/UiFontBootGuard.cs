using System;
using System.IO;
using MSSFP.Utils;
using Verse;

namespace MSSFP.Fonts;

/// <summary>
/// Crash-loop breaker for the UI font setting. A system font can pass every check and still
/// crash the native font code (loading or drawing). Because the choice is saved, that would
/// repeat on every launch. So a marker file holding the font id is written BEFORE a font is
/// loaded and deleted a few frames after it has drawn. If the marker is still there at the
/// next launch, the last font never got that far, and <see cref="UiFont"/> drops it.
///
/// Kept apart from <see cref="UiFont"/> on purpose: <see cref="OnFrameDrawn"/> runs from a patch
/// on Root.OnGUI during start-up, and touching <see cref="UiFont"/> there would run its static
/// constructor far too early.
/// </summary>
public static class UiFontBootGuard
{
    private const string FileName = "MSSFP_UiFont.pending";

    // Root.OnGUI runs for layout, repaint and input events, so this is only a few real frames.
    private const int CallsBeforeClear = 20;

    private static int callsLeft;

    private static string MarkerPath => Path.Combine(GenFilePaths.ConfigFolderPath, FileName);

    /// <summary>The id of a font that was loaded but never finished drawing, or null.</summary>
    public static string ReadInterrupted()
    {
        try
        {
            return File.Exists(MarkerPath) ? File.ReadAllText(MarkerPath) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ModLog.Warn($"[UiFont] Could not read the boot-guard marker: {e.Message}");
            return null;
        }
    }

    /// <summary>Records that <paramref name="id"/> is about to be loaded.</summary>
    public static void Arm(string id)
    {
        try
        {
            File.WriteAllText(MarkerPath, id);
            callsLeft = CallsBeforeClear;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ModLog.Warn($"[UiFont] Could not write the boot-guard marker; crash protection is off: {e.Message}");
        }
    }

    /// <summary>Removes the marker: the font loaded and drew, was never loaded, or was undone.</summary>
    public static void Disarm()
    {
        callsLeft = 0;
        try
        {
            File.Delete(MarkerPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ModLog.Warn($"[UiFont] Could not delete the boot-guard marker: {e.Message}");
        }
    }

    /// <summary>Called after each Root.OnGUI. Clears the marker once the new font has drawn.</summary>
    public static void OnFrameDrawn()
    {
        if (callsLeft > 0 && --callsLeft == 0)
            Disarm();
    }
}

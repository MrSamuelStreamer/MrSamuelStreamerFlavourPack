using System;
using HarmonyLib;
using Verse;

namespace MSSFP.GRF;

/// <summary>
/// Compatibility layer for Tarte/Mlie's Graffiti Mod (packageId <c>Mlie.GraffitiMod</c>).
///
/// Only loaded when that mod is active — <c>loadFolders.xml</c> gates the whole
/// <c>Compatibility/Mlie.GraffitiMod</c> folder on it.
///
/// The patch binds its target by string through <see cref="HarmonyLib.AccessTools"/>
/// rather than <c>typeof</c>, so nothing here takes a compile-time dependency on
/// GraffitiMod.dll and nothing can throw a type-load error if mod ordering puts us first.
/// </summary>
public class MSSFPGRFMod : Mod
{
    public MSSFPGRFMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from MSSFPGRFMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif

        try
        {
            Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.GRF.main");
            harmony.PatchAll();
        }
        catch (Exception e)
        {
            // A failed target must never take the whole mod list down with it.
            ModLog.Error("Failed to apply Graffiti compatibility patches", e);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(ColonistBar), "ColonistsOrCorpsesInScreenRect")]
public static class ColonistBar_ColonistsOrCorpsesInScreenRect_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref List<Thing> __result)
    {
        if (
            MSSFPMod.settings?.EnableColonistPortraitHiding != true
            || MSSFPMod.settings.ShowHiddenPortraits
        )
            return;

        __result = __result
            .Where(thing =>
            {
                if (thing is Pawn pawn && pawn.RaceProps?.Humanlike == true)
                {
                    return MSSFPMod.settings.HiddenColonistIds?.Contains(pawn.thingIDNumber)
                        != true;
                }
                return true;
            })
            .ToList();
    }
}

[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
public static class ColonistBar_CheckRecacheEntries_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ColonistBar __instance)
    {
        if (
            MSSFPMod.settings?.EnableColonistPortraitHiding != true
            || MSSFPMod.settings.ShowHiddenPortraits
        )
            return;

        var cachedEntriesField = typeof(ColonistBar).GetField(
            "cachedEntries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var cachedDrawLocsField = typeof(ColonistBar).GetField(
            "cachedDrawLocs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (
            cachedEntriesField?.GetValue(__instance) is List<ColonistBar.Entry> cachedEntries
            && cachedDrawLocsField?.GetValue(__instance) is List<Vector2> cachedDrawLocs
        )
        {
            for (int i = cachedEntries.Count - 1; i >= 0; i--)
            {
                var entry = cachedEntries[i];
                if (
                    entry.pawn != null
                    && entry.pawn.IsColonist
                    && MSSFPMod.settings.HiddenColonistIds?.Contains(entry.pawn.thingIDNumber)
                        == true
                )
                {
                    cachedEntries.RemoveAt(i);
                    if (i < cachedDrawLocs.Count)
                        cachedDrawLocs.RemoveAt(i);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
public static class ColonistBar_ProcessInput_Patch
{
    private static Vector2 lastMousePos;
    private static float lastClickTime;
    private static Pawn lastClickedPawn;

    [HarmonyPrefix]
    public static bool Prefix(ColonistBar __instance)
    {
        if (MSSFPMod.settings?.EnableColonistPortraitHiding != true)
            return true;

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            Vector2 mousePos = Event.current.mousePosition;

            for (int i = 0; i < __instance.Entries.Count; i++)
            {
                var entry = __instance.Entries[i];
                if (entry.pawn?.RaceProps?.Humanlike == true)
                {
                    Vector2 drawLoc = __instance.DrawLocs[i];
                    Rect colonistRect = new Rect(drawLoc.x, drawLoc.y, 48f, 48f);

                    if (colonistRect.Contains(mousePos))
                    {
                        lastMousePos = mousePos;
                        lastClickTime = Time.realtimeSinceStartup;
                        lastClickedPawn = entry.pawn;
                        return true;
                    }
                }
            }
        }

        if (lastClickedPawn != null && Time.realtimeSinceStartup - lastClickTime > 0.1f)
        {
            Vector2 currentMousePos = Event.current.mousePosition;
            if (Vector2.Distance(currentMousePos, lastMousePos) < 5f)
            {
                bool isHidden =
                    MSSFPMod.settings.HiddenColonistIds?.Contains(lastClickedPawn.thingIDNumber)
                    == true;
                string menuText = isHidden
                    ? "MSS_FP_RestoreColonist".Translate()
                    : "MSS_FP_HideColonist".Translate();

                var capturedPawn = lastClickedPawn;
                var floatMenuOptions = new List<FloatMenuOption>
                {
                    new FloatMenuOption(
                        menuText,
                        () =>
                        {
                            if (capturedPawn == null)
                                return;

                            if (isHidden)
                            {
                                MSSFPMod.settings.HiddenColonistIds?.Remove(
                                    capturedPawn.thingIDNumber
                                );
                            }
                            else
                            {
                                MSSFPMod.settings.HiddenColonistIds ??= new HashSet<int>();
                                MSSFPMod.settings.HiddenColonistIds.Add(capturedPawn.thingIDNumber);
                            }
                            MSSFPMod.settings.Write();
                            Find.ColonistBar.MarkColonistsDirty();
                        }
                    ),
                };

                Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                lastClickedPawn = null;
                return false;
            }
            else
            {
                lastClickedPawn = null;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlaySettings))]
public static class PlaySettings_ColonistPortraitHiding_Patch
{
    public static readonly Texture2D ToggleTex = ContentFinder<Texture2D>.Get(
        "UI/MSS_FP_Eye_Toggle"
    );

    [HarmonyPatch(nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    [HarmonyPostfix]
    public static void DoPlaySettingsGlobalControls_Patch(WidgetRow row)
    {
        if (MSSFPMod.settings?.EnableColonistPortraitHiding != true)
            return;

        if (MSSFPMod.settings.HiddenColonistIds?.Any() != true)
            return;

        if (row.ButtonIcon(ToggleTex, "MSS_FP_RightClickToRestore".Translate()))
        {
            if (Event.current.button == 1)
            {
                ShowHiddenColonistsContextMenu();
            }
        }
    }

    private static void ShowHiddenColonistsContextMenu()
    {
        var hiddenColonists = MSSFPMod.settings.HiddenColonistIds?.ToList() ?? new List<int>();
        if (!hiddenColonists.Any())
            return;

        var options = new List<FloatMenuOption>();

        foreach (int colonistId in hiddenColonists)
        {
            var colonist = Find.CurrentMap?.mapPawns?.AllPawnsSpawned?.FirstOrDefault(p =>
                p.thingIDNumber == colonistId
            );
            if (colonist != null)
            {
                options.Add(
                    new FloatMenuOption(
                        "MSS_FP_RestoreColonist".Translate() + ": " + colonist.Name?.ToStringFull,
                        () =>
                        {
                            MSSFPMod.settings.HiddenColonistIds?.Remove(colonistId);
                            MSSFPMod.settings.Write();
                            Find.ColonistBar.MarkColonistsDirty();
                        }
                    )
                );
            }
        }

        if (options.Any())
        {
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}

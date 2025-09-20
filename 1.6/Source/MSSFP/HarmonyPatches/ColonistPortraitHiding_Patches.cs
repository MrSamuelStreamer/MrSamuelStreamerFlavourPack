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
        if (MSSFPMod.settings?.EnableColonistPortraitHiding != true)
            return;

        if (MSSFPMod.settings.ShowHiddenPortraits)
            return;

        var worldComp = Find.World.GetComponent<ColonistHidingWorldComponent>();
        if (worldComp == null)
            return;

        __result = __result
            .Where(thing =>
            {
                if (thing is Pawn pawn && pawn.RaceProps?.Humanlike == true)
                {
                    return !worldComp.IsHidden(pawn);
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
        if (MSSFPMod.settings?.EnableColonistPortraitHiding != true)
            return;

        if (MSSFPMod.settings.ShowHiddenPortraits)
            return;

        var worldComp = Find.World.GetComponent<ColonistHidingWorldComponent>();
        if (worldComp == null)
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
                if (entry.pawn != null && entry.pawn.IsColonist && worldComp.IsHidden(entry.pawn))
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
                var worldComp = Find.World.GetComponent<ColonistHidingWorldComponent>();
                if (worldComp == null)
                    return true;

                bool isHidden = worldComp.IsHidden(lastClickedPawn);
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
                                worldComp.ShowColonist(capturedPawn);
                            }
                            else
                            {
                                worldComp.HideColonist(capturedPawn);
                            }
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

[StaticConstructorOnStartup]
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

        var worldComp = Find.World.GetComponent<ColonistHidingWorldComponent>();
        if (worldComp?.GetHiddenColonists().Any() != true)
            return;

        bool previousState = MSSFPMod.settings.ShowHiddenPortraits;
        bool wasRightClick = Event.current.button == 1;

        row.ToggleableIcon(
            ref MSSFPMod.settings.ShowHiddenPortraits,
            ToggleTex,
            "MSS_FP_ShowHiddenColonists".Translate(),
            SoundDefOf.Mouseover_ButtonToggle
        );

        if (previousState != MSSFPMod.settings.ShowHiddenPortraits)
        {
            if (wasRightClick)
            {
                MSSFPMod.settings.ShowHiddenPortraits = previousState;
                ShowHiddenColonistsContextMenu();
            }
            else
            {
                Find.ColonistBar?.MarkColonistsDirty();
            }
        }
    }

    private static void ShowHiddenColonistsContextMenu()
    {
        var worldComp = Find.World.GetComponent<ColonistHidingWorldComponent>();
        if (worldComp == null)
            return;

        var hiddenColonists = worldComp.GetHiddenColonists();
        if (!hiddenColonists.Any())
            return;

        var options = hiddenColonists
            .Where(colonist => colonist != null)
            .Select(colonist => new FloatMenuOption(
                "MSS_FP_RestoreColonist".Translate() + ": " + colonist.Name?.ToStringFull,
                () =>
                {
                    worldComp.ShowColonist(colonist);
                    Find.ColonistBar.MarkColonistsDirty();
                }
            ))
            .ToList();

        if (options.Any())
        {
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}

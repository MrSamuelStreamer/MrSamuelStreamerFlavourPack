using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
public static class ColonistBar_CheckRecacheEntries_Patch
{
    private static readonly AccessTools.FieldRef<ColonistBar, List<ColonistBar.Entry>> CachedEntriesRef =
        AccessTools.FieldRefAccess<ColonistBar, List<ColonistBar.Entry>>("cachedEntries");

    private static readonly AccessTools.FieldRef<ColonistBar, List<Vector2>> CachedDrawLocsRef =
        AccessTools.FieldRefAccess<ColonistBar, List<Vector2>>("cachedDrawLocs");

    private static readonly AccessTools.FieldRef<ColonistBar, ColonistBarDrawLocsFinder> DrawLocsFinderRef =
        AccessTools.FieldRefAccess<ColonistBar, ColonistBarDrawLocsFinder>("drawLocsFinder");

    private static readonly AccessTools.FieldRef<ColonistBar, float> CachedScaleRef =
        AccessTools.FieldRefAccess<ColonistBar, float>("cachedScale");

    // Every ColonistBar.Entries read calls CheckRecacheEntries, including reads made by
    // vanilla's own layout code mid-rebuild. Only filter when vanilla actually rebuilt.
    [HarmonyPrefix]
    public static void Prefix(bool ___entriesDirty, out bool __state)
    {
        __state = ___entriesDirty;
    }

    [HarmonyPostfix]
    public static void Postfix(ColonistBar __instance, bool __state)
    {
        if (!__state)
            return;

        if (MSSFPMod.settings?.EnableColonistPortraitHiding != true)
            return;

        if (MSSFPMod.settings.ShowHiddenPortraits)
            return;

        var worldComp = Find.World.GetComponent<ColonistHidingWorldComponent>();
        if (worldComp == null)
            return;

        List<ColonistBar.Entry> cachedEntries = CachedEntriesRef(__instance);
        List<Vector2> cachedDrawLocs = CachedDrawLocsRef(__instance);
        ColonistBarDrawLocsFinder drawLocsFinder = DrawLocsFinderRef(__instance);

        if (cachedEntries == null || cachedDrawLocs == null || drawLocsFinder == null)
            return;

        // Camps and caravans with nobody visible keep their hidden pawns so the player
        // can still reach them from the bar. A player home map is allowed to empty out.
        HashSet<int> offHomeGroupsAllHidden = new HashSet<int>();
        HashSet<int> groupsWithVisiblePawn = new HashSet<int>();
        for (int i = 0; i < cachedEntries.Count; i++)
        {
            ColonistBar.Entry entry = cachedEntries[i];
            if (entry.map?.IsPlayerHome != true)
                offHomeGroupsAllHidden.Add(entry.group);
            if (entry.pawn != null && !worldComp.IsHidden(entry.pawn))
                groupsWithVisiblePawn.Add(entry.group);
        }
        offHomeGroupsAllHidden.ExceptWith(groupsWithVisiblePawn);

        bool removedAny = false;
        for (int i = cachedEntries.Count - 1; i >= 0; i--)
        {
            ColonistBar.Entry entry = cachedEntries[i];
            if (
                entry.pawn != null
                && worldComp.IsHidden(entry.pawn)
                && !offHomeGroupsAllHidden.Contains(entry.group)
            )
            {
                cachedEntries.RemoveAt(i);
                removedAny = true;
            }
        }

        if (!removedAny)
            return;

        // Layout sizes entriesInGroup by groupsCount and indexes it by group id, so a
        // removed home group must not leave a hole. Entry delegates capture the group,
        // so renumbered entries have to be rebuilt rather than assigned in place.
        int groupsCount = 0;
        int lastGroup = -1;
        for (int i = 0; i < cachedEntries.Count; i++)
        {
            ColonistBar.Entry entry = cachedEntries[i];
            if (entry.group != lastGroup)
            {
                lastGroup = entry.group;
                groupsCount++;
            }

            int newGroup = groupsCount - 1;
            if (newGroup != entry.group)
                cachedEntries[i] = new ColonistBar.Entry(entry.pawn, entry.map, newGroup);
        }

        drawLocsFinder.CalculateDrawLocs(cachedDrawLocs, out float scale, groupsCount);
        CachedScaleRef(__instance) = scale;
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

            int entryCount = Mathf.Min(__instance.Entries.Count, __instance.DrawLocs.Count);
            Vector2 size = __instance.Size;
            for (int i = 0; i < entryCount; i++)
            {
                var entry = __instance.Entries[i];
                if (entry.pawn?.RaceProps?.Humanlike == true)
                {
                    Vector2 drawLoc = __instance.DrawLocs[i];
                    Rect colonistRect = new Rect(drawLoc.x, drawLoc.y, size.x, size.y);

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

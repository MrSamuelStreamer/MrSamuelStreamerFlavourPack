using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.AICore;

/// <summary>
/// Static manager for <see cref="AICoreBubble"/> instances keyed by host <see cref="Thing"/>.
///
/// INVARIANTS:
/// - Pure render-time. NOT scribed. Map transitions wipe via
///   <c>Patch_AICoreClearBubbles</c> on <see cref="Verse.Profile.MemoryUtility.ClearAllMapsAndWorld"/>.
/// - <see cref="OnGUI"/> driven by Harmony postfix on
///   <see cref="RimWorld.MapInterface.MapInterfaceOnGUI_BeforeMainTabs"/> — UI thread, single-threaded.
/// - <see cref="Add"/> called from sim-thread chatter; same thread under RimWorld single-thread model.
/// - FIFO eviction at <see cref="PerHostMax"/>: oldest bubble has fade accelerated (no visual pop).
/// - Self-disable on exception: <see cref="Disabled"/> flips true, no further draws.
///
/// Why we don't route through Jaxe.Bubbles' <c>PlayLog.Add</c> — Bubbles types its public surface as
/// <c>Pawn</c>, host here is <see cref="Building"/>.
/// DO NOT add a PlayLog hook later: any path that creates a <c>LogEntry</c> for a non-Pawn host is
/// undefined behaviour against the vanilla social log + Bubbles' Pawn-typed pipeline.
/// </summary>
public static class AICoreBubbler
{
    /// <summary>Per-host stack cap. Older bubbles get fade-accelerated when this is hit.</summary>
    public const int PerHostMax = 3;

    /// <summary>Pixel gap between stacked bubbles (at scale=1).</summary>
    public const int VerticalSpacing = 4;

    /// <summary>Camera RootSize bounds — bubbles only render in this zoom range.</summary>
    public const float MinRootSize = 11f;
    public const float MaxRootSize = 60f;

    /// <summary>Set true on first exception inside <see cref="OnGUI"/>; permanent until restart.</summary>
    public static bool Disabled { get; private set; }

    private static readonly Dictionary<Thing, List<AICoreBubble>> bubblesByHost =
        new Dictionary<Thing, List<AICoreBubble>>();

    private static readonly List<Thing> deadHostsScratch = new List<Thing>();

    /// <summary>
    /// Post a new bubble above <paramref name="host"/>. Caller responsible for personality color.
    /// No-op if <paramref name="host"/> is null/despawned or system disabled.
    /// </summary>
    public static void Add(Thing host, string text, Color color)
    {
        if (Disabled) return;
        if (host == null || !host.Spawned) return;
        if (string.IsNullOrEmpty(text)) return;

        if (!bubblesByHost.TryGetValue(host, out List<AICoreBubble> list))
        {
            list = new List<AICoreBubble>();
            bubblesByHost[host] = list;
        }

        // FIFO accelerate-fade — never yank a still-visible bubble (visual pop). Oldest = index 0.
        while (list.Count >= PerHostMax)
        {
            list[0].ForceFade();
            // Don't remove yet; Draw returns false next frame and we evict cleanly.
            // Edge case: if fade already maxed (unlikely under PerHostMax=3), break to avoid infinite loop.
            if (list.Count >= PerHostMax + 1) { list.RemoveAt(0); }
            else break;
        }

        int now = Find.TickManager?.TicksGame ?? 0;
        list.Add(new AICoreBubble(text, color, now));
    }

    /// <summary>
    /// Per-frame draw. Called from <c>Patch_MapInterfaceOnGUI</c> postfix on the Unity main thread.
    /// Wraps in try/catch externally — exceptions here flip <see cref="Disabled"/> and silence the system.
    /// </summary>
    public static void OnGUI()
    {
        if (Disabled) return;
        if (bubblesByHost.Count == 0) return;

        Map map = Find.CurrentMap;
        if (map == null) return;

        // Don't draw on world view.
        if (WorldRendererUtility.DrawingMap == false) return;

        // Honour F10 screenshot mode — vanilla uses this to suppress overlays.
        if (Find.UIRoot?.screenshotMode != null && Find.UIRoot.screenshotMode.FiltersCurrentEvent) return;

        CameraDriver cam = Find.CameraDriver;
        if (cam == null) return;
        float root = cam.RootSize;
        if (root < MinRootSize || root > MaxRootSize) return;

        // Camera-scale factor (vanilla bubble libs use ~22f as 1.0 reference).
        float scale = Mathf.Clamp(22f / root, 0.5f, 1.5f);

        deadHostsScratch.Clear();

        foreach (KeyValuePair<Thing, List<AICoreBubble>> kv in bubblesByHost)
        {
            Thing host = kv.Key;
            List<AICoreBubble> list = kv.Value;

            // Cull hosts that left the current map (despawn / map switch / destroy).
            if (host == null || host.Destroyed || !host.Spawned || host.Map != map)
            {
                deadHostsScratch.Add(host);
                continue;
            }

            // Skip fogged — don't reveal hidden cores.
            if (host.Map.fogGrid != null && host.Map.fogGrid.IsFogged(host.Position))
                continue;

            // Anchor: vanilla label-pos above the building. -0.6f mirrors LabelDrawPos truncation.
            Vector2 anchor = GenMapUI.LabelDrawPosFor(host, -0.6f);

            // Newest bubble drawn lowest (closest to host); older stack upward.
            int verticalOffsetPx = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                AICoreBubble b = list[i];
                bool keep = b.Draw(anchor, scale, verticalOffsetPx);
                if (!keep)
                {
                    list.RemoveAt(i);
                    continue;
                }
                verticalOffsetPx += b.Height + VerticalSpacing;
            }

            if (list.Count == 0)
                deadHostsScratch.Add(host);
        }

        for (int i = 0; i < deadHostsScratch.Count; i++)
            bubblesByHost.Remove(deadHostsScratch[i]);
        deadHostsScratch.Clear();
    }

    /// <summary>
    /// Drop all state. Called from <c>Patch_AICoreClearBubbles</c> on game shutdown / map clear.
    /// </summary>
    public static void Clear()
    {
        bubblesByHost.Clear();
        deadHostsScratch.Clear();
    }

    /// <summary>
    /// Permanently silence renderer until next assembly load. Called by patch try/catch.
    /// </summary>
    public static void DisableForSession()
    {
        Disabled = true;
        bubblesByHost.Clear();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MSSFP.Comps;
using RimWorld;
using Verse;

namespace MSSFP.Gravship;

/// <summary>
/// Owns the orb jump-event pool and the weighted pick. The pool is built lazily on first use
/// (which only happens on a gravship landing — long after all mod assemblies have loaded) by
/// reflecting over every loaded assembly whose name contains "MSSFP" for concrete
/// <see cref="OrbJumpEvent"/> subclasses. This mirrors how <see cref="Settings"/> discovers its
/// SettingsTabs, and lets the optional MSSFP.VGE assembly contribute events with no hard reference
/// from the core.
/// </summary>
public static class OrbJumpEventManager
{
    private static List<OrbJumpEvent> _events;

    private static List<OrbJumpEvent> Events => _events ??= BuildRegistry();

    private static List<OrbJumpEvent> BuildRegistry()
    {
        List<OrbJumpEvent> events = new();
        foreach (
            Assembly assembly in AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(a => a.FullName != null && a.FullName.Contains("MSSFP"))
        )
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (
                Type type in types.Where(t =>
                    t != null && !t.IsAbstract && typeof(OrbJumpEvent).IsAssignableFrom(t)
                )
            )
            {
                try
                {
                    if (Activator.CreateInstance(type) is OrbJumpEvent evt)
                        events.Add(evt);
                }
                catch (Exception ex)
                {
                    Log.Error($"[MSSFP] Failed to instantiate OrbJumpEvent {type.FullName}: {ex}");
                }
            }
        }

        Log.Message($"[MSSFP] Orb jump-event pool: {events.Count} event(s) ({events.Select(e => e.Label).ToCommaList()}).");
        return events;
    }

    /// <summary>
    /// Pick one event by weight and fire it. Caller is responsible for the orb-assist check and the
    /// overall chance roll; this method always fires (when the pool is non-empty). Each event's
    /// Fire is wrapped so a single bad event can't take down the others or the host postfix.
    /// </summary>
    public static void TryRunJumpEvent(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        OrbJumpEvent picked = WeightedPick(Events);
        if (picked == null)
            return;
        try
        {
            picked.Fire(gravship, engine, orb);
        }
        catch (Exception ex)
        {
            Log.Error($"[MSSFP] Orb jump-event '{picked.Label}' threw: {ex}");
        }
    }

    /// <summary>Cumulative-weight selection, mirroring HauntEventMapComponent.PickEvent.</summary>
    private static OrbJumpEvent WeightedPick(List<OrbJumpEvent> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        float total = 0f;
        foreach (OrbJumpEvent e in pool)
            total += Math.Max(0f, e.Weight);
        if (total <= 0f)
            return null;

        float roll = Rand.Value * total;
        float cumulative = 0f;
        foreach (OrbJumpEvent e in pool)
        {
            cumulative += Math.Max(0f, e.Weight);
            if (roll <= cumulative)
                return e;
        }
        return pool[pool.Count - 1];
    }
}

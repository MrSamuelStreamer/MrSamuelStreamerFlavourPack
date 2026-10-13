using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

// Note: namespace deliberately not "MSSFP.Map" — that would shadow Verse.Map across the
// project (see CS0118 from older revisions of this file).
namespace MSSFP.MapComponents;

/// <summary>
/// Tracks whether any active <c>MSSFP_SunTower</c> is on the map and toggles the
/// UI-marker GameCondition <c>MSSFP_SunTowerLight</c> on the map's
/// <see cref="GameConditionManager"/>.
///
/// Also exposes <see cref="AnyActive"/> + <see cref="IsForcedDark"/> as cached
/// per-frame inputs for the SkyManager.CurrentSkyTarget Harmony postfix. The postfix
/// reads only these flags — never walks listerThings — so the per-frame cost is a
/// dictionary lookup and two bool reads.
///
/// Cache is recomputed on <see cref="Notify_ActiveStateChanged"/>, which is invoked by
/// each tower on spawn, despawn, power signal, and TickRare-detected roof changes.
/// Active count is recomputed on demand from <c>map.listerThings.ThingsOfDef(...)</c>
/// rather than persisted, so save/load + destroyed-while-active edge cases reconcile
/// to the truth on the next call.
/// </summary>
public class MSSFPSunTowerMapComponent : MapComponent
{
    /// <summary>
    /// Anomaly-DLC darkness conditions that the sun tower must NOT override.
    /// UnnaturalDarkness + DarkenedSkies are narrative-driven dark states; vanilla
    /// Eclipse / SunBlocker are weather-class events that the tower DOES beat.
    /// </summary>
    private static readonly HashSet<string> DarknessOverrides = new()
    {
        "UnnaturalDarkness",
        "DarkenedSkies",
    };

    /// <summary>True if at least one tower on this map is currently powered + unroofed.
    /// Read by the SkyManager.CurrentSkyTarget postfix; written by
    /// <see cref="Notify_ActiveStateChanged"/>. Not persisted.</summary>
    public bool AnyActive { get; private set; }

    public MSSFPSunTowerMapComponent(Verse.Map map) : base(map) { }

    /// <summary>
    /// True when an Anomaly darkness condition overrides solar mechanics on this map.
    /// The postfix skips the glow clamp in this case — narrative darkness wins.
    /// Computed live (not cached) because GameConditions are added/removed off our
    /// signal path and we don't want to wire a global condition listener.
    /// </summary>
    public bool IsForcedDark
    {
        get
        {
            List<GameCondition> active = map.gameConditionManager.ActiveConditions;
            for (int i = 0; i < active.Count; i++)
            {
                if (DarknessOverrides.Contains(active[i].def.defName))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Recompute active-tower presence and bring the UI-marker condition into sync.
    /// Idempotent. Called on each tower's signal/tick/despawn boundary.
    ///
    /// Uses <see cref="MSSFP.Buildings.Building_SunTower.RecomputeActive"/> on each
    /// tower instead of reading the cached <c>active</c> field — this avoids stale
    /// reads when multiple towers' states change in the same tick (e.g. one despawns
    /// while another is roofed-over before its own TickRare runs).
    /// </summary>
    public void Notify_ActiveStateChanged()
    {
        ThingDef towerDef = MSSFPDefOf.MSSFP_SunTower;
        if (towerDef == null)
        {
            return;
        }

        bool anyActive = false;
        List<Thing> things = map.listerThings.ThingsOfDef(towerDef);
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i] is MSSFP.Buildings.Building_SunTower tower && tower.RecomputeActive())
            {
                anyActive = true;
                // Don't break — RecomputeActive on every tower keeps each tower's
                // own `active` field current, which Building_SunTower.Graphic and
                // future per-tower UI/effects rely on.
            }
        }

        AnyActive = anyActive;

        GameConditionDef conditionDef = MSSFPDefOf.MSSFP_SunTowerLight;
        if (conditionDef == null)
        {
            return;
        }

        GameConditionManager mgr = map.gameConditionManager;
        GameCondition existing = mgr.GetActiveCondition(conditionDef);

        if (anyActive && existing == null)
        {
            GameCondition cond = GameConditionMaker.MakeConditionPermanent(conditionDef);
            mgr.RegisterCondition(cond);
        }
        else if (!anyActive && existing != null)
        {
            existing.End();
        }
    }
}

using MSSFP.Comps;
using MSSFP.Holo;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Buildings;

/// <summary>
/// AI core building. Swaps to an offline sprite while <see cref="CompPowerTrader.PowerOn"/>
/// is false. Mirrors the <see cref="Building_SunTower"/> graphic-swap pattern (graphic cache
/// resolved in SpawnSetup, getter falls back to base while cache unresolved).
///
/// Sprite swap is driven entirely by the live PowerOn signal — no state of our own to
/// scribe. Map mesh dirtying on power transitions is handled by vanilla
/// <see cref="CompPowerTrader"/> (PowerTurnedOn / PowerTurnedOff signals trigger
/// <see cref="Thing.DirtyMapMesh"/> through the parent thing's notify path).
///
/// Projection / persona lifecycle stays on <see cref="MSSFP.Holo.CompHoloProjector"/> and
/// <see cref="MSSFP.Comps.CompTrueAICore"/>. This subclass exists solely for the graphic
/// override.
/// </summary>
public class Building_AICore : Building_ResearchBench
{
    private CompPowerTrader powerComp;
    private Graphic graphicPowered;
    private Graphic graphicOffline;

    // Per-instance — lazy-allocated on first draw. Each visible core needs its own block
    // because the gradient centre is cursor-relative-to-this-core's world position.
    private MaterialPropertyBlock orbPropertyBlock;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        ResolveGraphicsCache();
    }

    private bool IsPowered => powerComp == null || powerComp.PowerOn;

    /// <summary>
    /// Per-frame orb overlay. Requires <c>graphicData/drawerType = MapMeshAndRealtime</c> on
    /// the ThingDef — without it, vanilla bakes the base sprite into the map mesh and DrawAt
    /// is never called.
    /// </summary>
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        if (!IsPowered) return;
        if (!AICoreOrbShader.IsLoaded) return;

        // Tick-driven bob. Phase is locked to TicksGame so paused = frozen and
        // Superfast = faster bob (user spec: "obeys ticks").
        int t = Find.TickManager.TicksGame;
        float phase = (t % AICoreOrbRenderer.BobCycleTicks)
            / (float)AICoreOrbRenderer.BobCycleTicks
            * 2f
            * Mathf.PI;
        float bobZ = Mathf.Sin(phase)
            * AICoreOrbRenderer.OrbWorldSize
            * AICoreOrbRenderer.BobAmplitudeFraction;

        // World position of the orb centre (pre-bob z is the SVG-derived offset from
        // building origin; bob adds to it).
        Vector3 orbWorld = drawLoc
            + new Vector3(0f, 0f, AICoreOrbRenderer.OrbCenterZOffset + bobZ);

        // Cursor → orb-radius normalized delta → clamped → scaled into UV space.
        Vector3 mouseW = UI.MouseMapPosition();
        float orbRadius = AICoreOrbRenderer.OrbWorldSize * 0.5f;
        Vector2 delta = new Vector2(
            (mouseW.x - orbWorld.x) / orbRadius,
            (mouseW.z - orbWorld.z) / orbRadius
        );
        float mag = delta.magnitude;
        if (mag > AICoreOrbRenderer.MaxFollowRadius)
        {
            delta = delta * (AICoreOrbRenderer.MaxFollowRadius / mag);
        }

        Vector2 uv = new Vector2(0.5f, 0.5f)
            + AICoreOrbRenderer.BaseGradientOffset
            + delta * AICoreOrbRenderer.GradientFollowScale;

        orbPropertyBlock ??= new MaterialPropertyBlock();
        orbPropertyBlock.SetVector(
            AICoreOrbRenderer.GradientCenterID,
            new Vector4(uv.x, uv.y, 0f, 0f)
        );

        Vector3 drawPos = orbWorld;
        drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
        Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, Vector3.one);
        Graphics.DrawMesh(
            AICoreOrbRenderer.OrbMesh,
            matrix,
            AICoreOrbRenderer.OrbMaterial,
            0,
            null,
            0,
            orbPropertyBlock
        );
    }

    protected override void ReceiveCompSignal(string signal)
    {
        base.ReceiveCompSignal(signal);
        // Force a redraw on power edges. CompPowerTrader already dirties the map mesh
        // via its own notify path, but DirtyMapMesh is idempotent and cheap — belt + suspenders
        // against any future code path that emits these signals without the dirty.
        if (signal == "PowerTurnedOn" || signal == "PowerTurnedOff")
        {
            if (Spawned)
            {
                DirtyMapMesh(Map);
            }
        }
    }

    public override Graphic Graphic
    {
        get
        {
            if (graphicPowered == null || graphicOffline == null)
            {
                return base.Graphic;
            }
            bool powered = powerComp == null || powerComp.PowerOn;
            return powered ? graphicPowered : graphicOffline;
        }
    }

    private void ResolveGraphicsCache()
    {
        if (def?.graphicData == null)
        {
            return;
        }
        graphicPowered = def.graphic;
        graphicOffline = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/MSSFP_AI_Core_Offline",
            def.graphic.Shader,
            def.graphicData.drawSize,
            Color.white
        );
    }

    /// <summary>
    /// On deconstruct or destruction, spawn a <c>MSSFP_LoadedAIPersonaCore</c> at this
    /// building's position carrying the holo Pawn + active persona. Lets the player
    /// redeploy the same AI into a fresh chassis later via the loaded core's "Deploy
    /// Pondering Orb" gizmo.
    ///
    /// Other DestroyMode values (Vanish, FailConstruction, etc.) do NOT drop a loaded
    /// core — those code paths are internal cleanup (e.g. minify, frame-cancel) where
    /// dropping a duplicate persona item would duplicate the AI.
    /// </summary>
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Spawned && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
        {
            IntVec3 savedPos = Position;
            Map savedMap = Map;
            CompHoloProjector proj = this.TryGetComp<CompHoloProjector>();
            CompTrueAICore core = this.TryGetComp<CompTrueAICore>();

            // Recall any live projection back into the projector's stored container so the
            // pawn isn't left dangling on the map when the projector is destroyed.
            proj?.OnDespawnProjection();

            ThingDef loadedDef = DefDatabase<ThingDef>.GetNamedSilentFail("MSSFP_LoadedAIPersonaCore");
            if (loadedDef != null)
            {
                Thing loaded = ThingMaker.MakeThing(loadedDef);
                CompLoadedAIPersonaCore loadedComp = loaded.TryGetComp<CompLoadedAIPersonaCore>();
                if (loadedComp != null)
                {
                    if (proj?.stored != null && proj.stored.Count > 0)
                    {
                        proj.stored.TryTransferAllToContainer(loadedComp.storedHolo);
                    }
                    loadedComp.storedPersonality = core?.activePersonality;
                }
                GenSpawn.Spawn(loaded, savedPos, savedMap);
            }

            // Vanilla AIPersonaCore refund suppression now lives in XML via
            // <building><leavingsBlacklist><li>AIPersonaCore</li></leavingsBlacklist>.
            // GenLeaving.DoLeavingsFor consults that list directly. Earlier costList-mutation
            // approach failed because RimWorld.CostListCalculator caches CostListAdjusted
            // results keyed by (def, stuff) — mutating def.costList post-cache had no effect.
        }

        base.Destroy(mode);
    }
}

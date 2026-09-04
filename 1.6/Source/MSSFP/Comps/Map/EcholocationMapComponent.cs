using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps.Map;

public struct EcholocationBlip
{
    public IntVec3 Position;
    public int Tick;
    public float Intensity;
}

public class EcholocationMapComponent(Verse.Map map) : MapComponent(map)
{
    private const int CheckInterval = 60;
    private const int BlipMaxAge = 600;

    private static readonly (float T, Color Color)[] MagmaStops =
    [
        (0f, new Color(0f, 0f, 0f)),
        (0.25f, new Color(0.3f, 0f, 0.4f)),
        (0.5f, new Color(0.7f, 0.1f, 0.3f)),
        (0.75f, new Color(0.9f, 0.4f, 0.05f)),
        (1f, new Color(1f, 1f, 0.6f)),
    ];

    private const float MinActivity = 0.2f;
    private const float ActivityDecayPerFrame = 0.93f;
    private const float ActivityGainPerWorldUnit = 10f;
    private const float MinGlowSize = 1.1f;
    private const float MaxGlowSize = 3.4f;

    public bool Active;
    public int EndTick;
    public Pawn Caster;
    public List<EcholocationBlip> Blips = [];

    private Mesh _quadMesh;
    private Material _blackMat;
    private Mesh _pawnGlowMesh;
    private Material _pawnGlowMat;
    private MaterialPropertyBlock _pawnGlowMpb;

    private readonly Dictionary<Pawn, Vector3> _lastDrawPos = new();
    private readonly Dictionary<Pawn, float> _activity = new();

    public void StartEffect(Pawn caster, int durationTicks)
    {
        Active = true;
        Caster = caster;
        EndTick = Find.TickManager.TicksGame + durationTicks;
        EnsureAssets();
        ApplyBlindness();
        caster.health.AddHediff(MSSFPDefOf.MSSFP_EcholocationCasterBuff);
    }

    public void AddBlip(IntVec3 position, float intensity)
    {
        if (!Active)
            return;

        Blips.Add(
            new EcholocationBlip
            {
                Position = position,
                Tick = Find.TickManager.TicksGame,
                Intensity = intensity,
            }
        );
    }

    public override void MapComponentTick()
    {
        if (!Active)
            return;

        if (Find.TickManager.TicksGame >= EndTick)
        {
            EndEffect();
            return;
        }

        if (map.IsHashIntervalTick(CheckInterval))
        {
            ApplyBlindness();
            Blips.RemoveAll(b => Find.TickManager.TicksGame - b.Tick > BlipMaxAge);
            PruneDespawnedPawns();
        }
    }

    private void PruneDespawnedPawns()
    {
        var spawned = new HashSet<Pawn>(map.mapPawns.AllPawnsSpawned);
        foreach (Pawn stale in _lastDrawPos.Keys.Where(p => !spawned.Contains(p)).ToList())
        {
            _lastDrawPos.Remove(stale);
            _activity.Remove(stale);
        }
    }

    private void ApplyBlindness()
    {
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn == Caster)
                continue;

            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) <= 0f)
                continue;

            if (!pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSSFP_EcholocationBlindness))
            {
                pawn.health.AddHediff(MSSFPDefOf.MSSFP_EcholocationBlindness);
            }
        }
    }

    private void EndEffect()
    {
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(
                MSSFPDefOf.MSSFP_EcholocationBlindness
            );
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        Hediff buff = Caster?.health.hediffSet.GetFirstHediffOfDef(
            MSSFPDefOf.MSSFP_EcholocationCasterBuff
        );
        if (buff != null)
        {
            Caster.health.RemoveHediff(buff);
        }

        Active = false;
        Caster = null;
        Blips.Clear();
        _lastDrawPos.Clear();
        _activity.Clear();
    }

    public override void MapComponentDraw()
    {
        if (!Active || Find.CurrentMap != map || _quadMesh == null)
            return;

        // Below Pawn (23) so pawns and the caster still render on top; above
        // Building/Item so floor/walls stay hidden. World-anchored (not
        // camera-following) so it naturally pans/zooms with the map.
        float blackAltitude = AltitudeLayer.Item.AltitudeFor();
        float glowAltitude = AltitudeLayer.ItemImportant.AltitudeFor();

        Graphics.DrawMesh(_quadMesh, new Vector3(0f, blackAltitude, 0f), Quaternion.identity, _blackMat, 0);

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            Vector3 drawPos = pawn.DrawPos;
            float moveDist = _lastDrawPos.TryGetValue(pawn, out Vector3 prev)
                ? new Vector2(drawPos.x - prev.x, drawPos.z - prev.z).magnitude
                : 0f;
            _lastDrawPos[pawn] = drawPos;

            float decayed = _activity.TryGetValue(pawn, out float prevActivity)
                ? prevActivity * ActivityDecayPerFrame
                : 0f;
            float activity = Mathf.Max(decayed, Mathf.Clamp01(moveDist * ActivityGainPerWorldUnit));
            _activity[pawn] = activity;

            float shown = Mathf.Max(activity, MinActivity);
            float size = Mathf.Lerp(MinGlowSize, MaxGlowSize, shown);
            Color tint = MagmaColor(shown);

            _pawnGlowMpb.SetColor(ColorPropertyId, tint);
            var matrix = Matrix4x4.TRS(
                new Vector3(drawPos.x - size / 2f, glowAltitude, drawPos.z - size / 2f),
                Quaternion.identity,
                new Vector3(size, 1f, size)
            );
            Graphics.DrawMesh(_pawnGlowMesh, matrix, _pawnGlowMat, 0, null, 0, _pawnGlowMpb);
        }
    }

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private static Mesh BuildQuadMesh(float sizeX, float sizeZ)
    {
        var mesh = new Mesh();
        mesh.SetVertices(
            new List<Vector3>
            {
                new(0f, 0f, 0f),
                new(0f, 0f, sizeZ),
                new(sizeX, 0f, sizeZ),
                new(sizeX, 0f, 0f),
            }
        );
        mesh.SetUVs(
            0,
            new List<Vector2>
            {
                new(0f, 0f),
                new(0f, 1f),
                new(1f, 1f),
                new(1f, 0f),
            }
        );
        mesh.SetTriangles(new List<int> { 0, 1, 2, 0, 2, 3 }, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void EnsureAssets()
    {
        if (_quadMesh != null)
            return;

        _quadMesh = BuildQuadMesh(map.Size.x, map.Size.z);
        _blackMat = MaterialPool.MatFrom(BaseContent.BlackTex, ShaderDatabase.MetaOverlay, Color.white);

        _pawnGlowMesh = BuildQuadMesh(1f, 1f);
        Texture2D glowTex = BuildRadialGlowTexture();
        _pawnGlowMat = MaterialPool.MatFrom(glowTex, ShaderDatabase.MetaOverlay, Color.white);
        _pawnGlowMpb = new MaterialPropertyBlock();
    }

    private static Texture2D BuildRadialGlowTexture()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float falloff = Mathf.Clamp01(1f - dist / maxDist);
                falloff *= falloff;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, falloff));
            }
        }

        tex.Apply();
        return tex;
    }

    private static Color MagmaColor(float intensity)
    {
        float t = Mathf.Clamp01(intensity);
        for (int i = 0; i < MagmaStops.Length - 1; i++)
        {
            (float t0, Color c0) = MagmaStops[i];
            (float t1, Color c1) = MagmaStops[i + 1];
            if (t > t1)
                continue;

            float local = t1 > t0 ? (t - t0) / (t1 - t0) : 0f;
            Color rgb = Color.Lerp(c0, c1, local);
            return new Color(rgb.r, rgb.g, rgb.b, 1f);
        }

        (float _, Color last) = MagmaStops[MagmaStops.Length - 1];
        return new Color(last.r, last.g, last.b, 1f);
    }

    public override void MapRemoved()
    {
        base.MapRemoved();
        if (_quadMesh != null)
        {
            Object.Destroy(_quadMesh);
            _quadMesh = null;
        }
        if (_pawnGlowMesh != null)
        {
            Object.Destroy(_pawnGlowMesh);
            _pawnGlowMesh = null;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Active, "mssfpEcholocationActive");
        Scribe_Values.Look(ref EndTick, "mssfpEcholocationEndTick");
        Scribe_References.Look(ref Caster, "mssfpEcholocationCaster");
    }
}

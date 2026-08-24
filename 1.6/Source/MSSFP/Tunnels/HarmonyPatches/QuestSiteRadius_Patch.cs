using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Shared helper for the quest-site-radius patches.
/// Converts the QuestSiteRadiusStep setting index into an expanded max distance.
///
/// NOTE: unlike every other patch in this file, these patches are intentionally NOT
/// gated on <see cref="TunnelUtilities.IsEnabled"/> — quest-site radius is a global
/// MSSFP setting orthogonal to the tunnel system, not a tunnel-specific behavior.
/// The vanilla-parity sentinel is <c>step &lt;= 0</c> (ported unchanged from DFP): when
/// the step is at or below that sentinel, ExpandMax/ExpandMin hand back the untouched
/// vanilla value. Whether that sentinel is actually reached at MSSFPMod's default
/// QuestSiteRadiusStep predates this commit (set in the tunnel-port scaffold) and is
/// out of scope here — this file only ports the DFP patch logic and rebrands types.
/// </summary>
public static class QuestSiteRadiusHelper
{
    // Parallel to the UI step labels: Vanilla (sentinel), 4×, 8×, 16×, Unlimited
    private static readonly int[] Multipliers = { 0, 4, 8, 16, int.MaxValue };

    // Min distance caps at 16×; no Unlimited (an infinite minimum would block all spawns).
    private static readonly int[] MinMultipliers = { 0, 4, 8, 16 };

    /// <summary>
    /// Returns the expanded max distance, or the original value if the setting is Vanilla (step 0).
    /// Callable directly from injected IL.
    /// </summary>
    public static int ExpandMax(int vanillaMax)
    {
        int step = MSSFPMod.settings.QuestSiteRadiusStep;
        if (step <= 0) return vanillaMax;
        step = Math.Min(step, Multipliers.Length - 1);
        int multiplier = Multipliers[step];
        if (multiplier == int.MaxValue) return int.MaxValue;
        return vanillaMax * multiplier;
    }

    /// <summary>
    /// Returns the expanded min distance, or the original value if the setting is Vanilla (step 0).
    /// Callable directly from injected IL.
    /// </summary>
    public static int ExpandMin(int vanillaMin)
    {
        int step = MSSFPMod.settings.QuestSiteMinRadiusStep;
        if (step <= 0) return vanillaMin;
        step = Math.Min(step, MinMultipliers.Length - 1);
        return vanillaMin * MinMultipliers[step];
    }

    /// <summary>
    /// Doubles max without overflowing when max is already int.MaxValue (Unlimited sentinel).
    /// Used to guard the trueMax * 2 desperate-pass computation in Patch B.
    /// </summary>
    public static int SafeDoubleMax(int max)
        => max >= int.MaxValue / 2 ? int.MaxValue : max * 2;
}

// ── Patch A — TileFinder.TryFindNewSiteTile (generic quests + missions) ──────────────────
//
// Prefix on the 13-param workhorse overload. The 12-param forwarding overload delegates
// here, so a single Prefix covers both.
//
// The Archonexus Victory (3rd cycle) site uses the same code path but must stay within
// vanilla range. A companion Prefix/Postfix pair sets a thread-static suppression flag
// around that caller so the expansion is skipped.

[HarmonyPatch]
public static class TileFinder_TryFindNewSiteTile_Patch
{
    // Thread-local suppression — set by excluded callers before they invoke the target.
    [ThreadStatic]
    private static bool _suppressExpansion;

    public static void SetSuppressed() => _suppressExpansion = true;
    public static void ClearSuppressed() => _suppressExpansion = false;

    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
        => typeof(TileFinder).GetMethod(
            nameof(TileFinder.TryFindNewSiteTile),
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[]
            {
                typeof(PlanetTile).MakeByRefType(), // out PlanetTile tile
                typeof(PlanetTile),                  // PlanetTile nearTile
                typeof(int),                         // int minDist
                typeof(int),                         // int maxDist
                typeof(bool),                        // bool allowCaravans
                typeof(List<LandmarkDef>),           // List<LandmarkDef> allowedLandmarks
                typeof(float),                       // float selectLandmarkChance
                typeof(bool),                        // bool canSelectComboLandmarks
                typeof(TileFinderMode),              // TileFinderMode tileFinderMode
                typeof(bool),                        // bool exitOnFirstTileFound
                typeof(bool),                        // bool canBeSpace
                typeof(PlanetLayer),                 // PlanetLayer layer
                typeof(Predicate<PlanetTile>),       // Predicate<PlanetTile> validator
            },
            null);

    [HarmonyPrefix]
    public static void Prefix(ref int minDist, ref int maxDist)
    {
        if (_suppressExpansion) return;
        minDist = QuestSiteRadiusHelper.ExpandMin(minDist);
        maxDist = QuestSiteRadiusHelper.ExpandMax(maxDist);
    }
}

// ── Archonexus Victory (3rd cycle) exclusion ─────────────────────────────────────────────
//
// The endgame Archonexus victory site must stay within vanilla range regardless of the
// setting; forcing the player to cross the planet to complete the victory condition would
// be a terrible experience.

[HarmonyPatch(typeof(QuestNode_Root_ArchonexusVictory_ThirdCycle), "TryFindSiteTile")]
public static class ArchonexusVictory_TryFindSiteTile_ExclusionPatch
{
    [HarmonyPrefix]
    public static void Prefix() => TileFinder_TryFindNewSiteTile_Patch.SetSuppressed();

    [HarmonyPostfix]
    public static void Postfix() => TileFinder_TryFindNewSiteTile_Patch.ClearSuppressed();
}

// ── Patch B — QuestNode_Root_Gravcore.TryFindSiteTile (Odyssey gravcore quests) ──────────
//
// Transpiler (not Prefix) because the base class instance field distanceFromColonyRange
// would be mutated by a Prefix, and both TestRunInt and RunInt call TryFindSiteTile on
// the same instance — a Prefix would fire twice and compound the multiplier (e.g. 4× → 16×).
// The Transpiler operates on a local copy so there is no mutation.
//
// Two injection sites:
//   1. After get_TrueMax: inject call ExpandMax so the trueMax local holds the expanded value.
//   2. Replace ldc.i4.2 + mul (trueMax * 2 in the desperate pass) with call SafeDoubleMax
//      to guard against int.MaxValue overflow when the Unlimited setting is active.
//
// None of the 11 known Gravcore subclasses override TryFindSiteTile, so the single
// base-class Transpiler covers all of them.

[HarmonyPatch(typeof(QuestNode_Root_Gravcore), "TryFindSiteTile")]
public static class QuestNode_Root_Gravcore_TryFindSiteTile_Patch
{
    private static readonly MethodInfo _getTrueMin =
        AccessTools.PropertyGetter(typeof(IntRange), nameof(IntRange.TrueMin));

    private static readonly MethodInfo _getTrueMax =
        AccessTools.PropertyGetter(typeof(IntRange), nameof(IntRange.TrueMax));

    private static readonly MethodInfo _expandMin =
        AccessTools.Method(typeof(QuestSiteRadiusHelper), nameof(QuestSiteRadiusHelper.ExpandMin));

    private static readonly MethodInfo _expandMax =
        AccessTools.Method(typeof(QuestSiteRadiusHelper), nameof(QuestSiteRadiusHelper.ExpandMax));

    private static readonly MethodInfo _safeDoubleMax =
        AccessTools.Method(typeof(QuestSiteRadiusHelper), nameof(QuestSiteRadiusHelper.SafeDoubleMax));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            // Inject ExpandMin after the single get_TrueMin call.
            // The desperate passes hardcode 1f for minDist, so they are unaffected.
            if (codes[i].opcode == OpCodes.Call
                && codes[i].operand is MethodInfo getTrueMin
                && getTrueMin == _getTrueMin)
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, _expandMin));
                i++; // step past the injected instruction
                continue;
            }

            // Inject ExpandMax after the single get_TrueMax call.
            if (codes[i].opcode == OpCodes.Call
                && codes[i].operand is MethodInfo getTrueMax
                && getTrueMax == _getTrueMax)
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, _expandMax));
                i++; // step past the injected instruction
                continue;
            }

            // Replace ldc.i4.2 + mul (trueMax * 2) with SafeDoubleMax to guard
            // int.MaxValue overflow when the Unlimited setting is active.
            if (codes[i].opcode == OpCodes.Ldc_I4_2
                && i + 1 < codes.Count
                && codes[i + 1].opcode == OpCodes.Mul)
            {
                codes[i] = new CodeInstruction(OpCodes.Call, _safeDoubleMax);
                codes.RemoveAt(i + 1); // remove mul
                continue;
            }
        }

        return codes;
    }
}

// ── Patch C — QuestNode_Root_Site.TryFindSiteTile (XML-defined quests) ───────────────────
//
// Transpiler (not Prefix) because SlateRef<IntRange> makes a Prefix + reflection approach
// unwieldy — the backing value lives in the Slate context, not a simple instance field.
//
// The method has two independent get_TrueMax reads:
//   Read 1: int trueMax = distanceFromColonyRange.GetValue(slate).TrueMax;
//           (used in the main biome-constrained query)
//   Read 2: (float)distanceFromColonyRange.GetValue(slate).TrueMax
//           (used in the desperate fallback when desperateIgnoreDistance is false)
//
// Patching only Read 1 would leave the desperate fallback at the vanilla cap, silently
// making the setting ineffective in biome-poor regions. Both reads get ExpandMax injected.
//
// The two call sites are distinguishable by their successor instructions:
//   Read 1: get_TrueMax → stloc (int local)
//   Read 2: get_TrueMax → conv.r4 (int-to-float for maxDistTiles)
// Injecting after every get_TrueMax handles both uniformly.

[HarmonyPatch(typeof(QuestNode_Root_Site), "TryFindSiteTile")]
public static class QuestNode_Root_Site_TryFindSiteTile_Patch
{
    private static readonly MethodInfo _getTrueMin =
        AccessTools.PropertyGetter(typeof(IntRange), nameof(IntRange.TrueMin));

    private static readonly MethodInfo _getTrueMax =
        AccessTools.PropertyGetter(typeof(IntRange), nameof(IntRange.TrueMax));

    private static readonly MethodInfo _expandMin =
        AccessTools.Method(typeof(QuestSiteRadiusHelper), nameof(QuestSiteRadiusHelper.ExpandMin));

    private static readonly MethodInfo _expandMax =
        AccessTools.Method(typeof(QuestSiteRadiusHelper), nameof(QuestSiteRadiusHelper.ExpandMax));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            // Inject ExpandMin after every get_TrueMin call.
            // Read 1: trueMin local (main query). Read 2: num local (desperate fallback,
            // inside a ternary — only fires when desperateIgnoreDistance is false).
            if (codes[i].opcode == OpCodes.Call
                && codes[i].operand is MethodInfo getTrueMin
                && getTrueMin == _getTrueMin)
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, _expandMin));
                i++; // step past the injected instruction
                continue;
            }

            // Inject ExpandMax after every get_TrueMax call (both Read 1 and Read 2).
            if (codes[i].opcode == OpCodes.Call
                && codes[i].operand is MethodInfo getTrueMax
                && getTrueMax == _getTrueMax)
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, _expandMax));
                i++; // step past the injected instruction
                continue;
            }
        }

        return codes;
    }
}

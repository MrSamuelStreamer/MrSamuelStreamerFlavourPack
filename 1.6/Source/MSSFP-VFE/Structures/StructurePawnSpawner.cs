using MSSFP.ModExtensions;
using MSSFP.PawnPortability.Import;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.VFE.Structures;

/// <summary>
/// Places the pawns and corpses a structure was exported with. Live pawns defend the structure as
/// one lord. Dead entries are spawned and then killed so the game makes the corpse; they never join
/// a lord. Every entry is isolated so one bad template cannot stop loot and the rest from spawning.
/// </summary>
public static class StructurePawnSpawner
{
    public static void Spawn(StructureDefModExtension ext, Map map, CellRect rect, int assaultDelayTicks)
    {
        Faction faction = ext.anyHostile ? Faction.OfAncientsHostile : Faction.OfAncients;
        Lord lord = null;

        foreach (StructurePawnEntry entry in ext.pawnTemplates)
        {
            try
            {
                if (entry.template == null)
                {
                    ModLog.Warn("StructurePawnSpawner: entry has no template (missing mod?), skipping");
                    continue;
                }

                if (!TryResolveCell(entry, map, rect, out IntVec3 cell))
                    continue;

                if (entry.dead)
                {
                    SpawnCorpse(entry, cell, map, faction);
                    continue;
                }

                Pawn pawn = PawnTemplateSpawner.Spawn(entry.template, cell, map, faction);
                if (pawn == null)
                    continue;

                lord ??= LordMaker.MakeNewLord(
                    faction,
                    new LordJob_DefendBase(faction, rect.CenterCell, assaultDelayTicks, false),
                    map
                );
                lord.AddPawn(pawn);
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"StructurePawnSpawner: failed to spawn {entry.template?.defName}", ex);
            }
        }
    }

    private static bool TryResolveCell(StructurePawnEntry entry, Map map, CellRect rect, out IntVec3 cell)
    {
        cell = new IntVec3(rect.minX + entry.offset.x, 0, rect.minZ + entry.offset.z);
        if (cell.InBounds(map) && cell.Standable(map))
            return true;

        ModLog.Warn($"StructurePawnSpawner: {entry.template?.defName} cell {cell} is out of bounds or blocked, skipping");
        return false;
    }

    private static void SpawnCorpse(StructurePawnEntry entry, IntVec3 cell, Map map, Faction faction)
    {
        Pawn pawn = PawnTemplateSpawner.Spawn(entry.template, cell, map, faction);
        if (pawn == null)
            return;

        pawn.Kill(null);

        Corpse corpse = pawn.Corpse;
        if (!pawn.Dead || corpse == null || !corpse.Spawned)
        {
            ModLog.Warn($"StructurePawnSpawner: {entry.template.defName} did not leave a spawned corpse, removing");
            if (!pawn.Destroyed)
                pawn.Destroy();
            return;
        }

        if (entry.rotten)
            MakeRotten(corpse);
    }

    private static void MakeRotten(Corpse corpse)
    {
        CompRottable rot = corpse.GetComp<CompRottable>();
        if (rot == null)
            return;

        rot.RotProgress = rot.PropsRot.TicksToRotStart + 1;
    }
}

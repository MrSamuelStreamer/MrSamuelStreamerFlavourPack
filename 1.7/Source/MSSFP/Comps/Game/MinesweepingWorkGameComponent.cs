using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Game;

/// <summary>
///     New WorkTypeDefs load at priority 0 on colonists that already existed in a
///     save — <c>alwaysStartActive</c> only affects newly generated pawns — so an
///     existing colony would silently never pick up <see cref="MSSFPDefOf.MSSFP_Minesweeping" />.
///     Runs a one-time migration on <see cref="FinalizeInit" /> that enables it at
///     default priority 3 for every existing colonist, then never runs again.
/// </summary>
public class MinesweepingWorkGameComponent(Verse.Game game) : GameComponent
{
    public Verse.Game Game { get; } = game;

    private bool migrated;

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (migrated) return;
        migrated = true;

        foreach (Pawn pawn in AllColonistsEverPossible())
        {
            if (pawn.workSettings == null || !pawn.workSettings.EverWork) continue;
            if (pawn.WorkTypeIsDisabled(MSSFPDefOf.MSSFP_Minesweeping)) continue;

            pawn.workSettings.SetPriority(MSSFPDefOf.MSSFP_Minesweeping, 3);
        }
    }

    private static IEnumerable<Pawn> AllColonistsEverPossible()
    {
        foreach (Verse.Map map in Find.Maps)
        {
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                yield return pawn;
            }
        }

        foreach (Pawn pawn in Find.WorldPawns.AllPawnsAlive)
        {
            if (pawn.IsColonist)
            {
                yield return pawn;
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref migrated, "minesweepingMigrated");
    }
}

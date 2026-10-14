using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace MSSFP.Tunnels;

public class FloatMenuOptionProvider_EnterTunnel : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => true;

    protected override bool MechanoidCanDo => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(
        Thing clickedThing,
        FloatMenuContext context)
    {
        if (!TunnelUtilities.IsEnabled())
            yield break;

        Building_Tunnel tunnel = clickedThing as Building_Tunnel;
        if (tunnel == null)
            yield break;

        FloatMenuOption singleOptionFor = this.GetSingleOptionFor(clickedThing, context);
        if (singleOptionFor != null)
            yield return singleOptionFor;

        if (!tunnel.IsEnterable(out string reason))
        {
            yield return new FloatMenuOption("CannotEnterPortal".Translate((NamedArgument)tunnel.Label) + ": " + reason, null);
            yield break;
        }

        if (!context.IsMultiselect)
        {
            AcceptanceReport acceptanceReport = CanEnterTunnel(context.FirstSelectedPawn, tunnel);
            if (!acceptanceReport.Accepted)
            {
                yield return new FloatMenuOption("CannotEnterPortal".Translate((NamedArgument)tunnel.Label) + ": " + acceptanceReport.Reason, null);
                yield break;
            }
        }

        List<Pawn> selectedPawns = new List<Pawn>();
        foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
        {
            if (CanEnterTunnel(validSelectedPawn, tunnel).Accepted)
                selectedPawns.Add(validSelectedPawn);
        }

        if (selectedPawns.NullOrEmpty())
            yield break;

        if (tunnel.destination != PlanetTile.Invalid)
        {
            yield return new FloatMenuOption(tunnel.EnterString, () =>
            {
                SendToTunnel(tunnel, selectedPawns);
            });
        }
        else
        {
            HashSet<int> reachableTiles = TunnelGenData.Instance.GetReachableTilesFrom(tunnel.Tile);
            List<WorldObject> wos = TunnelGenData.WorldObjectsWithTunnelEntrances()
                .Where(wo => wo != null && wo.Tile != tunnel.Map.Tile && reachableTiles.Contains(wo.Tile))
                .ToList();

            foreach (WorldObject worldObject in wos)
            {
                float distance = Find.WorldGrid.ApproxDistanceInTiles(tunnel.Tile, worldObject.Tile);

                yield return new FloatMenuOption(worldObject.LabelCap + " [" + distance.ToStringDecimalIfSmall() + " tiles]", () =>
                {
                    Find.WindowStack.Add(new Dialog_EnterTunnel(tunnel, worldObject, selectedPawns));
                });
            }
        }
    }

    public void SendToTunnel(Building_Tunnel tunnel, List<Pawn> pawns)
    {
        foreach (Pawn pawn in pawns)
        {
            Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_EnterTunnel, (LocalTargetInfo)(Thing)tunnel);
            job.playerForced = true;
            pawn.jobs.TryTakeOrderedJob(job);
        }
    }

    private static AcceptanceReport CanEnterTunnel(Pawn pawn, Building_Tunnel tunnel)
    {
        if (!pawn.CanReach((LocalTargetInfo)(Thing)tunnel, PathEndMode.ClosestTouch, Danger.Deadly))
            return "NoPath".Translate();
        return !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ? "Incapable".Translate() : true;
    }
}

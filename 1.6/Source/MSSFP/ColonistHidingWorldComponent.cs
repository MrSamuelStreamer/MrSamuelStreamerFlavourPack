using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace MSSFP;

public class ColonistHidingWorldComponent : WorldComponent
{
    public HashSet<Pawn> HiddenColonists = new();

    public ColonistHidingWorldComponent(World world)
        : base(world) { }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref HiddenColonists, "HiddenColonists", LookMode.Reference);
    }

    public bool IsHidden(Pawn pawn)
    {
        return HiddenColonists?.Contains(pawn) == true;
    }

    public void HideColonist(Pawn pawn)
    {
        HiddenColonists ??= new HashSet<Pawn>();
        HiddenColonists.Add(pawn);
    }

    public void ShowColonist(Pawn pawn)
    {
        HiddenColonists?.Remove(pawn);
    }

    public List<Pawn> GetHiddenColonists()
    {
        return HiddenColonists?.Where(p => p != null && !p.Destroyed).ToList() ?? new List<Pawn>();
    }
}

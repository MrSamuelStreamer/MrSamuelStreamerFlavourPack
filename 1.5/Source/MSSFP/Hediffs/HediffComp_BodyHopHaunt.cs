using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

[StaticConstructorOnStartup]
public class HediffComp_BodyHopHaunt: HediffComp_Haunt
{
    public class PawnInfo: IExposable
    {
        public string name;
        public string description;
        public SkillDef bestSkill;
        public int skillOffset;
        public List<TraitDef> passedTraits;
        public int swapTick;

        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref this.name, "name");
            Scribe_Values.Look<string>(ref this.description, "description");
            Scribe_Defs.Look<SkillDef>(ref this.bestSkill, "bestSkill");
            Scribe_Values.Look<int>(ref this.skillOffset, "skillOffset");
            Scribe_Collections.Look(ref this.passedTraits, "passedTraits", LookMode.Def);
            Scribe_Values.Look<int>(ref this.swapTick, "swapTick");
        }
    }

    public List<PawnInfo> pawns = new List<PawnInfo>();

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Collections.Look<PawnInfo>(ref pawns, "pawns", LookMode.Deep);
    }

    public virtual void AddNewPawn(
        string name,
        string description,
        SkillDef bestSkill,
        int skillOffset,
        List<TraitDef> passedTraits,
        int swapTick = -1)
    {
        if (swapTick < 0) swapTick = Find.TickManager.TicksGame;

        pawns.Add(new PawnInfo()
        {
            name = name,
            description = description,
            bestSkill = bestSkill,
            skillOffset = skillOffset,
            passedTraits = passedTraits,
            swapTick = swapTick
        });
    }


    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }

        Command_Action showPawn = new Command_Action();
        showPawn.defaultLabel = "Add New Pawn History";
        showPawn.icon = icon;
        showPawn.action = delegate
        {
            Find.WindowStack.Add(new PawnHistoryEditorWindow(this));
        };

        yield return showPawn;
    }
}

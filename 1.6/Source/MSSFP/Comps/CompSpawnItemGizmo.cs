using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps
{
    public class CompProperties_SpawnItemGizmo : CompProperties
    {
        public ThingDef spawnItem;
        public int count = 1;
        public string label;
        public string description;
        public string iconPath;
        public int offsetX;
        public int offsetY;
        public int cooldownTicks = 0;

        public CompProperties_SpawnItemGizmo()
        {
            compClass = typeof(CompSpawnItemGizmo);
        }
    }

    public class CompSpawnItemGizmo : ThingComp
    {
        private CompProperties_SpawnItemGizmo Props => (CompProperties_SpawnItemGizmo)props;

        private int nextUseTick = -1;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextUseTick, "nextUseTick", -1);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (Props.spawnItem == null)
                yield break;

            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Reset itemspawn cooldown",
                    action = () => nextUseTick = -1
                };
            }

            int ticksLeft = Mathf.Max(0, nextUseTick - Find.TickManager.TicksGame);

            Command_Action command = new Command_Action
            {
                defaultLabel = Props.label,
                defaultDesc = IsOnCooldown() && Find.TickManager != null
                    ? $"{Props.description}\nCooldown: {ticksLeft.ToStringTicksToPeriod()} remaining"
                    : Props.description,
                icon = ContentFinder<Texture2D>.Get(Props.iconPath, true),
                action = SpawnItem
            };

            if (IsOnCooldown())
            {
                command.Disable("Not yet ready.");
            }

            yield return command;
        }

        private bool IsOnCooldown()
        {
            return Props.cooldownTicks > 0
                && Find.TickManager != null
                && Find.TickManager.TicksGame < nextUseTick;
        }

        private void SpawnItem()
        {
            if (Props.spawnItem == null)
                return;

            if (IsOnCooldown())
                return;

            Thing thing = ThingMaker.MakeThing(Props.spawnItem);
            thing.stackCount = Mathf.Max(1, Props.count);

            IntVec3 cell = parent.Position + new IntVec3(Props.offsetX, 0, Props.offsetY);

            if (!GenPlace.TryPlaceThing(thing, cell, parent.Map, ThingPlaceMode.Near))
            {
                thing.Destroy(DestroyMode.Vanish);
                return;
            }

            if (Props.cooldownTicks > 0 && Find.TickManager != null)
            {
                nextUseTick = Find.TickManager.TicksGame + Props.cooldownTicks;
            }
        }
    }
}

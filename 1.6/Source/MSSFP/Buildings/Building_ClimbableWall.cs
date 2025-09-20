using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Buildings
{
    public class Building_ClimbableWallProxy : Building
    {
        private Building originalWall;
        private bool isClimbable = false;
        private int ticksRemaining = 0;
        private Color originalColor;

        public bool IsClimbable => isClimbable;

        public void InitializeProxy(Building wall)
        {
            originalWall = wall;

            var originalDef = wall.def;
            var customDef = new ThingDef();

            customDef.defName = originalDef.defName + "_ClimbableProxy";
            customDef.label = originalDef.label;
            customDef.description = originalDef.description;
            customDef.thingClass = originalDef.thingClass;
            customDef.category = originalDef.category;
            customDef.graphicData = originalDef.graphicData;
            customDef.size = originalDef.size;
            customDef.statBases = originalDef.statBases;
            customDef.fillPercent = originalDef.fillPercent;
            customDef.blockWind = originalDef.blockWind;
            customDef.holdsRoof = originalDef.holdsRoof;
            customDef.castEdgeShadows = originalDef.castEdgeShadows;
            customDef.blockLight = originalDef.blockLight;
            customDef.altitudeLayer = originalDef.altitudeLayer;
            customDef.building = originalDef.building;
            customDef.comps = originalDef.comps;
            customDef.stuffCategories = originalDef.stuffCategories;
            customDef.selectable = originalDef.selectable;
            customDef.hasTooltip = originalDef.hasTooltip;
            customDef.inspectorTabs = originalDef.inspectorTabs;
            customDef.inspectorTabsResolved = originalDef.inspectorTabsResolved;
            customDef.drawGUIOverlay = originalDef.drawGUIOverlay;
            customDef.hideStats = originalDef.hideStats;
            customDef.hideInspect = originalDef.hideInspect;
            customDef.uiIconPath = originalDef.uiIconPath;
            customDef.drawerType = originalDef.drawerType;

            customDef.passability = Traversability.PassThroughOnly;
            customDef.pathCost = 80;
            customDef.tickerType = TickerType.Normal;

            this.def = customDef;
            this.Position = wall.Position;
            this.Rotation = wall.Rotation;
            this.HitPoints = wall.HitPoints;
            this.SetFactionDirect(wall.Faction);

            if (wall.Stuff != null)
                this.SetStuffDirect(wall.Stuff);

            if (wall.StyleDef != null)
                this.SetStyleDef(wall.StyleDef);

            this.AllComps.Clear();
            foreach (var comp in wall.AllComps)
            {
                this.AllComps.Add(comp);
                comp.parent = this;
            }

            originalColor = wall.Graphic.color;

            if (wall.MaxHitPoints != this.MaxHitPoints)
                this.HitPoints = wall.HitPoints;
        }

        public void MakeClimbable(int ticks)
        {
            isClimbable = true;
            ticksRemaining = ticks;

            var tintedColor = Color.Lerp(originalColor, Color.cyan, 0.3f);
            this.Graphic.color = tintedColor;

            Map.pathing.RecalculatePerceivedPathCostAt(Position);
            Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Buildings);
            Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
        }

        public void RestoreWall()
        {
            isClimbable = false;
            ticksRemaining = 0;
            this.Graphic.color = originalColor;
            Map.pathing.RecalculatePerceivedPathCostAt(Position);
            SwapBackToOriginal();
        }

        protected override void Tick()
        {
            base.Tick();

            if (isClimbable && ticksRemaining > 0)
            {
                ticksRemaining--;
                if (ticksRemaining <= 0)
                    RestoreWall();
            }
        }

        public override ushort PathWalkCostFor(Pawn p)
        {
            return isClimbable ? (ushort)80 : base.PathWalkCostFor(p);
        }

        public override bool IsDangerousFor(Pawn p)
        {
            return isClimbable ? false : base.IsDangerousFor(p);
        }

        private void SwapBackToOriginal()
        {
            var position = Position;
            var map = Map;

            map.edificeGrid.DeRegister(this);
            map.edificeGrid.Register(originalWall);
            map.thingGrid.Deregister(this);
            map.thingGrid.Register(originalWall);
            map.listerBuildings.Remove(this);
            map.listerBuildings.Add(originalWall);

            foreach (var comp in AllComps)
                comp.parent = originalWall;

            map.mapDrawer.MapMeshDirty(position, MapMeshFlagDefOf.Buildings);
            map.mapDrawer.MapMeshDirty(position, MapMeshFlagDefOf.Things);

            Destroy(DestroyMode.Vanish);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isClimbable, "isClimbable", false);
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
            Scribe_References.Look(ref originalWall, "originalWall");
        }

        public override string GetInspectString()
        {
            var baseString = base.GetInspectString();

            if (isClimbable && ticksRemaining > 0)
            {
                var minutesLeft = Mathf.CeilToInt(ticksRemaining / 60f / 60f);
                var climbableString = "MSS_SiegeLadder_ClimbableTime".Translate(minutesLeft);
                return baseString.NullOrEmpty()
                    ? climbableString
                    : baseString + "\n" + climbableString;
            }

            return baseString;
        }
    }
}

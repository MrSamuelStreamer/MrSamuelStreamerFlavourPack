using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class Settings : ModSettings
{
    public bool MabelDestroyFloors = false;
    public bool OverrideRelicPool = false;
    public bool DisableFroggeNom = false;
    public bool EnableMogus = false;
    public bool ShowHaunts = false;
    public bool EnableSkylanternRaids = false;
    public bool DrawByMrStreamer = false;
    public bool EnableGenesOnGrowthMoment = false;
    public float GeneEventChance = 1f;
    public float GoodGeneChance = 1f / 4f;
    public float BadGeneChance = 1f / 4f;
    public float NeutralGeneChance = 1f / 4f;
    public float RandomGeneChance = 1f / 4f;
    public bool EnableOutpostFission = false;
    public int DaysForOutpostFission = 15;
    public int DaysForFission = 7;
    public bool EnableLoversRetreat = false;
    public bool EnableFroggeIncidents = false;
    public bool SingleUseMentalFuses = false;
    public bool EnablePossession = false;
    public bool DisableBSIncorporateGeneLimit = false;
    public bool EnableNonsenseIncidents = false;
    public bool EnableGeneStealerNeed = false;
    public bool EnableDirtJobs = false;
    public bool EnableOskarianTech = false;
    public bool EnableGeneMutators = false;
    public bool EnableTrekBeamers = true;
    public bool EnableTaffRaids = true;
    public bool EnableWanderDelayModification = false;
    public int WanderDelayTicks = 0;

    protected static List<SettingsTab> Tabs = new();

    public static void RegisterTab(SettingsTab tab)
    {
        Tabs.Add(tab);
        Tabs = Tabs.OrderBy(t => t.TabOrder).ToList();
    }

    public SettingsTab DefaultTab => Tabs.FirstOrDefault(t => t.IsDefault);

    private float ScrollViewWidth = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public SettingsTab SelectedTab;

    public static Lazy<MethodInfo> _TempGuiContent = new(() => typeof(GUIContent).GetMethod("Temp", BindingFlags.NonPublic | BindingFlags.Static, null, [typeof(string)], null));

    public static GUIContent TempGuiContent(string t) => _TempGuiContent.Value.Invoke(null, [t]) as GUIContent;

    public static Color TabRectColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    public void DoWindowContents(Rect wrect)
    {
        float tabsHeight = 30 + 16;
        Rect tabButtonScrollerRect = new(wrect.x, wrect.y, wrect.width, tabsHeight);
        Rect tabScrollContainerRect = new(wrect.x, wrect.y, ScrollViewWidth, 30);

#if DEBUG
        Widgets.DrawRectFast(tabButtonScrollerRect, Color.red);
        Widgets.DrawRectFast(tabScrollContainerRect, Color.green);
#endif
        ScrollViewWidth = 0;
        scrollPosition = GUI.BeginScrollView(tabButtonScrollerRect, scrollPosition, tabScrollContainerRect);

        foreach (SettingsTab tab in Tabs)
        {
            // Unicode? No, I don't need code thanks, I've just written it
            // This assumes all chars are fixed width, Unicode modifiers and variable width fonts be damned.
            // It's "good enough" for a tab button that doesn't take user input (beyond a click) and probably won't be localized
            float width = 4 + 4 + (tab.TabName.Length * 12);

            Rect buttonRect = new(tabScrollContainerRect.x + ScrollViewWidth, tabScrollContainerRect.y, width, 30);

            if (SelectedTab == tab)
            {
                TextAnchor anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.DrawAtlas(buttonRect, Widgets.ButtonBGAtlasClick);
                Widgets.Label(buttonRect, tab.TabName);
                Text.Anchor = anchor;
            }
            else
            {
                if (Widgets.ButtonText(buttonRect, tab.TabName))
                {
                    SelectedTab = tab;
                }
            }

            ScrollViewWidth += buttonRect.width + 2;
        }

        GUI.EndScrollView();

        Widgets.DrawLineHorizontal(0, wrect.yMin + tabsHeight + 1, wrect.width, Color.grey);
        Widgets.DrawLineHorizontal(0, wrect.yMin + tabsHeight + 2, wrect.width, Color.grey);

        Rect tabRect = new(tabButtonScrollerRect.xMin, wrect.yMin + tabsHeight + 3, wrect.width, wrect.height - tabButtonScrollerRect.height - 3);

        Widgets.DrawRectFast(tabRect, TabRectColor);

        SelectedTab ??= DefaultTab;

        SelectedTab.DrawTab(tabRect);
    }

    public override void ExposeData()
    {
        foreach (SettingsTab settingsTab in Tabs)
        {
            settingsTab.ExposeData();
        }
    }
}

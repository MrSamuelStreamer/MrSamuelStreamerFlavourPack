using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public abstract class SettingsTab
{
    public ModSettings settings;
    public Mod mod;

    public static List<Action> PostSaveActions = new List<Action>();

    public SettingsTab(ModSettings settings, Mod mod)
    {
        this.settings = settings;
        this.mod = mod;
    }

    public virtual string TabName => "Settings Tab";
    public virtual bool IsDefault => false;
    public virtual int TabOrder => 0;
    private float ScrollViewHeight = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public static List<Type> Tabs =>
        AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(SettingsTab)))
            .ToList();

    public virtual void ExposeData() { }

    public void DrawTab(Rect tabRect)
    {
        Rect contentScrollContainerRect = new(
            tabRect.xMin,
            tabRect.yMin,
            tabRect.width,
            Mathf.Max(ScrollViewHeight, tabRect.height)
        );
#if DEBUG
        Widgets.DrawRectFast(contentScrollContainerRect, Color.blue);
#endif
        ScrollViewHeight = 0;
        scrollPosition = GUI.BeginScrollView(tabRect, scrollPosition, contentScrollContainerRect);

        Listing_Standard options = new();
        options.Begin(contentScrollContainerRect.ContractedBy(2f));

        try
        {
            DoTabContents(options, contentScrollContainerRect, ref ScrollViewHeight);
        }
        catch (Exception e)
        {
            ModLog.Error($"Failed to draw tab contents for {GetType()}", e);
        }
        finally
        {
            GUI.EndScrollView();
            options.End();
        }
    }

    public void DrawCheckBox(Listing_Standard options, string label, ref bool value, ref float svh)
    {
        float height = Text.CalcHeight(label, options.ColumnWidth) + 12f;
        options.CheckboxLabeled(label, ref value, height: height);
        svh += height;
    }

    public void DrawIntAdjuster(
        Listing_Standard options,
        string label,
        ref int value,
        int countChange,
        int min,
        ref float svh
    )
    {
        svh += options.Label(label).height;
        options.IntAdjuster(ref value, countChange, min);
        svh += 24f + options.verticalSpacing;
    }

    public virtual void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    ) { }
}

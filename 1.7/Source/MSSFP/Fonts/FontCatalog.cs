using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MSSFP.Utils;
using UnityEngine;

namespace MSSFP.Fonts;

/// <summary>One selectable system font.</summary>
public readonly record struct FontEntry(string Family, string Style, string Path, long SizeBytes)
{
    public string Label => string.IsNullOrEmpty(Style) ? Family : $"{Family} — {Style}";
}

/// <summary>
/// The installed system fonts, found with the engine's own scanner
/// (<see cref="Font.GetPathsToOSFonts"/>) and labelled from each file's name table. Built once,
/// on first use, because reading a few thousand headers takes noticeable time.
///
/// Not listed: fonts without outlines (the rasteriser cannot draw UI text with them) and files
/// over <see cref="MaxFileBytes"/>. Every font a player tries stays in memory until restart
/// (the engine has no unregister call and keeps several copies), so huge files are not offered.
/// </summary>
public static class FontCatalog
{
    public const long MaxFileBytes = 15L * 1024 * 1024;

    private static List<FontEntry> entries;

    /// <summary>True once the scan has run.</summary>
    public static bool IsBuilt => entries != null;

    public static IReadOnlyList<FontEntry> Entries => entries ??= Build();

    /// <summary>The entry for a file path, or null when it is unlisted or the scan has not run.</summary>
    public static FontEntry? Find(string path)
    {
        if (entries == null)
            return null;
        foreach (FontEntry entry in entries)
        {
            if (entry.Path == path)
                return entry;
        }
        return null;
    }

    private static List<FontEntry> Build()
    {
        Stopwatch timer = Stopwatch.StartNew();
        string[] paths = Font.GetPathsToOSFonts();
        Array.Sort(paths, StringComparer.Ordinal);

        Dictionary<(string, string), FontEntry> byName = new();
        int unreadable = 0;
        int noOutlines = 0;
        int tooLarge = 0;
        int duplicates = 0;

        foreach (string path in paths)
        {
            long size = SizeOf(path);
            if (size < 0 || !OpenTypeNames.TryRead(path, out FontFileInfo info))
            {
                unreadable++;
                continue;
            }
            if (!info.HasOutlines)
            {
                noOutlines++;
                continue;
            }
            if (size > MaxFileBytes)
            {
                tooLarge++;
                continue;
            }

            string family = info.Family ?? Path.GetFileNameWithoutExtension(path);
            FontEntry entry = new(family, info.Style, path, size);
            if (!byName.TryAdd((family.ToLowerInvariant(), (info.Style ?? "").ToLowerInvariant()), entry))
                duplicates++;
        }

        List<FontEntry> result = byName
            .Values.OrderBy(e => e.Family, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Style, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ModLog.Log(
            $"[UiFont] Scanned {paths.Length} font files in {timer.ElapsedMilliseconds} ms: {result.Count} listed, "
                + $"{unreadable} unreadable, {noOutlines} without outlines, {tooLarge} over "
                + $"{MaxFileBytes / (1024 * 1024)} MB, {duplicates} duplicates."
        );
        return result;
    }

    private static long SizeOf(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return -1;
        }
    }
}

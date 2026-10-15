using System;
using System.IO;
using System.Text;

namespace MSSFP.Fonts;

/// <summary>What the font picker needs to know about one font file.</summary>
/// <param name="Family">Family name, or null when the name table could not be read.</param>
/// <param name="Style">Style name such as "Bold Italic", or null when unreadable.</param>
/// <param name="HasOutlines">
/// True when the file has TrueType (glyf) or CFF/CFF2 outlines. Bitmap-only and colour-only
/// fonts (e.g. emoji fonts) are false: they must not be offered, because the rasteriser cannot
/// draw UI text with them.
/// </param>
public readonly record struct FontFileInfo(string Family, string Style, bool HasOutlines);

/// <summary>
/// Minimal OpenType reader: table directory plus the 'name' table, nothing else. It seeks
/// instead of loading the file, because the catalogue reads thousands of files and some are
/// megabytes in size. For a TTC collection it reads face 0, which is also the face the engine
/// would use.
/// </summary>
public static class OpenTypeNames
{
    private const uint TtcTag = 0x74746366; // 'ttcf'
    private const uint TagGlyf = 0x676C7966; // 'glyf'
    private const uint TagCff = 0x43464620; // 'CFF '
    private const uint TagCff2 = 0x43464632; // 'CFF2'
    private const uint TagName = 0x6E616D65; // 'name'

    private const uint SfntTrueType = 0x00010000;
    private const uint SfntOpenType = 0x4F54544F; // 'OTTO'
    private const uint SfntApple = 0x74727565; // 'true'

    private const int MaxNameTableBytes = 1 << 20;

    private const ushort FamilyId = 1;
    private const ushort StyleId = 2;
    private const ushort TypographicFamilyId = 16;
    private const ushort TypographicStyleId = 17;

    /// <summary>
    /// Reads the font at <paramref name="path"/>. Returns false when the file is unreadable or
    /// is not a font. Returns true with null names when it is a font whose names are unusable.
    /// </summary>
    public static bool TryRead(string path, out FontFileInfo info)
    {
        info = default;
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return TryRead(stream, out info);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryRead(FileStream stream, out FontFileInfo info)
    {
        info = default;
        long length = stream.Length;
        if (length < 16)
            return false;

        long directory = 0;
        if (ReadU32(stream, 0) == TtcTag)
            directory = ReadU32(stream, 12);

        if (directory + 12 > length)
            return false;
        uint sfnt = ReadU32(stream, directory);
        if (sfnt != SfntTrueType && sfnt != SfntOpenType && sfnt != SfntApple)
            return false;

        int tableCount = ReadU16(stream, directory + 4);
        if (directory + 12 + tableCount * 16L > length)
            return false;

        bool outlines = false;
        long nameOffset = -1;
        long nameLength = 0;
        for (int i = 0; i < tableCount; i++)
        {
            long record = directory + 12 + i * 16L;
            uint tag = ReadU32(stream, record);
            switch (tag)
            {
                case TagGlyf:
                case TagCff:
                case TagCff2:
                    outlines = true;
                    break;
                case TagName:
                    nameOffset = ReadU32(stream, record + 8);
                    nameLength = ReadU32(stream, record + 12);
                    break;
            }
        }

        string family = null;
        string style = null;
        if (nameOffset >= 0 && nameLength >= 6 && nameLength <= MaxNameTableBytes && nameOffset + nameLength <= length)
        {
            byte[] table = new byte[nameLength];
            stream.Position = nameOffset;
            stream.ReadExactly(table);
            family = BestName(table, TypographicFamilyId) ?? BestName(table, FamilyId);
            style = BestName(table, TypographicStyleId) ?? BestName(table, StyleId);
        }

        info = new FontFileInfo(family, style, outlines);
        return true;
    }

    /// <summary>
    /// Picks the best record for a name id: Windows English first, then any Windows or Unicode
    /// record, then Macintosh Roman (decoded as Latin-1, which is exact for the ASCII names
    /// that matter here).
    /// </summary>
    private static string BestName(byte[] table, ushort nameId)
    {
        int count = U16(table, 2);
        int storage = U16(table, 4);

        string best = null;
        int bestRank = int.MaxValue;
        for (int i = 0; i < count; i++)
        {
            int record = 6 + i * 12;
            if (record + 12 > table.Length)
                break;
            if (U16(table, record + 6) != nameId)
                continue;

            int platform = U16(table, record);
            int language = U16(table, record + 4);
            int stringLength = U16(table, record + 8);
            int start = storage + U16(table, record + 10);
            if (start + stringLength > table.Length)
                continue;

            int rank;
            Encoding encoding;
            if (platform == 3 && language == 0x409)
            {
                rank = 0;
                encoding = Encoding.BigEndianUnicode;
            }
            else if (platform is 3 or 0)
            {
                rank = 1;
                encoding = Encoding.BigEndianUnicode;
            }
            else if (platform == 1 && language == 0)
            {
                rank = 2;
                encoding = Encoding.Latin1;
            }
            else
            {
                continue;
            }

            if (rank >= bestRank)
                continue;
            string text = encoding.GetString(table, start, stringLength).Trim();
            if (text.Length == 0)
                continue;
            best = text;
            bestRank = rank;
        }
        return best;
    }

    private static int U16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];

    private static int ReadU16(FileStream stream, long offset)
    {
        Span<byte> buffer = stackalloc byte[2];
        stream.Position = offset;
        stream.ReadExactly(buffer);
        return (buffer[0] << 8) | buffer[1];
    }

    private static uint ReadU32(FileStream stream, long offset)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.Position = offset;
        stream.ReadExactly(buffer);
        return ((uint)buffer[0] << 24) | ((uint)buffer[1] << 16) | ((uint)buffer[2] << 8) | buffer[3];
    }
}

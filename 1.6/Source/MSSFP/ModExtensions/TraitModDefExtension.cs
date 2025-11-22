using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MSSFP.ModExtensions;

public class TraitModDefExtension: DefModExtension
{
    public Color? color;
    public bool bold;
    public bool italic;
    public bool rainbow;

    public override IEnumerable<string> ConfigErrors()
    {
        if (color.HasValue && rainbow)
        {
            yield return "Cannot set both color and rainbow mode";
        }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MSSFP;

public static class ModLog
{
    [Conditional("DEBUG")]
    public static void Debug(string x)
    {
        Verse.Log.Message(x);
    }

    public static void Log(string msg)
    {
        Verse.Log.Message(
            $"<color=#1c6beb>[Mr_Samuel_Streamer_Flavour_Pack]</color> {msg ?? "<null>"}"
        );
    }

    public static void Warn(string msg)
    {
        Verse.Log.Warning(
            $"<color=#1c6beb>[Mr_Samuel_Streamer_Flavour_Pack]</color> {msg ?? "<null>"}"
        );
    }

    public static string GetExtendedexceptionDetails(object e, string indent = null)
    {
        // we want to be robust when dealing with errors logging
        try
        {
            StringBuilder sb = new StringBuilder(indent);
            // it's good to know the type of exception
            sb.AppendLine("Type: " + e.GetType().FullName);
            // fetch instance level properties that we can read
            IEnumerable<PropertyInfo> props = e.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            foreach (PropertyInfo p in props)
            {
                try
                {
                    object v = p.GetValue(e, null);

                    // Usually this is InnerException
                    if (v is Exception exception)
                    {
                        sb.AppendLine($"{indent}{p.Name}:");
                        sb.AppendLine(GetExtendedexceptionDetails(exception, "  " + indent)); // recursive call
                    }
                    // some other property
                    else
                    {
                        sb.AppendLine($"{indent}{p.Name}: '{v}'");

                        // Usually this is Data property
                        if (v is IDictionary kvps)
                        {
                            sb.AppendLine($"{" " + indent}count={kvps.Count}");
                            foreach (DictionaryEntry kvp in kvps)
                            {
                                sb.AppendLine($"{" " + indent}[{kvp.Key}]:[{kvp.Value}]");
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Verse.Log.Error(exception.ToString());
                }
            }

            //remove redundant CR+LF in the end of buffer
            sb.Length -= 2;
            return sb.ToString();
        }
        catch (Exception)
        {
            //log or swallow here
            return string.Empty;
        }
    }

    public static void Error(string msg, Exception e = null)
    {
        Verse.Log.Error(
            $"<color=#1c6beb>[Mr_Samuel_Streamer_Flavour_Pack]</color> {msg ?? "<null>"}"
        );
        if (e != null)
            Verse.Log.Error(GetExtendedexceptionDetails(e));
    }
}
